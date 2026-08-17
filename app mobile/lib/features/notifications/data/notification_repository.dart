import '../../../core/network/api_client.dart';
import '../models/app_notification.dart';

/// Acceso a las notificaciones del usuario (endpoint devuelve el valor directo:
/// `{ unreadCount, items }`).
class NotificationRepository {
  NotificationRepository(this._api);
  final ApiClient _api;

  Future<({int unreadCount, List<AppNotification> items})> getMyNotifications(
      {int take = 50}) async {
    final res =
        await _api.dio.get('/notifications', queryParameters: {'take': take});
    final data = res.data as Map<String, dynamic>;
    return (
      unreadCount: data['unreadCount'] as int? ?? 0,
      items: (data['items'] as List<dynamic>? ?? const [])
          .map((e) => AppNotification.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<void> markRead(String id) =>
      _api.dio.post('/notifications/$id/read');

  Future<void> markAllRead() => _api.dio.post('/notifications/read-all');
}
