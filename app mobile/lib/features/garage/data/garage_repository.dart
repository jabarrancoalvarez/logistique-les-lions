import '../../../core/network/api_client.dart';
import '../models/garage_models.dart';
import '../models/maintenance.dart';
import '../models/reminder.dart';
import '../models/valuation.dart';

/// Acceso a Mon Garage (privado). Todos los endpoints salen del JWT y filtran
/// por usuario en el backend. Devuelven el valor directo (sin envoltorio Result).
class GarageRepository {
  GarageRepository(this._api);
  final ApiClient _api;

  // ─── Vehículos ──────────────────────────────────────────────────────────

  Future<GarageSummary> getGarage() async {
    final res = await _api.dio.get('/garage');
    return GarageSummary.fromJson(res.data as Map<String, dynamic>);
  }

  Future<GarageVehicleDetail> getVehicle(String id) async {
    final res = await _api.dio.get('/garage/$id');
    return GarageVehicleDetail.fromJson(res.data as Map<String, dynamic>);
  }

  Future<String> createVehicle(Map<String, dynamic> body) async {
    final res = await _api.dio.post('/garage', data: body);
    return (res.data as Map<String, dynamic>)['id'] as String;
  }

  Future<void> updateVehicle(String id, Map<String, dynamic> body) =>
      _api.dio.put('/garage/$id', data: body);

  Future<void> deleteVehicle(String id) => _api.dio.delete('/garage/$id');

  /// «Vendre ce véhicule» → crea un borrador de anuncio. Devuelve el slug.
  Future<String> sell(String id) async {
    final res = await _api.dio.post('/garage/$id/sell');
    return (res.data as Map<String, dynamic>)['slug'] as String;
  }

  // ─── Entretien ──────────────────────────────────────────────────────────

  Future<MaintenanceHistory> getMaintenance(String vehicleId) async {
    final res = await _api.dio.get('/garage/$vehicleId/maintenance');
    return MaintenanceHistory.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> addMaintenance(String vehicleId, Map<String, dynamic> body) =>
      _api.dio.post('/garage/$vehicleId/maintenance', data: body);

  Future<void> updateMaintenance(String recordId, Map<String, dynamic> body) =>
      _api.dio.put('/garage/maintenance/$recordId', data: body);

  Future<void> deleteMaintenance(String recordId) =>
      _api.dio.delete('/garage/maintenance/$recordId');

  // ─── Rappels ────────────────────────────────────────────────────────────

  Future<List<Reminder>> getReminders(String vehicleId) async {
    final res = await _api.dio.get('/garage/$vehicleId/reminders');
    return (res.data as List<dynamic>)
        .map((e) => Reminder.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<UpcomingReminder>> getUpcomingReminders({int limit = 5}) async {
    final res = await _api.dio
        .get('/garage/reminders', queryParameters: {'limit': limit});
    return (res.data as List<dynamic>)
        .map((e) => UpcomingReminder.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> addReminder(String vehicleId, Map<String, dynamic> body) =>
      _api.dio.post('/garage/$vehicleId/reminders', data: body);

  Future<void> updateReminder(String reminderId, Map<String, dynamic> body) =>
      _api.dio.put('/garage/reminders/$reminderId', data: body);

  Future<void> setReminderStatus(String reminderId, String status) =>
      _api.dio.post('/garage/reminders/$reminderId/status',
          data: {'status': status});

  Future<void> deleteReminder(String reminderId) =>
      _api.dio.delete('/garage/reminders/$reminderId');

  // ─── Valeur y complétude ────────────────────────────────────────────────

  Future<Valuation> getValuation(String vehicleId) async {
    final res = await _api.dio.get('/garage/$vehicleId/valuation');
    return Valuation.fromJson(res.data as Map<String, dynamic>);
  }

  Future<Completeness> getCompleteness(String vehicleId) async {
    final res = await _api.dio.get('/garage/$vehicleId/completeness');
    return Completeness.fromJson(res.data as Map<String, dynamic>);
  }
}
