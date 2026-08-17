import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../models/admin_enums.dart';
import '../models/admin_user.dart';
import '../providers/admin_providers.dart';
import 'reason_dialog.dart';
import 'widgets/admin_history.dart';

class AdminUserDetailScreen extends ConsumerWidget {
  const AdminUserDetailScreen({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminUserProvider(id));

    return Scaffold(
      appBar: AppBar(title: const Text('Utilisateur')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(adminUserProvider(id)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (u) => _Body(detail: u),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.detail});
  final AdminUserDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final p = detail.profile;
    final a = detail.activity;

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        Row(
          children: [
            CircleAvatar(
              radius: 26,
              backgroundColor: AppColors.navy,
              child: Text(
                p.displayName.isNotEmpty ? p.displayName[0].toUpperCase() : '?',
                style: const TextStyle(color: AppColors.white, fontSize: 20),
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(p.displayName,
                      style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w800,
                          color: AppColors.navy)),
                  Text('${userRoleLabel(p.role)} · ${accountTypeLabel(p.accountType)}',
                      style: const TextStyle(color: AppColors.steel)),
                ],
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: accountStatusColor(p.status).withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(accountStatusLabel(p.status),
                  style: TextStyle(
                      fontWeight: FontWeight.w700,
                      color: accountStatusColor(p.status))),
            ),
          ],
        ),
        const SizedBox(height: 16),
        AdminSection(
          title: 'Profil',
          child: Column(
            children: [
              AdminRow('Téléphone',
                  '${p.phone}${p.phoneVerified ? ' ✓' : ''}'),
              if (p.email != null) AdminRow('E-mail', p.email!),
              if (detail.region != null) AdminRow('Région', detail.region!),
              if (p.city != null) AdminRow('Ville', p.city!),
              AdminRow('Inscrit le',
                  DateFormat('d MMM yyyy', 'fr').format(p.createdAt.toLocal())),
              if (p.suspendedUntil != null)
                AdminRow('Suspendu jusqu’au',
                    DateFormat('d MMM yyyy', 'fr')
                        .format(p.suspendedUntil!.toLocal())),
            ],
          ),
        ),
        AdminSection(
          title: 'Activité',
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _chip('Annonces', a.listingsPublished),
              _chip('Vendues', a.listingsSold),
              _chip('Négociations', a.negotiations),
              _chip('Ventes vérif.', a.verifiedSales),
              _chip('Garage', a.garageVehicles),
              _chip('Signalé', a.reportsReceived),
            ],
          ),
        ),
        AdminSection(
          title: 'Statut du compte',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'Toute mesure exige un motif et laisse une trace.',
                style: TextStyle(fontSize: 12, color: AppColors.steel),
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                children: [
                  for (final s in accountStatusValues)
                    if (s != p.status)
                      OutlinedButton(
                        onPressed: () => _changeStatus(context, ref, p.id, s),
                        style: OutlinedButton.styleFrom(
                            foregroundColor: accountStatusColor(s)),
                        child: Text(accountStatusLabel(s)),
                      ),
                ],
              ),
            ],
          ),
        ),
        AdminSection(
          title: 'Historique',
          child: AdminActionsHistory(
              actions: detail.actions, notes: detail.notes),
        ),
      ],
    );
  }

  Widget _chip(String label, int value) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        decoration: BoxDecoration(
          color: AppColors.frost,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: AppColors.frostDark),
        ),
        child: Text('$label : $value',
            style: const TextStyle(fontSize: 12, color: AppColors.navyDark)),
      );

  Future<void> _changeStatus(
      BuildContext context, WidgetRef ref, String userId, String status) async {
    final reason = await askReason(
      context,
      title: '${accountStatusLabel(status)} ce compte ?',
      confirmLabel: accountStatusLabel(status),
      destructive: status != 'Active',
    );
    if (reason == null) return;
    try {
      await ref
          .read(adminRepositoryProvider)
          .setUserStatus(userId, status: status, reason: reason);
      ref.invalidate(adminUserProvider(userId));
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Statut mis à jour.')),
        );
      }
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Action impossible.')),
        );
      }
    }
  }
}
