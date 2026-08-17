import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../auth/providers/auth_providers.dart';
import '../../favorites/providers/favorites_providers.dart';
import '../../notifications/ui/notification_bell.dart';
import '../models/vehicle_summary.dart';
import '../providers/vehicle_providers.dart';
import 'filters_sheet.dart';
import 'widgets/vehicle_card.dart';

/// «Véhicules» — el escaparate público. Búsqueda, filtros, ordenación y listado
/// paginado. Se puede navegar sin sesión; el corazón exige iniciarla.
class MarketplaceScreen extends ConsumerStatefulWidget {
  const MarketplaceScreen({super.key});

  @override
  ConsumerState<MarketplaceScreen> createState() => _MarketplaceScreenState();
}

class _MarketplaceScreenState extends ConsumerState<MarketplaceScreen> {
  final _searchCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollCtrl.addListener(_onScroll);
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _scrollCtrl.removeListener(_onScroll);
    _scrollCtrl.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollCtrl.position.pixels >=
        _scrollCtrl.position.maxScrollExtent - 400) {
      ref.read(marketplaceControllerProvider.notifier).loadMore();
    }
  }

  Future<void> _openFilters() async {
    final current = ref.read(marketplaceControllerProvider).filters;
    final result = await showModalBottomSheet<FiltersSheetResult>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => FiltersSheet(initial: current),
    );
    if (result != null) {
      ref.read(marketplaceControllerProvider.notifier).applyFilters(result.filters);
    }
  }

  Future<void> _toggleFavorite(VehicleSummary v) async {
    final auth = ref.read(authControllerProvider);
    if (auth is! Authenticated) {
      _promptLogin();
      return;
    }
    await ref.read(favoritesControllerProvider.notifier).toggle(v);
  }

  void _promptLogin() {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: const Text('Connectez-vous pour enregistrer des favoris.'),
        action: SnackBarAction(
            label: 'Se connecter', onPressed: () => context.push('/login')),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(marketplaceControllerProvider);
    final favIds = ref.watch(favoritesControllerProvider).ids;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Véhicules'),
        actions: [
          IconButton(
            tooltip: 'Trier',
            icon: const Icon(Icons.swap_vert),
            onPressed: () => _openSort(state.filters.sortBy, state.filters.sortDesc),
          ),
          const NotificationBell(),
        ],
      ),
      body: Column(
        children: [
          _SearchBar(
            controller: _searchCtrl,
            activeFilters: state.filters.activeCount,
            onSubmit: (term) => ref
                .read(marketplaceControllerProvider.notifier)
                .setSearch(term.trim().isEmpty ? null : term.trim()),
            onFiltersTap: _openFilters,
          ),
          _ResultsHeader(loading: state.loading, total: state.totalCount),
          Expanded(child: _buildBody(state, favIds)),
        ],
      ),
    );
  }

  Widget _buildBody(MarketplaceState state, Set<String> favIds) {
    if (state.loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.error) {
      return _Message(
        icon: Icons.cloud_off,
        title: 'Impossible de charger les annonces',
        subtitle: 'Vérifiez votre connexion et réessayez.',
        action: FilledButton(
          onPressed: () => ref.read(marketplaceControllerProvider.notifier).refresh(),
          child: const Text('Réessayer'),
        ),
      );
    }
    if (state.items.isEmpty) {
      return const _Message(
        icon: Icons.search_off,
        title: 'Aucun véhicule trouvé',
        subtitle: 'Modifiez vos filtres pour élargir la recherche.',
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(marketplaceControllerProvider.notifier).refresh(),
      child: ListView.separated(
        controller: _scrollCtrl,
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
        itemCount: state.items.length + (state.hasNext ? 1 : 0),
        separatorBuilder: (_, _) => const SizedBox(height: 14),
        itemBuilder: (context, index) {
          if (index >= state.items.length) {
            return const Padding(
              padding: EdgeInsets.symmetric(vertical: 16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          final v = state.items[index];
          return VehicleCard(
            vehicle: v,
            isFavorite: favIds.contains(v.id),
            onToggleFavorite: () => _toggleFavorite(v),
            onTap: () => context.push('/vehicules/${v.slug}'),
          );
        },
      ),
    );
  }

  void _openSort(String sortBy, bool sortDesc) {
    showModalBottomSheet<void>(
      context: context,
      builder: (_) {
        Widget option(String label, String by, bool desc) {
          final selected = sortBy == by && sortDesc == desc;
          return ListTile(
            title: Text(label),
            trailing: selected
                ? const Icon(Icons.check, color: AppColors.azureDark)
                : null,
            onTap: () {
              Navigator.pop(context);
              final f = ref.read(marketplaceControllerProvider).filters;
              ref.read(marketplaceControllerProvider.notifier).applyFilters(
                    f.copyWith(sortBy: by, sortDesc: desc),
                  );
            },
          );
        }

        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text('Trier par',
                    style:
                        TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
              ),
              option('Plus récents', 'createdAt', true),
              option('Prix croissant', 'price', false),
              option('Prix décroissant', 'price', true),
              option('Kilométrage', 'mileage', false),
              option('Année récente', 'year', true),
            ],
          ),
        );
      },
    );
  }
}

class _SearchBar extends StatelessWidget {
  const _SearchBar({
    required this.controller,
    required this.activeFilters,
    required this.onSubmit,
    required this.onFiltersTap,
  });

  final TextEditingController controller;
  final int activeFilters;
  final ValueChanged<String> onSubmit;
  final VoidCallback onFiltersTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: controller,
              textInputAction: TextInputAction.search,
              onSubmitted: onSubmit,
              decoration: InputDecoration(
                hintText: 'Marque, modèle…',
                prefixIcon: const Icon(Icons.search),
                isDense: true,
                suffixIcon: controller.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          controller.clear();
                          onSubmit('');
                        },
                      ),
              ),
            ),
          ),
          const SizedBox(width: 8),
          Badge(
            isLabelVisible: activeFilters > 0,
            label: Text('$activeFilters'),
            child: IconButton.filledTonal(
              onPressed: onFiltersTap,
              icon: const Icon(Icons.tune),
              tooltip: 'Filtres',
            ),
          ),
        ],
      ),
    );
  }
}

class _ResultsHeader extends StatelessWidget {
  const _ResultsHeader({required this.loading, required this.total});
  final bool loading;
  final int total;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 4, 16, 4),
        child: Text(
          loading
              ? 'Recherche…'
              : '$total véhicule${total > 1 ? 's' : ''} disponible${total > 1 ? 's' : ''}',
          style: const TextStyle(
              color: AppColors.steel, fontSize: 13, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}

class _Message extends StatelessWidget {
  const _Message({
    required this.icon,
    required this.title,
    required this.subtitle,
    this.action,
  });
  final IconData icon;
  final String title;
  final String subtitle;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: AppColors.silver),
            const SizedBox(height: 16),
            Text(title,
                textAlign: TextAlign.center,
                style: const TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            const SizedBox(height: 6),
            Text(subtitle,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.steel)),
            if (action != null) ...[const SizedBox(height: 20), action!],
          ],
        ),
      ),
    );
  }
}
