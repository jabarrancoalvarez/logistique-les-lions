import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../vehicles/models/vehicle_enums.dart';
import '../models/garage_enums.dart';
import '../models/garage_models.dart';
import '../models/valuation.dart';
import '../providers/garage_providers.dart';
import 'widgets/garage_image.dart';

/// Ficha de un vehículo de Mon Garage: datos, valeur, complétude y accesos a
/// entretien y rappels, además de vendre / modifier / supprimer.
class GarageVehicleScreen extends ConsumerWidget {
  const GarageVehicleScreen({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(garageVehicleProvider(id));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Véhicule'),
        actions: [
          async.maybeWhen(
            data: (v) => PopupMenuButton<String>(
              onSelected: (choice) => _onMenu(context, ref, v, choice),
              itemBuilder: (_) => [
                const PopupMenuItem(value: 'edit', child: Text('Modifier')),
                const PopupMenuItem(
                    value: 'delete', child: Text('Supprimer')),
              ],
            ),
            orElse: () => const SizedBox.shrink(),
          ),
        ],
      ),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(garageVehicleProvider(id)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (v) => _Body(vehicle: v),
      ),
    );
  }

  Future<void> _onMenu(
      BuildContext context, WidgetRef ref, GarageVehicleDetail v, String c) async {
    if (c == 'edit') {
      await context.push('/garage/${v.id}/modifier');
      ref.invalidate(garageVehicleProvider(v.id));
    } else if (c == 'delete') {
      final ok = await showDialog<bool>(
        context: context,
        builder: (_) => AlertDialog(
          title: const Text('Retirer ce véhicule ?'),
          content: const Text(
              'Il sera retiré de Mon Garage avec son historique privé.'),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Annuler')),
            FilledButton(
                style: FilledButton.styleFrom(backgroundColor: AppColors.error),
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Supprimer')),
          ],
        ),
      );
      if (ok == true) {
        try {
          await ref.read(garageRepositoryProvider).deleteVehicle(v.id);
          if (context.mounted) context.pop();
        } catch (_) {
          if (context.mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Suppression impossible.')),
            );
          }
        }
      }
    }
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.vehicle});
  final GarageVehicleDetail vehicle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final v = vehicle;

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
      children: [
        Text(v.title,
            style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w800,
                color: AppColors.navy)),
        const SizedBox(height: 8),
        if (v.boughtOnYoonUAuto)
          const _Tag(
              icon: Icons.verified, text: 'Acheté sur Yoon u Auto', color: AppColors.azureDark),
        if (v.listedVehicleId != null)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: InkWell(
              onTap: v.listedVehicleSlug == null
                  ? null
                  : () => context.push('/vehicules/${v.listedVehicleSlug}'),
              child: _Tag(
                icon: Icons.sell_outlined,
                text:
                    'En vente (${statusLabel(v.listedVehicleStatus)}) — voir l’annonce',
                color: AppColors.warning,
              ),
            ),
          ),
        const SizedBox(height: 16),
        _PhotosStrip(vehicle: v),
        const SizedBox(height: 16),
        _specs(v),
        if (v.purchaseDate != null || v.purchasePrice != null) ...[
          const SizedBox(height: 8),
          _PurchaseCard(vehicle: v),
        ],
        const SizedBox(height: 16),
        _ValuationCard(vehicleId: v.id),
        const SizedBox(height: 12),
        _CompletenessCard(vehicleId: v.id),
        const SizedBox(height: 20),
        _NavTile(
          icon: Icons.build_outlined,
          title: 'Entretien',
          subtitle: 'Historique des interventions',
          onTap: () async {
            await context.push('/garage/${v.id}/entretien');
            ref.invalidate(completenessProvider(v.id));
          },
        ),
        _NavTile(
          icon: Icons.notifications_outlined,
          title: 'Rappels',
          subtitle: 'Échéances à venir',
          onTap: () async {
            await context.push('/garage/${v.id}/rappels');
            ref.invalidate(completenessProvider(v.id));
          },
        ),
        _NavTile(
          icon: Icons.folder_outlined,
          title: 'Documents',
          subtitle: 'Carte grise, factures, assurance…',
          onTap: () async {
            await context.push('/garage/${v.id}/documents');
            ref.invalidate(completenessProvider(v.id));
          },
        ),
        if (v.listedVehicleId != null)
          _NavTile(
            icon: Icons.visibility_outlined,
            title: 'Transparence',
            subtitle: 'Ce que l’annonce partage de l’historique',
            onTap: () =>
                context.push('/garage/transparence/${v.listedVehicleId}'),
          ),
        const SizedBox(height: 20),
        FilledButton.icon(
          onPressed: v.listedVehicleId != null ? null : () => _sell(context, ref),
          icon: const Icon(Icons.sell_outlined),
          label: Text(v.listedVehicleId != null
              ? 'Déjà mis en vente'
              : 'Vendre ce véhicule'),
          style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
        ),
      ],
    );
  }

  Future<void> _sell(BuildContext context, WidgetRef ref) async {
    try {
      final slug = await ref.read(garageRepositoryProvider).sell(vehicle.id);
      if (!context.mounted) return;
      ref.invalidate(garageVehicleProvider(vehicle.id));
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Brouillon d’annonce créé. À compléter avant publication.')),
      );
      context.push('/vehicules/$slug');
    } catch (_) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Impossible de créer l’annonce.')),
      );
    }
  }

  Widget _specs(GarageVehicleDetail v) {
    final rows = <(String, String)>[
      ('Année', '${v.year}'),
      if (v.mileage != null)
        ('Kilométrage', '${fcfa(v.mileage, withSuffix: false)} km'),
      if (v.fuelType != null) ('Carburant', fuelLabel(v.fuelType)),
      if (v.transmission != null) ('Boîte', transmissionLabel(v.transmission)),
      if (v.bodyType != null) ('Carrosserie', bodyLabel(v.bodyType)),
      if (v.powerCv != null) ('Puissance', '${v.powerCv} ch'),
      if (v.engineDisplacementCc != null)
        ('Cylindrée', '${v.engineDisplacementCc} cm³'),
      if (v.color != null && v.color!.isNotEmpty) ('Couleur', v.color!),
      if (v.registrationPlate != null)
        ('Immatriculation', v.registrationPlate!),
      if (v.vin != null) ('VIN', v.vin!),
    ];
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.frost,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.frostDark),
      ),
      child: Column(
        children: [
          for (final (label, value) in rows)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(
                children: [
                  SizedBox(
                      width: 120,
                      child: Text(label,
                          style: const TextStyle(
                              color: AppColors.steel, fontSize: 13))),
                  Expanded(
                    child: Text(value,
                        style: const TextStyle(
                            fontWeight: FontWeight.w600,
                            color: AppColors.navyDark,
                            fontSize: 13)),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _PhotosStrip extends ConsumerWidget {
  const _PhotosStrip({required this.vehicle});
  final GarageVehicleDetail vehicle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return SizedBox(
      height: 96,
      child: ListView(
        scrollDirection: Axis.horizontal,
        children: [
          _AddPhotoButton(onTap: () => _add(context, ref)),
          for (final id in vehicle.imageIds)
            Padding(
              padding: const EdgeInsets.only(left: 10),
              child: GestureDetector(
                onLongPress: () => _delete(context, ref, id),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(10),
                  child: SizedBox(
                      width: 128, height: 96, child: GarageImage(imageId: id)),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _add(BuildContext context, WidgetRef ref) async {
    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      builder: (_) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Prendre une photo'),
              onTap: () => Navigator.pop(context, ImageSource.camera),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Choisir dans la galerie'),
              onTap: () => Navigator.pop(context, ImageSource.gallery),
            ),
          ],
        ),
      ),
    );
    if (source == null) return;

    final picked = await ImagePicker()
        .pickImage(source: source, maxWidth: 2000, imageQuality: 85);
    if (picked == null) return;
    final bytes = await picked.readAsBytes();
    try {
      await ref.read(garageRepositoryProvider).uploadVehicleImage(
            vehicle.id,
            bytes: bytes,
            filename: picked.name,
            contentType: _contentType(picked.name, picked.mimeType),
            isPrimary: vehicle.imageIds.isEmpty,
          );
      ref.invalidate(garageVehicleProvider(vehicle.id));
      ref.invalidate(garageSummaryProvider);
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Envoi de la photo impossible.')),
        );
      }
    }
  }

  Future<void> _delete(BuildContext context, WidgetRef ref, String id) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Supprimer la photo ?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Annuler')),
          FilledButton(
              style: FilledButton.styleFrom(backgroundColor: AppColors.error),
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Supprimer')),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(garageRepositoryProvider).deleteVehicleImage(id);
      ref.invalidate(garageVehicleProvider(vehicle.id));
      ref.invalidate(garageSummaryProvider);
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Suppression impossible.')),
        );
      }
    }
  }

  static String _contentType(String name, String? mime) {
    if (mime != null && mime.isNotEmpty) return mime;
    final n = name.toLowerCase();
    if (n.endsWith('.png')) return 'image/png';
    if (n.endsWith('.webp')) return 'image/webp';
    return 'image/jpeg';
  }
}

