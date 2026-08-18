import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/util/fcfa.dart';
import '../../models/vehicle_enums.dart';
import '../../models/vehicle_summary.dart';

/// Tarjeta de anuncio del Marketplace y de Favoris.
class VehicleCard extends StatelessWidget {
  const VehicleCard({
    super.key,
    required this.vehicle,
    required this.onTap,
    required this.isFavorite,
    required this.onToggleFavorite,
  });

  final VehicleSummary vehicle;
  final VoidCallback onTap;
  final bool isFavorite;
  final VoidCallback onToggleFavorite;

  @override
  Widget build(BuildContext context) {
    final indicator = priceIndicatorStyle(vehicle.priceIndicator);
    final location =
        [vehicle.city, vehicle.region].where((e) => e != null && e.isNotEmpty).join(', ');

    return Card(
      clipBehavior: Clip.antiAlias,
      margin: EdgeInsets.zero,
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _Cover(
              imageUrl: vehicle.coverImage,
              imageCount: vehicle.imageCount,
              isFavorite: isFavorite,
              onToggleFavorite: onToggleFavorite,
              statusBadge: vehicle.status == 'Reserve'
                  ? 'Réservé'
                  : vehicle.status == 'Vendu'
                      ? 'Vendu'
                      : null,
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 10, 12, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    vehicle.title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                        fontWeight: FontWeight.w700,
                        fontSize: 15,
                        color: AppColors.navy),
                  ),
                  const SizedBox(height: 6),
                  _SpecsLine(vehicle: vehicle),
                  const SizedBox(height: 10),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Expanded(
                        child: Text(
                          fcfa(vehicle.price),
                          style: const TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 17,
                              color: AppColors.azureDark),
                        ),
                      ),
                      if (indicator != null)
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 8, vertical: 3),
                          decoration: BoxDecoration(
                            color: indicator.background,
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(indicator.icon,
                                  size: 13, color: indicator.color),
                              const SizedBox(width: 4),
                              Text(indicator.label,
                                  style: TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w600,
                                      color: indicator.color)),
                            ],
                          ),
                        ),
                    ],
                  ),
                  if (location.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        const Icon(Icons.place_outlined,
                            size: 14, color: AppColors.steel),
                        const SizedBox(width: 3),
                        Expanded(
                          child: Text(location,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                  fontSize: 12, color: AppColors.steel)),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Cover extends StatelessWidget {
  const _Cover({
    required this.imageUrl,
    required this.imageCount,
    required this.isFavorite,
    required this.onToggleFavorite,
    this.statusBadge,
  });

  final String? imageUrl;
  final int imageCount;
  final bool isFavorite;
  final VoidCallback onToggleFavorite;
  final String? statusBadge;

  @override
  Widget build(BuildContext context) {
    return AspectRatio(
      aspectRatio: 16 / 10,
      child: Stack(
        fit: StackFit.expand,
        children: [
          if (imageUrl != null)
            Image.network(
              imageUrl!,
              fit: BoxFit.cover,
              errorBuilder: (_, _, _) => const _CoverPlaceholder(),
              loadingBuilder: (context, child, progress) =>
                  progress == null ? child : const _CoverPlaceholder(),
            )
          else
            const _CoverPlaceholder(),
          if (statusBadge != null)
            Positioned(
              top: 8,
              left: 8,
              child: Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.navy.withValues(alpha: 0.9),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(statusBadge!,
                    style: const TextStyle(
                        color: AppColors.white,
                        fontSize: 11,
                        fontWeight: FontWeight.w700)),
              ),
            ),
          Positioned(
            top: 4,
            right: 4,
            child: Material(
              color: Colors.white.withValues(alpha: 0.85),
              shape: const CircleBorder(),
              child: InkWell(
                customBorder: const CircleBorder(),
                onTap: onToggleFavorite,
                child: Padding(
                  padding: const EdgeInsets.all(6),
                  child: Icon(
                    isFavorite ? Icons.favorite : Icons.favorite_border,
                    size: 20,
                    color: isFavorite ? AppColors.error : AppColors.steel,
                  ),
                ),
              ),
            ),
          ),
          if (imageCount > 1)
            Positioned(
              bottom: 8,
              right: 8,
              child: Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                decoration: BoxDecoration(
                  color: Colors.black.withValues(alpha: 0.55),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.photo_library_outlined,
                        size: 12, color: Colors.white),
                    const SizedBox(width: 3),
                    Text('$imageCount',
                        style: const TextStyle(
                            color: Colors.white, fontSize: 11)),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _CoverPlaceholder extends StatelessWidget {
  const _CoverPlaceholder();

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.frostDark,
      child: const Center(
        child: Icon(Icons.directions_car_outlined,
            size: 40, color: AppColors.silver),
      ),
    );
  }
}

class _SpecsLine extends StatelessWidget {
  const _SpecsLine({required this.vehicle});
  final VehicleSummary vehicle;

  @override
  Widget build(BuildContext context) {
    final parts = <String>[
      '${vehicle.year}',
      if (vehicle.mileage != null) '${fcfa(vehicle.mileage, withSuffix: false)} km',
      if (vehicle.fuelType != null) fuelLabel(vehicle.fuelType),
      if (vehicle.transmission != null) transmissionLabel(vehicle.transmission),
    ];
    return Text(
      parts.join(' · '),
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
      style: const TextStyle(fontSize: 12.5, color: AppColors.steel),
    );
  }
}
