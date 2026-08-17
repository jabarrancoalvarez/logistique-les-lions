import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../models/admin_enums.dart';
import '../models/admin_user.dart';
import '../providers/admin_providers.dart';

class AdminUsersScreen extends ConsumerStatefulWidget {
  const AdminUsersScreen({super.key});

  @override
  ConsumerState<AdminUsersScreen> createState() => _AdminUsersScreenState();
}

class _AdminUsersScreenState extends ConsumerState<AdminUsersScreen> {
  final _search = TextEditingController();
  String? _query;

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(adminUsersProvider((search: _query, status: null)));

    return Scaffold(
      appBar: AppBar(title: const Text('Utilisateurs')),
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
                hintText: 'Nom, téléphone, e-mail…',
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
                  onPressed: () => ref.invalidate(
                      adminUsersProvider((search: _query, status: null))),
                  child: const Text('Réessayer'),
                ),
              ),
              data: (page) {
                if (page.items.isEmpty) {
                  return const Center(
                      child: Text('Aucun utilisateur',
                          style: TextStyle(color: AppColors.steel)));
                }
                return ListView.separated(
                  itemCount: page.items.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) => _UserTile(user: page.items[i]),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _UserTile extends StatelessWidget {
  const _UserTile({required this.user});
  final AdminUserRow user;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: CircleAvatar(
        backgroundColor: AppColors.navy,
        child: Text(
          user.displayName.isNotEmpty
              ? user.displayName[0].toUpperCase()
              : '?',
          style: const TextStyle(color: AppColors.white),
        ),
      ),
      title: Row(
        children: [
          Expanded(
            child: Text(user.displayName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontWeight: FontWeight.w700)),
          ),
          if (user.role == 'Admin')
            const Padding(
              padding: EdgeInsets.only(left: 4),
              child: Icon(Icons.shield, size: 14, color: AppColors.azureDark),
            ),
        ],
      ),
      subtitle: Text('${user.phone} · ${accountTypeLabel(user.accountType)}'),
      trailing: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: accountStatusColor(user.status).withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Text(accountStatusLabel(user.status),
            style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: accountStatusColor(user.status))),
      ),
      onTap: () => context.push('/admin/users/${user.id}'),
    );
  }
}