class _AddPhotoButton extends StatelessWidget {
  const _AddPhotoButton({required this.onTap});
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(10),
      child: Container(
        width: 96,
        height: 96,
        decoration: BoxDecoration(
          color: AppColors.frost,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: AppColors.silver),
        ),
        child: const Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.add_a_photo_outlined, color: AppColors.azureDark),
            SizedBox(height: 4),
            Text('Ajouter',
                style: TextStyle(fontSize: 11, color: AppColors.steel)),
          ],
        ),
      ),
    );
  }
}

class _PurchaseCard extends StatelessWidget {
  const _PurchaseCard({required this.vehicle});
  final GarageVehicleDetail vehicle;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.frost,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.frostDark),
      ),
      child: Row(
        children: [
          const Icon(Icons.shopping_bag_outlined, color: AppColors.azureDark),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Achat',
                    style: TextStyle(fontSize: 12, color: AppColors.steel)),
                Text(
                  [
                    if (vehicle.purchaseDate != null)
                      DateFormat('d MMM yyyy', 'fr')
                          .format(vehicle.purchaseDate!.toLocal()),
                    if (vehicle.purchasePrice != null)
                      fcfa(vehicle.purchasePrice),
                  ].join(' · '),
                  style: const TextStyle(
                      fontWeight: FontWeight.w700, color: AppColors.navy),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ValuationCard extends ConsumerWidget {
  const _ValuationCard({required this.vehicleId});
  final String vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(valuationProvider(vehicleId));
    return _card(
      title: 'Valeur estimée',
      child: async.when(
        loading: () => const _CardLoading(),
        error: (_, _) => const Text('Indisponible',
            style: TextStyle(color: AppColors.steel)),
        data: (val) => _content(val),
      ),
    );
  }

  Widget _content(Valuation val) {
    if (!val.hasEstimate || val.estimatedValue == null) {
      return const Text(
        'Pas assez d’annonces comparables pour estimer une valeur.',
        style: TextStyle(color: AppColors.steel, fontSize: 13),
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(fcfa(val.estimatedValue),
            style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w900,
                color: AppColors.azureDark)),
        if (val.lowValue != null && val.highValue != null)
          Text('Fourchette : ${fcfa(val.lowValue)} – ${fcfa(val.highValue)}',
              style: const TextStyle(fontSize: 12, color: AppColors.steel)),
        Text('Basé sur ${val.comparableCount} annonces comparables',
            style: const TextStyle(fontSize: 11, color: AppColors.steel)),
        if (val.evolution?.changePercent != null)
          Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Row(
              children: [
                Icon(
                    val.evolution!.changePercent! >= 0
                        ? Icons.trending_up
                        : Icons.trending_down,
                    size: 16,
                    color: val.evolution!.changePercent! >= 0
                        ? AppColors.success
                        : AppColors.error),
                const SizedBox(width: 4),
                Text(
                  '${val.evolution!.changePercent!.toStringAsFixed(1)} % sur ${val.evolution!.monthsCovered} mois',
                  style: const TextStyle(fontSize: 12, color: AppColors.steel),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

class _CompletenessCard extends ConsumerWidget {
  const _CompletenessCard({required this.vehicleId});
  final String vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(completenessProvider(vehicleId));
    return _card(
      title: 'Complétude du dossier',
      child: async.when(
        loading: () => const _CardLoading(),
        error: (_, _) => const Text('Indisponible',
            style: TextStyle(color: AppColors.steel)),
        data: (comp) => Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Text('${comp.score} %',
                    style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w900,
                        color: AppColors.navy)),
                const SizedBox(width: 8),
                Text('· ${completenessLevelLabel(comp.level)}',
                    style: const TextStyle(color: AppColors.steel)),
              ],
            ),
            const SizedBox(height: 8),
            ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(
                value: comp.score / 100,
                minHeight: 6,
                backgroundColor: AppColors.frostDark,
                color: AppColors.azureDark,
              ),
            ),
            const SizedBox(height: 10),
            for (final it in comp.items)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 2),
                child: Row(
                  children: [
                    Icon(
                      it.status == 'Complete'
                          ? Icons.check_circle
                          : it.status == 'Partial'
                              ? Icons.remove_circle_outline
                              : Icons.radio_button_unchecked,
                      size: 16,
                      color: it.status == 'Complete'
                          ? AppColors.success
                          : it.status == 'Partial'
                              ? AppColors.warning
                              : AppColors.silver,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(completenessCheckLabel(it.check),
                          style: const TextStyle(fontSize: 13)),
                    ),
                    if (it.detail != null)
                      Text('${it.detail}',
                          style: const TextStyle(
                              fontSize: 12, color: AppColors.steel)),
                  ],
                ),
              ),
            const SizedBox(height: 6),
            const Text(
              'Mesure l’historique numérique, pas l’état mécanique du véhicule.',
              style: TextStyle(fontSize: 11, color: AppColors.steel),
            ),
          ],
        ),
      ),
    );
  }
}

Widget _card({required String title, required Widget child}) => Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.frostDark),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title,
              style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  color: AppColors.navy,
                  fontSize: 15)),
          const SizedBox(height: 8),
          child,
        ],
      ),
    );

class _CardLoading extends StatelessWidget {
  const _CardLoading();
  @override
  Widget build(BuildContext context) => const SizedBox(
        height: 20,
        width: 20,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
}

class _NavTile extends StatelessWidget {
  const _NavTile({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });
  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: Icon(icon, color: AppColors.azureDark),
        title: Text(title,
            style: const TextStyle(fontWeight: FontWeight.w700)),
        subtitle: Text(subtitle),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}

class _Tag extends StatelessWidget {
  const _Tag({required this.icon, required this.text, required this.color});
  final IconData icon;
  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 15, color: color),
          const SizedBox(width: 6),
          Flexible(
            child: Text(text,
                style: TextStyle(
                    fontSize: 12, fontWeight: FontWeight.w600, color: color)),
          ),
        ],
      ),
    );
  }
}
