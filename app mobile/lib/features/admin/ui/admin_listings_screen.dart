import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../vehicles/models/vehicle_enums.dart';
import '../models/admin_listing.dart';
import '../providers/admin_providers.dart';

class AdminListingsScreen extends ConsumerStatefulWidget {
  const AdminListingsScreen({super.key});

  @override
  ConsumerState<AdminListingsScreen> createState() =>
      _AdminListingsScreenState();
}

class _AdminListingsScreenState extends ConsumerState<AdminListingsScreen> {
  final _search = TextEditingController();
  String? _query;

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(adminListingsProvider(_query));

    return Scaffold(
      appBar: AppBar(title: const Text('Annonces')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: TextField(
              controller: _search,
              textInputAction: TextInputAction.search,
              onSubmitted: (v) =>
                  setState(() => _query = v.trim().isEmpty ? null : v.trim()),
              decoration: InputDecoration(
                hintText: 'Titre, référence…',
                prefixIcon: const Icon(Icons.search),
                isDense: true,
                suffixIcon: _search.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          _search.clear();
                          setState(() => _query = null);
                        },
                      ),
              ),
            ),
          ),
          Expanded(
            child: async.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (_, _) => Center(
                child: FilledButton(
                  onPressed: () =>
                      ref.invalidate(adminListingsProvider(_query)),
                  child: const Text('Réessayer'),
                ),
              ),
              data: (page) {
                if (page.items.isEmpty) {
                  return const Center(
                      child: Text('Aucune annonce',
                          style: TextStyle(color: AppColors.steel)));
                }
                return ListView.separated(
                  itemCount: page.items.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) => _ListingTile(row: page.items[i]),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _ListingTile extends StatelessWidget {
  const _ListingTile({required this.row});
  final AdminListingRow row;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(row.title,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontWeight: FontWeight.w700)),
      subtitle: Text(
          '${row.publicReference} · ${statusLabel(row.status)} · ${fcfa(row.price)}'),
      trailing: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          if (row.hiddenByAdmin)
            const Icon(Icons.visibility_off, size: 16, color: AppColors.error),
          if (row.flaggedForReview)
            const Icon(Icons.flag, size: 16, color: AppColors.warning),
          if (row.openReports > 0)
            Text('${row.openReports} signal.',
                style: const TextStyle(
                    fontSize: 11,
                    color: AppColors.error,
                    fontWeight: FontWeight.w600)),
        ],
      ),
      onTap: () => context.push('/admin/listings/${row.id}'),
    );
  }
}
