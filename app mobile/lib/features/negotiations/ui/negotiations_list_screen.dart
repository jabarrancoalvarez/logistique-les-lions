import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../../core/util/time_ago.dart';
import '../../auth/providers/auth_providers.dart';
import '../models/negotiation_summary.dart';
import '../providers/negotiation_providers.dart';

/// «Mes négociations» con pestañas En cours · En attente · Terminées. Requiere sesión.
class NegotiationsListScreen extends ConsumerWidget {
  const NegotiationsListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authControllerProvider);

    if (auth is! Authenticated) {
      return Scaffold(
        appBar: AppBar(title: const Text('Négociations')),
        body: _GuestView(),
      );
    }

    return DefaultTabController(
      length: 3,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Négociations'),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'En cours'),
              Tab(text: 'En attente'),
              Tab(text: 'Terminées'),
            ],
          ),
        ),
        body: const TabBarView(
          children: [
            _NegotiationsTab(status: 'EnCours'),
            _NegotiationsTab(status: 'EnAttente'),
            _NegotiationsTab(status: 'Terminee'),
          ],
        ),
      ),
    );
  }
}

class _NegotiationsTab extends ConsumerWidget {
  const _NegotiationsTab({required this.status});
  final String status;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(negotiationsListProvider(status));

    return async.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (_, _) => _ErrorView(
        onRetry: () => ref.invalidate(negotiationsListProvider(status)),
      ),
      data: (list) {
        if (list.isEmpty) return const _EmptyView();
        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(negotiationsListProvider(status)),
          child: ListView.separated(
            padding: const EdgeInsets.symmetric(vertical: 8),
            itemCount: list.length,
            separatorBuilder: (_, _) =>
                const Divider(height: 1, indent: 88),
            itemBuilder: (_, i) => _NegotiationTile(item: list[i]),
          ),
        );
      },
    );
  }
}

class _NegotiationTile extends StatelessWidget {
  const _NegotiationTile({required this.item});
  final NegotiationSummary item;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      leading: _Thumb(url: item.vehicleThumbnailUrl),
      title: Text(item.vehicleTitle,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(
              fontWeight: FontWeight.w700, color: AppColors.navy)),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SizedBox(height: 2),
          Text(
            '${item.isBuyer ? 'Vendeur' : 'Acheteur'} : ${item.otherUserName}',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 12, color: AppColors.steel),
          ),
          const SizedBox(height: 2),
          Text(
            item.lastMessage ?? fcfa(item.vehiclePrice),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              fontSize: 13,
              color: item.unreadCount > 0 ? AppColors.navy : AppColors.steel,
              fontWeight:
                  item.unreadCount > 0 ? FontWeight.w600 : FontWeight.w400,
            ),
          ),
        ],
      ),
      trailing: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text(timeAgo(item.lastActivityAt),
              style: const TextStyle(fontSize: 11, color: AppColors.steel)),
          const SizedBox(height: 6),
          if (item.unreadCount > 0)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
              decoration: const BoxDecoration(
                color: AppColors.azureDark,
                shape: BoxShape.circle,
              ),
              child: Text('${item.unreadCount}',
                  style: const TextStyle(
                      color: AppColors.white,
                      fontSize: 11,
                      fontWeight: FontWeight.w700)),
            ),
        ],
      ),
      onTap: () => context.push('/negociations/${item.id}'),
    );
  }
}

class _Thumb extends StatelessWidget {
  const _Thumb({this.url});
  final String? url;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: SizedBox(
        width: 56,
        height: 56,
        child: url != null
            ? Image.network(url!,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => const _ThumbPlaceholder())
            : const _ThumbPlaceholder(),
      ),
    );
  }
}

class _ThumbPlaceholder extends StatelessWidget {
  const _ThumbPlaceholder();
  @override
  Widget build(BuildContext context) => Container(
        color: AppColors.frostDark,
        child: const Icon(Icons.directions_car_outlined,
            color: AppColors.silver, size: 24),
      );
}

class _EmptyView extends StatelessWidget {
  const _EmptyView();
  @override
  Widget build(BuildContext context) {
    return ListView(
      children: const [
        SizedBox(height: 100),
        Icon(Icons.forum_outlined, size: 56, color: AppColors.silver),
        SizedBox(height: 16),
        Center(
          child: Text('Aucune négociation ici',
              style: TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 16,
                  color: AppColors.navy)),
        ),
        SizedBox(height: 6),
        Padding(
          padding: EdgeInsets.symmetric(horizontal: 40),
          child: Text(
            'Contactez un vendeur depuis une annonce pour démarrer une négociation.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.steel),
          ),
        ),
      ],
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.onRetry});
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off, size: 48, color: AppColors.silver),
          const SizedBox(height: 12),
          const Text('Impossible de charger',
              style: TextStyle(color: AppColors.steel)),
          const SizedBox(height: 16),
          FilledButton(onPressed: onRetry, child: const Text('Réessayer')),
        ],
      ),
    );
  }
}

class _GuestView extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.forum_outlined, size: 56, color: AppColors.silver),
            const SizedBox(height: 16),
            const Text('Vos négociations',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            const SizedBox(height: 6),
            const Text(
              'Connectez-vous pour discuter avec les vendeurs et suivre vos offres.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.steel),
            ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: () => context.push('/login'),
              child: const Text('Se connecter'),
            ),
          ],
        ),
      ),
    );
  }
}
