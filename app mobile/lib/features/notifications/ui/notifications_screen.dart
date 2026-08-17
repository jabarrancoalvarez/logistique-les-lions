import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/time_ago.dart';
import '../models/app_notification.dart';
import '../providers/notification_providers.dart';

/// Lista de notificaciones del usuario, con lectura y navegación al elemento.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(notificationsControllerProvider);
    final controller = ref.read(notificationsControllerProvider.notifier);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Notifications'),
        actions: [
          if (state.unreadCount > 0)
            TextButton(
              onPressed: controller.markAllRead,
              child: const Text('Tout lire'),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: controller.load,
        child: _body(context, state, controller),
      ),
    );
  }

  Widget _body(BuildContext context, NotificationsState state,
      NotificationsController controller) {
    if (state.loading && state.items.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.items.isEmpty) {
      return ListView(
        children: const [
          SizedBox(height: 120),
          Icon(Icons.notifications_none, size: 56, color: AppColors.silver),
          SizedBox(height: 16),
          Center(
            child: Text('Aucune notification',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
          ),
          SizedBox(height: 6),
          Center(
            child: Text('Vous serez prévenu ici des messages, offres et rappels.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.steel)),
          ),
        ],
      );
    }

    return ListView.separated(
      itemCount: state.items.length,
      separatorBuilder: (_, _) => const Divider(height: 1, indent: 72),
      itemBuilder: (_, i) {
        final n = state.items[i];
        return _NotificationTile(
          notification: n,
          onTap: () {
            controller.markRead(n.id);
            final route = notificationRoute(n.link);
            if (route != null) context.push(route);
          },
        );
      },
    );
  }
}

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({required this.notification, required this.onTap});
  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final n = notification;
    final style = notificationStyle(n.category);

    return ListTile(
      onTap: onTap,
      tileColor: n.isRead ? null : AppColors.frost,
      leading: CircleAvatar(
        backgroundColor: style.color.withValues(alpha: 0.12),
        child: Icon(style.icon, color: style.color, size: 20),
      ),
      title: Text(n.title,
          style: TextStyle(
              fontWeight: n.isRead ? FontWeight.w600 : FontWeight.w800,
              color: AppColors.navy)),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (n.body != null && n.body!.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 2),
              child: Text(n.body!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 13)),
            ),
          const SizedBox(height: 2),
          Text(timeAgo(n.createdAt),
              style: const TextStyle(fontSize: 11, color: AppColors.steel)),
        ],
      ),
      trailing: n.isRead
          ? null
          : Container(
              width: 9,
              height: 9,
              decoration: const BoxDecoration(
                  color: AppColors.azureDark, shape: BoxShape.circle),
            ),
    );
  }
}
