import 'dart:async';
import 'package:signalr_netcore/signalr_client.dart';
import '../../../core/config/api_config.dart';
import '../../../core/storage/secure_storage.dart';
import '../models/chat_message.dart';

/// Señal de «está escribiendo» que llega del hub.
class TypingSignal {
  final String senderId;
  final String vehicleId;
  const TypingSignal(this.senderId, this.vehicleId);
}

/// Mensaje empujado por el servidor, con la negociación a la que pertenece.
class IncomingMessage {
  final String negotiationId;
  final ChatMessage message;
  const IncomingMessage(this.negotiationId, this.message);
}

/// Conexión al hub de chat (`/hubs/chat`). Transporta lo efímero (typing, read) y
/// el aviso en vivo de mensajes nuevos; lo que se guarda va por REST.
///
/// El JWT viaja como `?access_token=` porque los handshakes WebSocket no admiten
/// cabeceras personalizadas (igual que la web).
class ChatHubService {
  ChatHubService(this._storage);

  final SecureStorage _storage;
  HubConnection? _hub;

  final _messages = StreamController<IncomingMessage>.broadcast();
  final _typing = StreamController<TypingSignal>.broadcast();

  /// Mensajes nuevos empujados por el servidor (`ReceiveMessage`).
  Stream<IncomingMessage> get messages => _messages.stream;

  /// La otra parte está escribiendo (`UserTyping`).
  Stream<TypingSignal> get typing => _typing.stream;

  bool get isConnected =>
      _hub?.state == HubConnectionState.Connected;

  Future<void> connect() async {
    if (_hub != null) return;

    final hub = HubConnectionBuilder()
        .withUrl(
          ApiConfig.chatHubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => await _storage.accessToken ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();

    hub.on('ReceiveMessage', (args) {
      if (args == null || args.isEmpty) return;
      final payload = args.first;
      if (payload is Map) {
        final map = Map<String, dynamic>.from(payload);
        final negotiationId =
            (map['negotiationId'] ?? map['NegotiationId'] ?? '').toString();
        _messages.add(IncomingMessage(negotiationId, ChatMessage.fromHub(map)));
      }
    });

    hub.on('UserTyping', (args) {
      if (args == null || args.isEmpty) return;
      final p = args.first;
      if (p is Map) {
        _typing.add(TypingSignal(
          (p['senderId'] ?? p['SenderId'] ?? '').toString(),
          (p['vehicleId'] ?? p['VehicleId'] ?? '').toString(),
        ));
      }
    });

    _hub = hub;
    try {
      await hub.start();
    } catch (_) {
      // El chat funciona igual por REST; el tiempo real es un extra.
    }
  }

  Future<void> startTyping(String recipientId, String vehicleId) async {
    if (!isConnected) return;
    try {
      await _hub!.invoke('StartTyping', args: [recipientId, vehicleId]);
    } catch (_) {}
  }

  Future<void> markAsRead(String senderId, String vehicleId) async {
    if (!isConnected) return;
    try {
      await _hub!.invoke('MarkAsRead', args: [senderId, vehicleId]);
    } catch (_) {}
  }

  Future<void> disconnect() async {
    final hub = _hub;
    _hub = null;
    try {
      await hub?.stop();
    } catch (_) {}
  }

  void dispose() {
    disconnect();
    _messages.close();
    _typing.close();
  }
}
