import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/providers/auth_providers.dart';
import '../../vehicles/models/vehicle_summary.dart';
import '../../vehicles/providers/vehicle_providers.dart';

/// Estado de «Favoris». Guarda los ids marcados (para pintar el corazón en las
/// tarjetas) y la lista completa (para la pantalla de favoritos).
class FavoritesState {
  final Set<String> ids;
  final List<VehicleSummary> items;
  final bool loading;
  final bool loaded;

  const FavoritesState({
    required this.ids,
    required this.items,
    required this.loading,
    required this.loaded,
  });

  const FavoritesState.empty()
      : ids = const {},
        items = const [],
        loading = false,
        loaded = false;

  FavoritesState copyWith({
    Set<String>? ids,
    List<VehicleSummary>? items,
    bool? loading,
    bool? loaded,
  }) =>
      FavoritesState(
        ids: ids ?? this.ids,
        items: items ?? this.items,
        loading: loading ?? this.loading,
        loaded: loaded ?? this.loaded,
      );
}

class FavoritesController extends StateNotifier<FavoritesState> {
  FavoritesController(this._ref) : super(const FavoritesState.empty());

  final Ref _ref;

  bool isFavorite(String id) => state.ids.contains(id);

  /// Carga la lista una vez. Segura de llamar varias veces.
  Future<void> ensureLoaded() async {
    if (state.loaded || state.loading) return;
    await load();
  }

  Future<void> load() async {
    state = state.copyWith(loading: true);
    try {
      final items = await _ref.read(vehicleRepositoryProvider).getFavorites();
      state = FavoritesState(
        ids: items.map((e) => e.id).toSet(),
        items: items,
        loading: false,
        loaded: true,
      );
    } catch (_) {
      state = state.copyWith(loading: false, loaded: true);
    }
  }

  /// Alterna un favorito. Devuelve el nuevo estado (`true` = guardado).
  Future<bool> toggle(VehicleSummary vehicle) async {
    final saved =
        await _ref.read(vehicleRepositoryProvider).toggleFavorite(vehicle.id);
    final ids = {...state.ids};
    var items = [...state.items];
    if (saved) {
      ids.add(vehicle.id);
      if (!items.any((e) => e.id == vehicle.id)) items.insert(0, vehicle);
    } else {
      ids.remove(vehicle.id);
      items = items.where((e) => e.id != vehicle.id).toList();
    }
    state = state.copyWith(ids: ids, items: items, loaded: true);
    return saved;
  }

  /// Alterna por id (desde la ficha, donde no hay un [VehicleSummary]). La lista
  /// completa se recargará la próxima vez que se abra Favoris.
  Future<bool> toggleById(String vehicleId) async {
    final saved =
        await _ref.read(vehicleRepositoryProvider).toggleFavorite(vehicleId);
    final ids = {...state.ids};
    if (saved) {
      ids.add(vehicleId);
    } else {
      ids.remove(vehicleId);
    }
    state = state.copyWith(
      ids: ids,
      items: state.items.where((e) => ids.contains(e.id)).toList(),
      loaded: false,
    );
    return saved;
  }

  void reset() => state = const FavoritesState.empty();
}

final favoritesControllerProvider =
    StateNotifierProvider<FavoritesController, FavoritesState>((ref) {
  final controller = FavoritesController(ref);
  // Al cerrar sesión, se vacían; al iniciarla, se recargan.
  ref.listen(authControllerProvider, (previous, next) {
    if (next is Unauthenticated) {
      controller.reset();
    } else if (next is Authenticated && previous is! Authenticated) {
      controller.load();
    }
  });
  return controller;
});
