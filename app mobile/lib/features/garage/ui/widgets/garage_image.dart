import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/theme/app_colors.dart';
import '../../providers/garage_providers.dart';

/// Muestra una foto **privada** del garaje: se descarga por endpoint autenticado
/// y se pinta desde memoria. Nunca hay URL pública.
class GarageImage extends ConsumerWidget {
  const GarageImage({super.key, required this.imageId, this.fit = BoxFit.cover});
  final String imageId;
  final BoxFit fit;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(garageImageBytesProvider(imageId));
    return async.when(
      loading: () => const _Placeholder(loading: true),
      error: (_, _) => const _Placeholder(loading: false),
      data: (bytes) => bytes.isEmpty
          ? const _Placeholder(loading: false)
          : Image.memory(Uint8List.fromList(bytes),
              fit: fit,
              gaplessPlayback: true,
              errorBuilder: (_, _, _) => const _Placeholder(loading: false)),
    );
  }
}

class _Placeholder extends StatelessWidget {
  const _Placeholder({required this.loading});
  final bool loading;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: AppColors.frostDark,
      child: Center(
        child: loading
            ? const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2))
            : const Icon(Icons.directions_car_outlined,
                color: AppColors.silver),
      ),
    );
  }
}
