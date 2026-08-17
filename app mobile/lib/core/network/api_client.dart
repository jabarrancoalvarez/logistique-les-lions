import 'package:dio/dio.dart';
import '../config/api_config.dart';
import '../storage/secure_storage.dart';

/// Cliente HTTP contra la API de Yoon u Auto.
///
/// Añade el token a cada petición y, ante un 401, refresca el token y reintenta —el
/// mismo comportamiento que el interceptor de la web—. Si el refresh falla, avisa para
/// que la app cierre sesión.
class ApiClient {
  ApiClient(this._storage) {
    dio = Dio(BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: ApiConfig.connectTimeout,
      receiveTimeout: ApiConfig.receiveTimeout,
      headers: {'Accept': 'application/json'},
    ));
    dio.interceptors.add(_authInterceptor());
  }

  final SecureStorage _storage;

  /// Se invoca cuando la sesión ya no puede refrescarse (refresh caducado/revocado).
  /// Se asigna tras la construcción para no crear un ciclo de dependencias con el
  /// controlador de sesión.
  void Function()? onSessionExpired;

  late final Dio dio;

  /// Dio separado, sin interceptor, para el propio refresh (evita recursión).
  final Dio _refreshDio = Dio(BaseOptions(baseUrl: ApiConfig.baseUrl));

  bool _refreshing = false;

  Interceptor _authInterceptor() => InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _storage.accessToken;
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (e, handler) async {
          final isAuthCall = e.requestOptions.path.contains('/auth/');
          if (e.response?.statusCode == 401 && !isAuthCall && !_refreshing) {
            final newToken = await _tryRefresh();
            if (newToken != null) {
              // Reintentar la petición original con el token nuevo.
              final req = e.requestOptions;
              req.headers['Authorization'] = 'Bearer $newToken';
              try {
                final clone = await dio.fetch(req);
                return handler.resolve(clone);
              } catch (err) {
                return handler.next(err as DioException);
              }
            }
            onSessionExpired?.call();
          }
          handler.next(e);
        },
      );

  Future<String?> _tryRefresh() async {
    final refresh = await _storage.refreshToken;
    if (refresh == null || refresh.isEmpty) return null;

    _refreshing = true;
    try {
      final res = await _refreshDio.post('/auth/refresh', data: {
        'refreshToken': refresh,
      });
      final value = (res.data as Map<String, dynamic>)['value'] as Map<String, dynamic>;
      final access = value['accessToken'] as String;
      final newRefresh = value['refreshToken'] as String;
      await _storage.updateTokens(accessToken: access, refreshToken: newRefresh);
      return access;
    } catch (_) {
      return null;
    } finally {
      _refreshing = false;
    }
  }
}
