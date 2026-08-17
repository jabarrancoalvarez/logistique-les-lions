import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Guarda los tokens de sesión cifrados (Keystore en Android).
///
/// Equivale al `localStorage` de la web, pero cifrado: en móvil el token JWT no debe
/// quedar en claro. Un login abre una sesión; el refresh la mantiene viva.
class SecureStorage {
  static const _storage = FlutterSecureStorage();

  static const _kAccess = 'yua_access_token';
  static const _kRefresh = 'yua_refresh_token';
  static const _kUser = 'yua_user';

  Future<void> saveSession({
    required String accessToken,
    required String refreshToken,
    required String userJson,
  }) async {
    await _storage.write(key: _kAccess, value: accessToken);
    await _storage.write(key: _kRefresh, value: refreshToken);
    await _storage.write(key: _kUser, value: userJson);
  }

  Future<void> updateTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    await _storage.write(key: _kAccess, value: accessToken);
    await _storage.write(key: _kRefresh, value: refreshToken);
  }

  Future<String?> get accessToken => _storage.read(key: _kAccess);
  Future<String?> get refreshToken => _storage.read(key: _kRefresh);
  Future<String?> get userJson => _storage.read(key: _kUser);

  Future<void> clear() async {
    await _storage.delete(key: _kAccess);
    await _storage.delete(key: _kRefresh);
    await _storage.delete(key: _kUser);
  }
}
