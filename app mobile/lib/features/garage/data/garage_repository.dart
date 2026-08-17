import 'package:dio/dio.dart';
import '../../../core/network/api_client.dart';
import '../models/garage_document.dart';
import '../models/garage_models.dart';
import '../models/maintenance.dart';
import '../models/reminder.dart';
import '../models/transparency.dart';
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

  // ─── Fotos (privadas) ───────────────────────────────────────────────────

  /// Bytes de una foto privada del garaje (endpoint autenticado).
  Future<List<int>> imageBytes(String imageId) async {
    final res = await _api.dio.get<List<int>>('/garage/images/$imageId',
        options: Options(responseType: ResponseType.bytes));
    return res.data ?? const [];
  }

  Future<void> uploadVehicleImage(
    String vehicleId, {
    required List<int> bytes,
    required String filename,
    required String contentType,
    bool isPrimary = false,
    int sortOrder = 0,
  }) async {
    final form = FormData.fromMap({
      'file': MultipartFile.fromBytes(bytes,
          filename: filename, contentType: DioMediaType.parse(contentType)),
      'isPrimary': isPrimary,
      'sortOrder': sortOrder,
    });
    await _api.dio.post('/garage/$vehicleId/images', data: form);
  }

  Future<void> deleteVehicleImage(String imageId) =>
      _api.dio.delete('/garage/images/$imageId');

  // ─── Documents ──────────────────────────────────────────────────────────

  Future<List<GarageDocument>> getDocuments(String vehicleId) async {
    final res = await _api.dio.get('/garage/$vehicleId/documents');
    return (res.data as List<dynamic>)
        .map((e) => GarageDocument.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> uploadDocument(
    String vehicleId, {
    required List<int> bytes,
    required String filename,
    required String contentType,
    required String type,
    String? name,
    DateTime? documentDate,
    String? notes,
  }) async {
    final form = FormData.fromMap({
      'file': MultipartFile.fromBytes(bytes,
          filename: filename, contentType: DioMediaType.parse(contentType)),
      'type': type,
      'name': ?name,
      'documentDate': ?documentDate?.toUtc().toIso8601String(),
      'notes': ?notes,
    });
    await _api.dio.post('/garage/$vehicleId/documents', data: form);
  }

  Future<List<int>> documentBytes(String documentId) async {
    final res = await _api.dio.get<List<int>>(
        '/garage/documents/$documentId/file',
        options: Options(responseType: ResponseType.bytes));
    return res.data ?? const [];
  }

  Future<void> updateDocument(String documentId, Map<String, dynamic> body) =>
      _api.dio.put('/garage/documents/$documentId', data: body);

  Future<void> deleteDocument(String documentId) =>
      _api.dio.delete('/garage/documents/$documentId');

  // ─── Transparence ───────────────────────────────────────────────────────

  Future<TransparencySettings> getTransparency(String vehicleId) async {
    final res =
        await _api.dio.get('/garage/listings/$vehicleId/transparency');
    return TransparencySettings.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> saveTransparency(
    String vehicleId, {
    required bool showMaintenanceHistory,
    required bool showMaintenanceDetails,
    required bool showMileageEvolution,
    required List<TransparencyRecord> records,
  }) async {
    await _api.dio.put('/garage/listings/$vehicleId/transparency', data: {
      'showMaintenanceHistory': showMaintenanceHistory,
      'showMaintenanceDetails': showMaintenanceDetails,
      'showMileageEvolution': showMileageEvolution,
      'records': records.map((r) => r.toInput()).toList(),
    });
  }
}
