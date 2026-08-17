import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../auth/providers/auth_providers.dart';
import '../../vehicles/ui/widgets/vehicle_card.dart';
import '../providers/favorites_providers.dart';

/// «Favoris». Requiere sesión; lista los anuncios guardados por el usuario.
class FavoritesScreen extends ConsumerStatefulWidget {
  const FavoritesScreen({super.key});

  @override
  ConsumerState<FavoritesScreen> createState() => _FavoritesScreenState();
}

class _FavoritesScreenState extends ConsumerState<FavoritesScreen> {
  @override
  void initState() {
    super.initState();
    // Carga diferida al primer montaje, si hay sesión.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (ref.read(authControllerProvider) is Authenticated) {
        ref.read(favoritesControllerProvider.notifier).ensureLoaded();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Favoris')),
      body: auth is! Authenticated ? _guestView() : _listView(),
    );
  }

  Widget _guestView() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.favorite_border, size: 56, color: AppColors.silver),
            const SizedBox(height: 16),
            const Text('Vos véhicules favoris',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            const SizedBox(height: 6),
            const Text(
              'Connectez-vous pour enregistrer des annonces et les retrouver ici.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.steel),
            ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: () => context.push('/login'),
              child: const Text('Se connecter'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _listView() {
    final state = ref.watch(favoritesControllerProvider);

    if (state.loading && state.items.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.items.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.favorite_border,
                  size: 56, color: AppColors.silver),
              const SizedBox(height: 16),
              const Text('Aucun favori pour le moment',
                  style: TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 16,
                      color: AppColors.navy)),
              const SizedBox(height: 6),
              const Text('Touchez le cœur sur une annonce pour l’enregistrer.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: AppColors.steel)),
              const SizedBox(height: 20),
              OutlinedButton(
                onPressed: () => context.go('/vehicules'),
                child: const Text('Parcourir les véhicules'),
              ),
            ],
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(favoritesControllerProvider.notifier).load(),
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
        itemCount: state.items.length,
        separatorBuilder: (_, _) => const SizedBox(height: 14),
        itemBuilder: (_, i) {
          final v = state.items[i];
          return VehicleCard(
            vehicle: v,
            isFavorite: true,
            onToggleFavorite: () =>
                ref.read(favoritesControllerProvider.notifier).toggle(v),
            onTap: () => context.push('/vehicules/${v.slug}'),
          );
        },
      ),
    );
  }
}
