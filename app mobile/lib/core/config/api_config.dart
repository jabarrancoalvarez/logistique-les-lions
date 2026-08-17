/// Configuración de conexión al backend.
///
/// La app móvil consume **la misma API** que la web (Render + Neon): no reimplementa
/// nada de negocio. Mismo JWT, mismos datos, mismos tres usuarios.
class ApiConfig {
  ApiConfig._();

  /// API de producción en Render. Es la que usa también la web.
  static const String baseUrl =
      'https://logistique-les-lions-api.onrender.com/api/v1';

  /// Raíz sin el prefijo de versión, para SignalR y estáticos.
  static const String rootUrl =
      'https://logistique-les-lions-api.onrender.com';

  /// El primer arranque en frío de Render puede tardar; damos margen.
  static const Duration connectTimeout = Duration(seconds: 60);
  static const Duration receiveTimeout = Duration(seconds: 60);
}
