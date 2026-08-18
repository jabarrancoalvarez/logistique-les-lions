import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../vehicles/models/featured_vehicle.dart';
import '../../vehicles/providers/vehicle_providers.dart';

/// Sección «À la une» de la portada: carrusel horizontal de anuncios destacados.
/// La API los baraja (rotación equitativa); aquí se recorren deslizando. Si no hay
/// ninguno, la sección se oculta por completo.
class FeaturedCarousel extends ConsumerWidget {
  const FeaturedCarousel({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final featured = ref.watch(featuredVehiclesProvider);

    return featured.maybeWhen(
      data: (list) => list.isEmpty ? const SizedBox.shrink() : _section(context, list),
      orElse: () => const SizedBox.shrink(),
    );
  }

  Widget _section(BuildContext context, List<FeaturedVehicle> list) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 4, 20, 10),
          child: Row(
            children: [
              const Icon(Icons.star, size: 18, color: AppColors.azureDark),
              const SizedBox(width: 6),
              const Text(
                'À la une',
                style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                    color: AppColors.navy),
              ),
              const Spacer(),
              TextButton(
                onPressed: () => context.push('/vehicules'),
                child: const Text('Voir tout'),
              ),
            ],
          ),
        ),
        SizedBox(
          height: 214,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 20),
            itemCount: list.length,
            separatorBuilder: (_, _) => const SizedBox(width: 12),
            itemBuilder: (context, i) => _FeaturedCard(vehicle: list[i]),
          ),
        ),
        const SizedBox(height: 24),
      ],
    );
  }
}

class _FeaturedCard extends StatelessWidget {
  const _FeaturedCard({required this.vehicle});
  final FeaturedVehicle vehicle;

  @override
  Widget build(BuildContext context) {
    final subtitle = [
      vehicle.year.toString(),
      if (vehicle.mileage != null) '${fcfa(vehicle.mileage, withSuffix: false)} km',
    ].join(' · ');

    return SizedBox(
      width: 236,
      child: Card(
        clipBehavior: Clip.antiAlias,
        margin: EdgeInsets.zero,
        child: InkWell(
          onTap: () => context.push('/vehicules/${vehicle.slug}'),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AspectRatio(
                aspectRatio: 16 / 10,
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    if (vehicle.coverImage != null)
                      Image.network(vehicle.coverImage!, fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => _placeholder(),
                          loadingBuilder: (c, child, p) =>
                              p == null ? child : _placeholder())
                    else
                      _placeholder(),
                    Positioned(
                      top: 8,
                      left: 8,
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.azureDark,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.star, size: 12, color: Colors.white),
                            SizedBox(width: 3),
                            Text('À la une',
                                style: TextStyle(
                                    color: Colors.white,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w700)),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(12, 10, 12, 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(vehicle.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 14,
                            color: AppColors.navy)),
                    const SizedBox(height: 2),
                    Text(subtitle,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                            fontSize: 12, color: AppColors.steel)),
                    const SizedBox(height: 6),
                    Text(fcfa(vehicle.price),
                        style: const TextStyle(
                            fontWeight: FontWeight.w800,
                            fontSize: 15,
                            color: AppColors.azureDark)),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _placeholder() => Container(
        color: AppColors.frostDark,
        alignment: Alignment.center,
        child: const Icon(Icons.directions_car_outlined,
            size: 40, color: AppColors.silver),
      );
}
