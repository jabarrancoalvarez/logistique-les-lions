import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/providers/auth_providers.dart';
import '../data/notification_hub_service.dart';
import '../data/notification_repository.dart';
import '../models/app_notification.dart';

final notificationRepositoryProvider = Provider<NotificationRepository>(
  (ref) => NotificationRepository(ref.watch(apiClientProvider)),
);

/// Hub de notificaciones, ligado a la sesión.
final notificationHubServiceProvider = Provider<NotificationHubService>((ref) {
  final service = NotificationHubService(ref.watch(secureStorageProvider));
  ref.onDispose(service.dispose);

  ref.listen(authControllerProvider, (previous, next) {
    if (next is Authenticated) {
      service.connect();
    } else if (next is Unauthenticated) {
      service.disconnect();
    }
  });
  if (ref.read(authControllerProvider) is Authenticated) service.connect();
  return service;
});

class NotificationsState {
  final List<AppNotification> items;
  final int unreadCount;
  final bool loading;
  final bool loaded;

  const NotificationsState({
    this.items = const [],
    this.unreadCount = 0,
    this.loading = false,
    this.loaded = false,
  });

  NotificationsState copyWith({
    List<AppNotification>? items,
    int? unreadCount,
    bool? loading,
    bool? loaded,
  }) =>
      NotificationsState(
        items: items ?? this.items,
        unreadCount: unreadCount ?? this.unreadCount,
        loading: loading ?? this.loading,
        loaded: loaded ?? this.loaded,
      );
}

class NotificationsController extends StateNotifier<NotificationsState> {
  NotificationsController(this._ref) : super(const NotificationsState()) {
    _sub = _ref.read(notificationHubServiceProvider).incoming.listen(_onIncoming);
    _ref.listen(authControllerProvider, (previous, next) {
      if (next is Authenticated && previous is! Authenticated) {
        load();
      } else if (next is Unauthenticated) {
        state = const NotificationsState();
      }
    });
    if (_ref.read(authControllerProvider) is Authenticated) load();
  }

  final Ref _ref;
  StreamSubscription<AppNotification>? _sub;

  NotificationRepository get _repo =>
      _ref.read(notificationRepositoryProvider);

  void _onIncoming(AppNotification n) {
    // Evita duplicar si ya está (p. ej. tras recargar).
    if (state.items.any((e) => e.id == n.id)) return;
    state = state.copyWith(
      items: [n, ...state.items],
      unreadCount: state.unreadCount + 1,
    );
  }

  Future<void> load() async {
    state = state.copyWith(loading: true);
    try {
      final res = await _repo.getMyNotifications();
      state = NotificationsState(
        items: res.items,
        unreadCount: res.unreadCount,
        loading: false,
        loaded: true,
      );
    } catch (_) {
      state = state.copyWith(loading: false, loaded: true);
    }
  }

  Future<void> markRead(String id) async {
    final target = state.items.where((e) => e.id == id).firstOrNull;
    if (target == null || target.isRead) return;
    state = state.copyWith(
      items: [
        for (final n in state.items) n.id == id ? n.copyWith(isRead: true) : n,
      ],
      unreadCount: (state.unreadCount - 1).clamp(0, 1 << 30),
    );
    try {
      await _repo.markRead(id);
    } catch (_) {}
  }

  Future<void> markAllRead() async {
    state = state.copyWith(
      items: [for (final n in state.items) n.copyWith(isRead: true)],
      unreadCount: 0,
    );
    try {
      await _repo.markAllRead();
    } catch (_) {}
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}

final notificationsControllerProvider =
    StateNotifierProvider<NotificationsController, NotificationsState>(
  (ref) => NotificationsController(ref),
);
