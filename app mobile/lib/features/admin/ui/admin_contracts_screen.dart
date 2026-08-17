import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../negotiations/models/contract.dart';
import '../models/admin_contract.dart';
import '../providers/admin_providers.dart';

class AdminContractsScreen extends ConsumerWidget {
  const AdminContractsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminContractsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Contrats')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(adminContractsProvider),
            child: const Text('Réessayer'),
          ),
        ),
        data: (page) {
          if (page.items.isEmpty) {
            return const Center(
                child: Text('Aucun contrat',
                    style: TextStyle(color: AppColors.steel)));
          }
          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(adminContractsProvider),
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
  final AdminContractRow row;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(row.vehicleLabel,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontWeight: FontWeight.w700)),
      subtitle: Text(
          '${row.publicReference} · ${fcfa(row.agreedPrice)} · ${row.buyerName}'),
      trailing: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text(contractStatusLabel(row.status),
              style: const TextStyle(fontSize: 12, color: AppColors.steel)),
          if (row.isVerifiedSale)
            const Icon(Icons.verified, size: 15, color: AppColors.azureDark),
        ],
      ),
      onTap: () => context.push('/admin/contracts/${row.id}'),
    );
  }
}
