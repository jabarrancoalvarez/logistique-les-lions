import '../config/api_config.dart';

/// Resuelve una URL de imagen a absoluta.
///
/// La API puede devolver:
/// - URLs absolutas (`http…`): subidas de usuario servidas por la API → tal cual.
/// - Rutas `/files/…`: almacenamiento de la API → se antepone la raíz de la API.
/// - Rutas `/assets/…` (imágenes de demostración del seed): las sirve el frontend
///   web (Vercel) → se antepone el dominio web. En la web resuelven solas contra
///   su origen; en el móvil hay que anteponer el dominio o `Image.network` falla.
String? resolveImageUrl(String? url) {
  if (url == null || url.trim().isEmpty) return null;
  final u = url.trim();
  if (u.startsWith('http://') || u.startsWith('https://')) return u;
  final path = u.startsWith('/') ? u : '/$u';
  if (path.startsWith('/files')) return '${ApiConfig.rootUrl}$path';
  return '${ApiConfig.webUrl}$path';
}
