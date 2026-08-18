import '../../../core/util/image_url.dart';

/// Anuncio «À la une» de la portada (proyección de `FeaturedVehicleDto`). Lleva lo
/// justo para la tarjeta compacta del carrusel; al tocarla se abre la ficha por slug.
class FeaturedVehicle {
  final String id;
  final String slug;
  final String title;
  final String makeName;
  final String? modelName;
  final int year;
  final int? mileage;
  final num price;
  final String? primaryImageUrl;
  final String? thumbnailUrl;

  const FeaturedVehicle({
    required this.id,
    required this.slug,
    required this.title,
    required this.makeName,
    required this.year,
    required this.price,
    this.modelName,
    this.mileage,
    this.primaryImageUrl,
    this.thumbnailUrl,
  });

  /// Imagen de portada resuelta a URL absoluta.
  String? get coverImage => resolveImageUrl(thumbnailUrl ?? primaryImageUrl);

  factory FeaturedVehicle.fromJson(Map<String, dynamic> j) => FeaturedVehicle(
        id: j['id'] as String,
        slug: j['slug'] as String,
        title: (j['title'] ?? '') as String,
        makeName: (j['makeName'] ?? '') as String,
        modelName: j['modelName'] as String?,
        year: j['year'] as int? ?? 0,
        mileage: j['mileage'] as int?,
        price: (j['price'] as num?) ?? 0,
        primaryImageUrl: j['primaryImageUrl'] as String?,
        thumbnailUrl: j['thumbnailUrl'] as String?,
      );
}
