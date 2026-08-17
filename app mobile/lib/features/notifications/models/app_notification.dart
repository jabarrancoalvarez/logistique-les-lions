import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

/// Notificación del usuario (`NotificationDto`). El evento en vivo del hub trae
/// solo id/category/title/body/link.
class AppNotification {
  final String id;
  final String category;
  final String title;
  final String? body;
  final String? link;
  final bool isRead;
  final DateTime createdAt;

  const AppNotification({
    required this.id,
    required this.category,
    required this.title,
    required this.isRead,
    required this.createdAt,
    this.body,
    this.link,
  });

  AppNotification copyWith({bool? isRead}) => AppNotification(
        id: id,
        category: category,
        title: title,
        body: body,
        link: link,
        isRead: isRead ?? this.isRead,
        createdAt: createdAt,
      );

  factory AppNotification.fromJson(Map<String, dynamic> j) => AppNotification(
        id: j['id'] as String,
        category: (j['category'] ?? 'system') as String,
        title: (j['title'] ?? '') as String,
        body: j['body'] as String?,
        link: j['link'] as String?,
        isRead: j['isRead'] as bool? ?? false,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );

  /// Desde el evento `notification` del hub (sin isRead ni createdAt).
  factory AppNotification.fromHub(Map<String, dynamic> j) => AppNotification(
        id: (j['id'] ?? '') as String,
        category: (j['category'] ?? 'system') as String,
        title: (j['title'] ?? '') as String,
        body: j['body'] as String?,
        link: j['link'] as String?,
        isRead: false,
        createdAt: DateTime.now(),
      );
}

/// Icono y color por categoría (mismas categorías que el backend).
({IconData icon, Color color}) notificationStyle(String category) =>
    switch (category) {
      'message' => (icon: Icons.chat_bubble_outline, color: AppColors.azureDark),
      'offer' => (icon: Icons.local_offer_outlined, color: AppColors.warning),
      'contract' => (icon: Icons.description_outlined, color: AppColors.navy),
      'reminder' => (icon: Icons.notifications_active_outlined, color: AppColors.azureDark),
      'price-drop' => (icon: Icons.trending_down, color: AppColors.success),
      'new-listing' => (icon: Icons.directions_car_outlined, color: AppColors.azureDark),
      'request-proposal' => (icon: Icons.campaign_outlined, color: AppColors.warning),
      'admin' => (icon: Icons.shield_outlined, color: AppColors.error),
      _ => (icon: Icons.notifications_none, color: AppColors.steel),
    };

/// Traduce el `link` del backend (rutas web) a una ruta de la app, o `null`.
String? notificationRoute(String? link) {
  if (link == null || link.isEmpty) return null;
  final l = link.startsWith('/') ? link : '/$link';
  if (l.startsWith('/mis-negociaciones/')) {
    return '/negociations/${l.split('/').last}';
  }
  if (l.startsWith('/vehiculos/') || l.startsWith('/vehicules/')) {
    return '/vehicules/${l.split('/').last}';
  }
  if (l.startsWith('/favoritos') || l.startsWith('/favoris')) return '/favoris';
  return null;
}
