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

  /// Hub SignalR del chat de negociaciones. El JWT viaja como `?access_token=`.
  static const String chatHubUrl =
      'https://logistique-les-lions-api.onrender.com/hubs/chat';

  /// Hub SignalR de notificaciones (campana en vivo).
  static const String notificationsHubUrl =
      'https://logistique-les-lions-api.onrender.com/hubs/notifications';

  /// Frontend web (Vercel), para la página pública de vérification del contrato.
  static const String webUrl = 'https://yoon-u-auto.vercel.app';

  /// El primer arranque en frío de Render puede tardar; damos margen.
  static const Duration connectTimeout = Duration(seconds: 60);
  static const Duration receiveTimeout = Duration(seconds: 60);
}
