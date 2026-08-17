import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../models/admin_dashboard.dart';
import '../providers/admin_providers.dart';

/// Backoffice — panel de administración. Solo rol Admin.
class AdminHomeScreen extends ConsumerWidget {
  const AdminHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminDashboardProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Backoffice')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(adminDashboardProvider),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
          children: [
            async.when(
              loading: () => const Padding(
                padding: EdgeInsets.all(40),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (_, _) => Padding(
                padding: const EdgeInsets.all(24),
                child: Center(
                  child: FilledButton(
                    onPressed: () => ref.invalidate(adminDashboardProvider),
                    child: const Text('Réessayer'),
                  ),
                ),
              ),
              data: (d) => _Dashboard(dashboard: d),
            ),
            const SizedBox(height: 16),
            _NavTile(
              icon: Icons.people_outline,
              title: 'Utilisateurs',
              onTap: () => context.push('/admin/users'),
            ),
            _NavTile(
              icon: Icons.directions_car_outlined,
              title: 'Annonces',
              onTap: () => context.push('/admin/listings'),
            ),
            _NavTile(
              icon: Icons.flag_outlined,
              title: 'Signalements',
              onTap: () => context.push('/admin/reports'),
            ),
          ],
        ),
      ),
    );
  }
}

class _Dashboard extends StatelessWidget {
  const _Dashboard({required this.dashboard});
  final AdminDashboard dashboard;

  @override
  Widget build(BuildContext context) {
    final d = dashboard;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _Group(title: 'Utilisateurs', stats: [
          ('Total', d.users.total),
          ('Nouveaux 7 j', d.users.newLast7Days),
          ('Tél. vérifié', d.users.phoneVerified),
          ('Pros', d.users.professionnels),
        ]),
        _Group(title: 'Marketplace', stats: [
          ('Actives', d.marketplace.active),
          ('Réservées', d.marketplace.reserved),
          ('Vendues', d.marketplace.sold),
          ('À modérer', d.marketplace.pendingModeration),
        ]),
        _Group(title: 'Activité', stats: [
          ('Négociations', d.activity.negotiationsActive),
          ('Offres', d.activity.offersMade),
          ('Contrats', d.activity.contractsValidated),
          ('Ventes vérif.', d.activity.verifiedSales),
        ]),
        _Group(title: 'Demande & Garage', stats: [
          ('Favoris', d.demand.favoritesTotal),
          ('Demandes', d.demand.requestsPending),
          ('Garages', d.garage.vehiclesTotal),
          ('→ annonces', d.garage.convertedToListings),
        ]),
      ],
    );
  }
}

class _Group extends StatelessWidget {
  const _Group({required this.title, required this.stats});
  final String title;
  final List<(String, int)> stats;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title,
              style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  color: AppColors.navy,
                  fontSize: 15)),
          const SizedBox(height: 8),
          Row(
            children: [
              for (final (label, value) in stats)
                Expanded(
                  child: Container(
                    margin: const EdgeInsets.only(right: 8),
                    padding:
                        const EdgeInsets.symmetric(vertical: 12, horizontal: 6),
                    decoration: BoxDecoration(
                      color: AppColors.frost,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: AppColors.frostDark),
                    ),
                    child: Column(
                      children: [
                        Text('$value',
                            style: const TextStyle(
                                fontWeight: FontWeight.w800,
                                fontSize: 18,
                                color: AppColors.azureDark)),
                        const SizedBox(height: 2),
                        Text(label,
                            textAlign: TextAlign.center,
                            maxLines: 2,
                            style: const TextStyle(
                                fontSize: 10, color: AppColors.steel)),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _NavTile extends StatelessWidget {
  const _NavTile(
      {required this.icon, required this.title, required this.onTap});
  final IconData icon;
  final String title;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: Icon(icon, color: AppColors.azureDark),
        title: Text(title,
            style: const TextStyle(fontWeight: FontWeight.w700)),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
