import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/providers/auth_providers.dart';
import '../data/vehicle_repository.dart';
import '../models/vehicle_detail.dart';
import '../models/vehicle_filters.dart';
import '../models/vehicle_make.dart';
import '../models/vehicle_summary.dart';

final vehicleRepositoryProvider = Provider<VehicleRepository>(
  (ref) => VehicleRepository(ref.watch(apiClientProvider)),
);

/// Marcas para el buscador (cacheadas por la propia API 24 h).
final makesProvider = FutureProvider<List<VehicleMake>>(
  (ref) => ref.watch(vehicleRepositoryProvider).getMakes(),
);

/// Ficha de un vehículo por slug. Registra la visualización de paso.
final vehicleDetailProvider =
    FutureProvider.family<VehicleDetail, String>((ref, slug) async {
  final repo = ref.watch(vehicleRepositoryProvider);
  final detail = await repo.getBySlug(slug);
  repo.registerView(detail.id);
  return detail;
});

// ─── Estado del Marketplace ────────────────────────────────────────────────

class MarketplaceState {
  final List<VehicleSummary> items;
  final VehicleFilters filters;
  final bool loading;
  final bool loadingMore;
  final bool error;
  final int page;
  final bool hasNext;
  final int totalCount;

  const MarketplaceState({
    required this.items,
    required this.filters,
    required this.loading,
    required this.loadingMore,
    required this.error,
    required this.page,
    required this.hasNext,
    required this.totalCount,
  });

  factory MarketplaceState.initial() => const MarketplaceState(
        items: [],
        filters: VehicleFilters(),
        loading: true,
        loadingMore: false,
        error: false,
        page: 0,
        hasNext: false,
        totalCount: 0,
      );

  MarketplaceState copyWith({
    List<VehicleSummary>? items,
    VehicleFilters? filters,
    bool? loading,
    bool? loadingMore,
    bool? error,
    int? page,
    bool? hasNext,
    int? totalCount,
  }) =>
      MarketplaceState(
        items: items ?? this.items,
        filters: filters ?? this.filters,
        loading: loading ?? this.loading,
        loadingMore: loadingMore ?? this.loadingMore,
        error: error ?? this.error,
        page: page ?? this.page,
        hasNext: hasNext ?? this.hasNext,
        totalCount: totalCount ?? this.totalCount,
      );
}

class MarketplaceController extends StateNotifier<MarketplaceState> {
  MarketplaceController(this._repo) : super(MarketplaceState.initial()) {
    load();
  }

  final VehicleRepository _repo;

  Future<void> load({VehicleFilters? filters}) async {
    final f = filters ?? state.filters;
    state = state.copyWith(
        loading: true, error: false, filters: f, items: [], page: 0);
    try {
      final res = await _repo.search(f, page: 1);
      state = state.copyWith(
        loading: false,
        items: res.items,
        page: res.page,
        hasNext: res.hasNextPage,
        totalCount: res.totalCount,
      );
    } catch (_) {
      state = state.copyWith(loading: false, error: true);
    }
  }

  Future<void> loadMore() async {
    if (state.loadingMore || state.loading || !state.hasNext) return;
    state = state.copyWith(loadingMore: true);
    try {
      final res = await _repo.search(state.filters, page: state.page + 1);
      state = state.copyWith(
        loadingMore: false,
        items: [...state.items, ...res.items],
        page: res.page,
        hasNext: res.hasNextPage,
        totalCount: res.totalCount,
      );
    } catch (_) {
      state = state.copyWith(loadingMore: false);
    }
  }

  void applyFilters(VehicleFilters f) => load(filters: f);

  void setSearch(String? term) =>
      load(filters: state.filters.copyWith(search: term));

  Future<void> refresh() => load();
}

final marketplaceControllerProvider =
    StateNotifierProvider<MarketplaceController, MarketplaceState>(
  (ref) => MarketplaceController(ref.watch(vehicleRepositoryProvider)),
);
