import 'dart:convert';
import '../../../core/network/api_client.dart';
import '../../../core/storage/secure_storage.dart';
import '../models/app_user.dart';

/// Acceso a los endpoints de autenticación (mismos que la web).
class AuthRepository {
  AuthRepository(this._api, this._storage);

  final ApiClient _api;
  final SecureStorage _storage;

  /// Login por teléfono (+221XXXXXXXXX) o email. Persiste la sesión cifrada.
  Future<AppUser> login(String identifier, String password) async {
    final res = await _api.dio.post('/auth/login', data: {
      'identifier': identifier,
      'password': password,
    });
    return _persist(res.data);
  }

  /// Alta de cuenta. El teléfono es el identificador; el correo es opcional.
  Future<AppUser> register({
    required String phone,
    required String password,
    required String displayName,
    required String accountType, // 'Particulier' | 'Professionnel'
    String? city,
    String? email,
  }) async {
    final res = await _api.dio.post('/auth/register', data: {
      'phone': phone,
      'password': password,
      'displayName': displayName,
      'accountType': accountType,
      'city': city,
      'email': (email != null && email.isNotEmpty) ? email : null,
    });
    return _persist(res.data);
  }

  Future<void> logout() async {
    try {
      await _api.dio.post('/auth/logout');
    } catch (_) {
      // El cierre local no depende de que el servidor responda.
    }
    await _storage.clear();
  }

  /// Restaura la sesión guardada al abrir la app. `null` si no hay ninguna.
  Future<AppUser?> restore() async {
    final userJson = await _storage.userJson;
    if (userJson == null) return null;
    return AppUser.fromJson(jsonDecode(userJson) as Map<String, dynamic>);
  }

  Future<AppUser> _persist(dynamic data) async {
    final value = (data as Map<String, dynamic>)['value'] as Map<String, dynamic>;
    final user = AppUser.fromJson(value['user'] as Map<String, dynamic>);
    await _storage.saveSession(
      accessToken: value['accessToken'] as String,
      refreshToken: value['refreshToken'] as String,
      userJson: jsonEncode(user.toJson()),
    );
    return user;
  }
}
