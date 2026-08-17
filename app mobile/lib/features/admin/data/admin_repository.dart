import '../../../core/network/api_client.dart';
import '../models/admin_common.dart';
import '../models/admin_dashboard.dart';
import '../models/admin_listing.dart';
import '../models/admin_report.dart';
import '../models/admin_user.dart';

/// Acceso al backoffice (`/admin/*`, rol Admin). Los endpoints devuelven el
/// valor directo. Toda acción moderadora lleva **motivo**, que el backend
/// registra en `admin_actions`.
class AdminRepository {
  AdminRepository(this._api);
  final ApiClient _api;

  Future<AdminDashboard> getDashboard() async {
    final res = await _api.dio.get('/admin/dashboard');
    return AdminDashboard.fromJson(res.data as Map<String, dynamic>);
  }

  // ─── Users ──────────────────────────────────────────────────────────────

  Future<AdminPage<AdminUserRow>> getUsers(
      {String? search, String? status, int page = 1, int pageSize = 30}) async {
    final res = await _api.dio.get('/admin/users', queryParameters: {
      'search': ?search,
      'status': ?status,
      'page': page,
      'pageSize': pageSize,
    });
    return AdminPage.fromJson(
        res.data as Map<String, dynamic>, AdminUserRow.fromJson);
  }

  Future<AdminUserDetail> getUser(String id) async {
    final res = await _api.dio.get('/admin/users/$id');
    return AdminUserDetail.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> setUserStatus(String id,
      {required String status, required String reason, DateTime? suspendedUntil}) {
    return _api.dio.post('/admin/users/$id/status', data: {
      'status': status,
      'reason': reason,
      'suspendedUntil': ?suspendedUntil?.toUtc().toIso8601String(),
    });
  }

  // ─── Listings ───────────────────────────────────────────────────────────

  Future<AdminPage<AdminListingRow>> getListings(
      {String? search, int page = 1, int pageSize = 30}) async {
    final res = await _api.dio.get('/admin/listings', queryParameters: {
      'search': ?search,
      'page': page,
      'pageSize': pageSize,
    });
    return AdminPage.fromJson(
        res.data as Map<String, dynamic>, AdminListingRow.fromJson);
  }

  Future<AdminListingDetail> getListing(String id) async {
    final res = await _api.dio.get('/admin/listings/$id');
    return AdminListingDetail.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> listingAction(String id,
          {required String action, required String reason}) =>
      _api.dio.post('/admin/listings/$id/action',
          data: {'action': action, 'reason': reason});

  Future<void> requestCorrection(String id, String message) =>
      _api.dio.post('/admin/listings/$id/correction',
          data: {'message': message});

  // ─── Reports (signalements) ─────────────────────────────────────────────

  Future<ReportList> getReports(
      {String? status, int page = 1, int pageSize = 30}) async {
    final res = await _api.dio.get('/admin/reports', queryParameters: {
      'status': ?status,
      'page': page,
      'pageSize': pageSize,
    });
    return ReportList.fromJson(res.data as Map<String, dynamic>);
  }

  Future<ReportDetail> getReport(String id) async {
    final res = await _api.dio.get('/admin/reports/$id');
    return ReportDetail.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> setReportStatus(String id,
          {required String status, String? resolution}) =>
      _api.dio.post('/admin/reports/$id/status',
          data: {'status': status, 'resolution': ?resolution});

  Future<void> warnReportedUser(String id, String message) =>
      _api.dio.post('/admin/reports/$id/warn', data: {'message': message});
}
