import 'dart:async';
import 'package:signalr_netcore/signalr_client.dart';
import '../../../core/config/api_config.dart';
import '../../../core/storage/secure_storage.dart';
import '../models/app_notification.dart';

/// Conexión al hub de notificaciones (`/hubs/notifications`). Empuja en vivo lo
/// que el backend genera dentro de la transacción de negocio, tras SaveChanges.
class NotificationHubService {
  NotificationHubService(this._storage);

  final SecureStorage _storage;
  HubConnection? _hub;
  final _incoming = StreamController<AppNotification>.broadcast();

  /// Notificaciones nuevas empujadas por el servidor (evento `notification`).
  Stream<AppNotification> get incoming => _incoming.stream;

  Future<void> connect() async {
    if (_hub != null) return;

    final hub = HubConnectionBuilder()
        .withUrl(
          ApiConfig.notificationsHubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => await _storage.accessToken ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();

    hub.on('notification', (args) {
      if (args == null || args.isEmpty) return;
      final payload = args.first;
      if (payload is Map) {
        _incoming.add(
            AppNotification.fromHub(Map<String, dynamic>.from(payload)));
      }
    });

    _hub = hub;
    try {
      await hub.start();
    } catch (_) {
      // La campana funciona igual por REST; el tiempo real es un extra.
    }
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
    _incoming.close();
  }
}
