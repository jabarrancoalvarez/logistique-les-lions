import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../negotiations/models/negotiation_enums.dart';
import '../models/admin_negotiation.dart';
import '../providers/admin_providers.dart';

class AdminNegotiationsScreen extends ConsumerWidget {
  const AdminNegotiationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminNegotiationsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Négociations')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(adminNegotiationsProvider),
            child: const Text('Réessayer'),
          ),
        ),
        data: (page) {
          if (page.items.isEmpty) {
            return const Center(
                child: Text('Aucune négociation',
                    style: TextStyle(color: AppColors.steel)));
          }
          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(adminNegotiationsProvider),
            child: ListView.separated(
              itemCount: page.items.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (_, i) => _Tile(row: page.items[i]),
            ),
          );
        },
      ),
    );
  }
}

class _Tile extends StatelessWidget {
  const _Tile({required this.row});
  final AdminNegotiationRow row;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(row.vehicleTitle,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontWeight: FontWeight.w700)),
      subtitle: Text(
        '${row.buyerName} ↔ ${row.sellerName} · ${row.offersCount} offre(s) · ${row.messagesCount} msg',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
      trailing: Text(negotiationStatusLabel(row.status),
          style: const TextStyle(fontSize: 12, color: AppColors.steel)),
      onTap: () => context.push('/admin/negotiations/${row.id}'),
    );
  }
}
