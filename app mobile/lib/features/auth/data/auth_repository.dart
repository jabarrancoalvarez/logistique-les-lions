import 'dart:convert';
import '../../../core/network/api_client.dart';
import '../../../core/storage/secure_storage.dart';
import '../models/app_user.dart';
import '../models/profile.dart';

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
    String? region,
    String? city,
    String? email,
  }) async {
    final res = await _api.dio.post('/auth/register', data: {
      'phone': phone,
      'password': password,
      'displayName': displayName,
      'accountType': accountType,
      'region': region,
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

  /// Perfil completo del usuario. `GET /auth/me` (envuelto en Result).
  Future<Profile> getMyProfile() async {
    final res = await _api.dio.get('/auth/me');
    final value = (res.data as Map<String, dynamic>)['value']
        as Map<String, dynamic>;
    return Profile.fromJson(value);
  }

  /// Actualiza el perfil. `PUT /auth/me`. El teléfono no se envía (no editable).
  Future<void> updateProfile({
    required String displayName,
    required String accountType,
    String? region,
    String? city,
    String? email,
    String? bio,
    bool allowWhatsAppContact = false,
  }) async {
    await _api.dio.put('/auth/me', data: {
      'displayName': displayName,
      'accountType': accountType,
      'region': region,
      'city': city,
      'email': email,
      'bio': bio,
      'allowWhatsAppContact': allowWhatsAppContact,
    });
  }

  /// Vuelve a pedir el perfil y actualiza el [AppUser] guardado (sin tocar los
  /// tokens). Se usa tras editar el perfil.
  Future<AppUser?> reloadUser() async {
    final p = await getMyProfile();
    final user = AppUser(
      id: p.id,
      displayName: p.displayName,
      role: p.role,
      phone: p.phone,
      email: p.email,
      accountType: p.accountType,
      avatarUrl: p.avatarUrl,
    );
    await _storage.saveUser(jsonEncode(user.toJson()));
    return user;
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
