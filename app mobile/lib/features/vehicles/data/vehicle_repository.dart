import '../../../core/models/paged_result.dart';
import '../../../core/network/api_client.dart';
import '../models/vehicle_detail.dart';
import '../models/vehicle_filters.dart';
import '../models/vehicle_make.dart';
import '../models/vehicle_summary.dart';

/// Acceso al Marketplace (mismos endpoints que la web).
class VehicleRepository {
  VehicleRepository(this._api);

  final ApiClient _api;

  /// Listado paginado con filtros. `GET /vehicles`.
  Future<PagedResult<VehicleSummary>> search(
    VehicleFilters filters, {
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await _api.dio.get('/vehicles', queryParameters: {
      ...filters.toQueryParameters(),
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromJson(
      res.data as Map<String, dynamic>,
      VehicleSummary.fromJson,
    );
  }

  /// Ficha completa por slug. `GET /vehicles/{slug}`.
  Future<VehicleDetail> getBySlug(String slug) async {
    final res = await _api.dio.get('/vehicles/$slug');
    return VehicleDetail.fromJson(res.data as Map<String, dynamic>);
  }

  /// Marcas para el buscador. `GET /vehicles/makes`.
  Future<List<VehicleMake>> getMakes() async {
    final res = await _api.dio.get('/vehicles/makes');
    return (res.data as List<dynamic>)
        .map((e) => VehicleMake.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Alterna un favorito y devuelve el nuevo estado. `POST /vehicles/{id}/favorite`.
  Future<bool> toggleFavorite(String vehicleId) async {
    final res = await _api.dio.post('/vehicles/$vehicleId/favorite');
    return (res.data as Map<String, dynamic>)['isSaved'] as bool? ?? false;
  }

  /// Favoritos del usuario. `GET /vehicles/favorites`.
  Future<List<VehicleSummary>> getFavorites() async {
    final res = await _api.dio.get('/vehicles/favorites');
    final data = res.data;
    // El endpoint puede devolver una lista o un objeto con `items`.
    final list = data is List
        ? data
        : (data as Map<String, dynamic>)['items'] as List<dynamic>? ?? const [];
    return list
        .map((e) => VehicleSummary.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Registra una visualización (informativo). `POST /vehicles/{id}/view`.
  Future<void> registerView(String vehicleId) async {
    try {
      await _api.dio.post('/vehicles/$vehicleId/view');
    } catch (_) {
      // El contador nunca debe romper la carga de la ficha.
    }
  }
}
