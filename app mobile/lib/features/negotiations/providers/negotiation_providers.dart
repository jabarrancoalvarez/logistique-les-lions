import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/providers/auth_providers.dart';
import '../data/chat_hub_service.dart';
import '../data/negotiation_repository.dart';
import '../models/chat_message.dart';
import '../models/negotiation_detail.dart';
import '../models/negotiation_summary.dart';

final negotiationRepositoryProvider = Provider<NegotiationRepository>(
  (ref) => NegotiationRepository(ref.watch(apiClientProvider)),
);

/// Conexión al hub de chat, ligada a la sesión: conecta al iniciarla y desconecta
/// al cerrarla. Vive mientras viva el contenedor de providers.
final chatHubServiceProvider = Provider<ChatHubService>((ref) {
  final service = ChatHubService(ref.watch(secureStorageProvider));
  ref.onDispose(service.dispose);

  ref.listen(authControllerProvider, (previous, next) {
    if (next is Authenticated) {
      service.connect();
    } else if (next is Unauthenticated) {
      service.disconnect();
    }
  });

  if (ref.read(authControllerProvider) is Authenticated) {
    service.connect();
  }
  return service;
});

/// «Mes négociations» filtradas por pestaña (status `null` = todas).
final negotiationsListProvider =
    FutureProvider.family<List<NegotiationSummary>, String?>(
  (ref, status) =>
      ref.watch(negotiationRepositoryProvider).getMyNegotiations(status: status),
);

// ─── Chat de una negociación ───────────────────────────────────────────────

class ChatState {
  final NegotiationDetail? detail;
  final List<ChatMessage> messages;
  final bool loading;
  final bool error;
  final bool sending;
  final bool actionBusy;
  final bool otherTyping;

  const ChatState({
    this.detail,
    this.messages = const [],
    this.loading = true,
    this.error = false,
    this.sending = false,
    this.actionBusy = false,
    this.otherTyping = false,
  });

  ChatState copyWith({
    NegotiationDetail? detail,
    List<ChatMessage>? messages,
    bool? loading,
    bool? error,
    bool? sending,
    bool? actionBusy,
    bool? otherTyping,
  }) =>
      ChatState(
        detail: detail ?? this.detail,
        messages: messages ?? this.messages,
        loading: loading ?? this.loading,
        error: error ?? this.error,
        sending: sending ?? this.sending,
        actionBusy: actionBusy ?? this.actionBusy,
        otherTyping: otherTyping ?? this.otherTyping,
      );
}

class ChatController extends StateNotifier<ChatState> {
  ChatController(this._ref, this.negotiationId) : super(const ChatState()) {
    _subscribe();
  }

  final Ref _ref;
  final String negotiationId;
  StreamSubscription<IncomingMessage>? _msgSub;
  StreamSubscription<TypingSignal>? _typingSub;
  Timer? _typingTimer;

  NegotiationRepository get _repo => _ref.read(negotiationRepositoryProvider);
  ChatHubService get _hub => _ref.read(chatHubServiceProvider);

  String? get _myId {
    final auth = _ref.read(authControllerProvider);
    return auth is Authenticated ? auth.user.id : null;
  }

  void _subscribe() {
    final hub = _hub;
    _msgSub = hub.messages.listen((incoming) {
      if (incoming.negotiationId != negotiationId) return;
      if (state.messages.any((m) => m.id == incoming.message.id)) return;
      state = state.copyWith(
        messages: [...state.messages, incoming.message],
        otherTyping: false,
      );
      final other = state.detail?.otherUserId;
      final vehicle = state.detail?.vehicleId;
      if (other != null && vehicle != null) hub.markAsRead(other, vehicle);
    });
    _typingSub = hub.typing.listen((signal) {
      if (signal.senderId != state.detail?.otherUserId) return;
      state = state.copyWith(otherTyping: true);
      _typingTimer?.cancel();
      _typingTimer = Timer(const Duration(seconds: 3), () {
        if (mounted) state = state.copyWith(otherTyping: false);
      });
    });
  }

  Future<void> load() async {
    state = state.copyWith(loading: true, error: false);
    try {
      final results = await Future.wait([
        _repo.getNegotiation(negotiationId),
        _repo.getMessages(negotiationId),
      ]);
      final detail = results[0] as NegotiationDetail;
      final messages = results[1] as List<ChatMessage>;
      state = state.copyWith(
          detail: detail, messages: messages, loading: false);
      _hub.markAsRead(detail.otherUserId, detail.vehicleId);
    } catch (_) {
      state = state.copyWith(loading: false, error: true);
    }
  }

  Future<void> refreshDetail() async {
    try {
      final detail = await _repo.getNegotiation(negotiationId);
      state = state.copyWith(detail: detail);
    } catch (_) {}
  }

  Future<bool> send(String body) async {
    final detail = state.detail;
    final myId = _myId;
    if (detail == null || myId == null || body.trim().isEmpty) return false;
    state = state.copyWith(sending: true);
    try {
      final id = await _repo.sendMessage(
        recipientId: detail.otherUserId,
        vehicleId: detail.vehicleId,
        body: body.trim(),
      );
      final msg = ChatMessage(
        id: id,
        senderId: myId,
        body: body.trim(),
        createdAt: DateTime.now(),
        isRead: false,
      );
      state = state.copyWith(sending: false, messages: [...state.messages, msg]);
      return true;
    } catch (_) {
      state = state.copyWith(sending: false);
      return false;
    }
  }

  void notifyTyping() {
    final detail = state.detail;
    if (detail != null) _hub.startTyping(detail.otherUserId, detail.vehicleId);
  }

  Future<String?> makeCounterOffer(num amount, String? message) =>
      _run(() => _repo.counterOffer(negotiationId,
          amount: amount, message: message));

  Future<String?> acceptOffer(String offerId) =>
      _run(() => _repo.acceptOffer(offerId));

  Future<String?> rejectOffer(String offerId) =>
      _run(() => _repo.rejectOffer(offerId));

  /// Ejecuta una acción de oferta y recarga la cronología. Devuelve un mensaje
  /// de error o `null` si fue bien.
  Future<String?> _run(Future<void> Function() action) async {
    state = state.copyWith(actionBusy: true);
    try {
      await action();
      await refreshDetail();
      state = state.copyWith(actionBusy: false);
      return null;
    } catch (_) {
      state = state.copyWith(actionBusy: false);
      return 'Action impossible. Réessayez.';
    }
  }

  @override
  void dispose() {
    _msgSub?.cancel();
    _typingSub?.cancel();
    _typingTimer?.cancel();
    super.dispose();
  }
}

final chatControllerProvider = StateNotifierProvider.autoDispose
    .family<ChatController, ChatState, String>(
  (ref, negotiationId) => ChatController(ref, negotiationId)..load(),
);
