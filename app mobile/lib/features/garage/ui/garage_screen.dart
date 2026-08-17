import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../models/garage_models.dart';
import '../providers/garage_providers.dart';
import 'widgets/garage_image.dart';

/// Mon Garage — espacio privado del usuario con los vehículos que posee.
class GarageScreen extends ConsumerWidget {
  const GarageScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(garageSummaryProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Mon Garage')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          await context.push('/garage/nouveau');
          ref.invalidate(garageSummaryProvider);
        },
        icon: const Icon(Icons.add),
        label: const Text('Ajouter'),
      ),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(garageSummaryProvider),
            child: const Text('Réessayer'),
          ),
        ),
        data: (garage) {
          if (garage.vehicles.isEmpty) return const _EmptyGarage();
          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(garageSummaryProvider),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
              children: [
                _SummaryBar(garage: garage),
                const SizedBox(height: 16),
                for (final v in garage.vehicles) ...[
                  _VehicleCard(
                    card: v,
                    onTap: () async {
                      await context.push('/garage/${v.id}');
                      ref.invalidate(garageSummaryProvider);
                    },
                  ),
                  const SizedBox(height: 14),
                ],
              ],
            ),
          );
        },
      ),
    );
  }
}

class _SummaryBar extends StatelessWidget {
  const _SummaryBar({required this.garage});
  final GarageSummary garage;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [AppColors.navy, AppColors.navyLight],
        ),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        children: [
          _stat('${garage.vehicleCount}',
              garage.vehicleCount > 1 ? 'véhicules' : 'véhicule'),
          _divider(),
          _stat('${garage.openReminderCount}',
              garage.openReminderCount > 1 ? 'rappels' : 'rappel'),
          _divider(),
          _stat(
            garage.totalEstimatedValue == null
                ? '—'
                : fcfa(garage.totalEstimatedValue, withSuffix: false),
            'valeur estimée',
          ),
        ],
      ),
    );
  }

  Widget _stat(String value, String label) => Expanded(
        child: Column(
          children: [
            Text(value,
                textAlign: TextAlign.center,
                style: const TextStyle(
                    color: AppColors.white,
                    fontWeight: FontWeight.w800,
                    fontSize: 18)),
            Text(label,
                textAlign: TextAlign.center,
                style: TextStyle(
                    color: AppColors.white.withValues(alpha: 0.8),
                    fontSize: 11)),
          ],
        ),
      );

  Widget _divider() => Container(
      width: 1, height: 34, color: AppColors.white.withValues(alpha: 0.25));
}

class _VehicleCard extends StatelessWidget {
  const _VehicleCard({required this.card, required this.onTap});
  final GarageVehicleCard card;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      margin: EdgeInsets.zero,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  if (card.primaryImageId != null) ...[
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: SizedBox(
                        width: 52,
                        height: 52,
                        child: GarageImage(imageId: card.primaryImageId!),
                      ),
                    ),
                    const SizedBox(width: 10),
                  ],
                  Expanded(
                    child: Text(card.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                            fontWeight: FontWeight.w800,
                            fontSize: 16,
                            color: AppColors.navy)),
                  ),
                  if (card.boughtOnYoonUAuto)
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: AppColors.azure.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: const Text('Acheté ici',
                          style: TextStyle(
                              fontSize: 10,
                              fontWeight: FontWeight.w700,
                              color: AppColors.azureDark)),
                    ),
                ],
              ),
              const SizedBox(height: 4),
              Text(
                [
                  '${card.year}',
                  if (card.mileage != null)
                    '${fcfa(card.mileage, withSuffix: false)} km',
                  if (card.registrationPlate != null) card.registrationPlate!,
                ].join(' · '),
                style: const TextStyle(fontSize: 13, color: AppColors.steel),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(child: _completeness(card.completenessScore)),
                  const SizedBox(width: 12),
                  Text(
                    card.estimatedValue == null
                        ? '—'
                        : fcfa(card.estimatedValue),
                    style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        color: AppColors.azureDark),
                  ),
                ],
              ),
              if (card.nextReminder != null) ...[
                const SizedBox(height: 12),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(
                    color: AppColors.frostDark,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.notifications_active_outlined,
                          size: 15, color: AppColors.steel),
                      const SizedBox(width: 6),
                      Expanded(
                        child: Text(
                          'Prochain rappel : ${card.nextReminder!.label}',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                              fontSize: 12, color: AppColors.navyDark),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _completeness(int score) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Dossier $score %',
            style: const TextStyle(fontSize: 11, color: AppColors.steel)),
        const SizedBox(height: 4),
        ClipRRect(
          borderRadius: BorderRadius.circular(4),
          child: LinearProgressIndicator(
            value: score / 100,
            minHeight: 6,
            backgroundColor: AppColors.frostDark,
            color: AppColors.azureDark,
          ),
        ),
      ],
    );
  }
}

class _EmptyGarage extends StatelessWidget {
  const _EmptyGarage();
  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.garage_outlined, size: 56, color: AppColors.silver),
            SizedBox(height: 16),
            Text('Votre garage est vide',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            SizedBox(height: 6),
            Text(
              'Ajoutez un véhicule pour suivre son entretien, ses rappels et sa valeur.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.steel),
            ),
          ],
        ),
      ),
    );
  }
}
