import '../../../core/util/image_url.dart';

/// Tarjeta del Marketplace (proyección de `VehicleListDto`). Lleva lo justo para
/// mostrar el anuncio sin abrirlo.
class VehicleSummary {
  final String id;
  final String publicReference;
  final String slug;
  final String title;
  final String makeName;
  final String? modelName;
  final String? version;
  final int year;
  final int? mileage;
  final num price;
  final String? region;
  final String? city;
  final String? customsStatus;
  final String condition;
  final String? fuelType;
  final String? transmission;
  final String? bodyType;
  final String? primaryImageUrl;
  final String? thumbnailUrl;
  final List<String> images;
  final int imageCount;
  final bool isFeatured;
  final int favoritesCount;
  final int viewsCount;
  final String status;
  final String sellerId;
  final String? priceIndicator;

  const VehicleSummary({
    required this.id,
    required this.publicReference,
    required this.slug,
    required this.title,
    required this.makeName,
    required this.year,
    required this.price,
    required this.condition,
    required this.status,
    required this.sellerId,
    required this.images,
    required this.imageCount,
    required this.isFeatured,
    required this.favoritesCount,
    required this.viewsCount,
    this.modelName,
    this.version,
    this.mileage,
    this.region,
    this.city,
    this.customsStatus,
    this.fuelType,
    this.transmission,
    this.bodyType,
    this.primaryImageUrl,
    this.thumbnailUrl,
    this.priceIndicator,
  });

  /// Primera imagen utilizable para la tarjeta, resuelta a URL absoluta.
  String? get coverImage {
    final raw = images.isNotEmpty
        ? images.first
        : (thumbnailUrl ?? primaryImageUrl);
    return resolveImageUrl(raw);
  }

  factory VehicleSummary.fromJson(Map<String, dynamic> j) => VehicleSummary(
        id: j['id'] as String,
        publicReference: (j['publicReference'] ?? '') as String,
        slug: j['slug'] as String,
        title: (j['title'] ?? '') as String,
        makeName: (j['makeName'] ?? '') as String,
        modelName: j['modelName'] as String?,
        version: j['version'] as String?,
        year: j['year'] as int? ?? 0,
        mileage: j['mileage'] as int?,
        price: (j['price'] as num?) ?? 0,
        region: j['region'] as String?,
        city: j['city'] as String?,
        customsStatus: j['customsStatus'] as String?,
        condition: (j['condition'] ?? 'Used') as String,
        fuelType: j['fuelType'] as String?,
        transmission: j['transmission'] as String?,
        bodyType: j['bodyType'] as String?,
        primaryImageUrl: j['primaryImageUrl'] as String?,
        thumbnailUrl: j['thumbnailUrl'] as String?,
        images: (j['images'] as List<dynamic>? ?? const [])
            .map((e) => e as String)
            .toList(),
        imageCount: j['imageCount'] as int? ?? 0,
        isFeatured: j['isFeatured'] as bool? ?? false,
        favoritesCount: j['favoritesCount'] as int? ?? 0,
        viewsCount: j['viewsCount'] as int? ?? 0,
        status: (j['status'] ?? 'Actif') as String,
        sellerId: (j['sellerId'] ?? '') as String,
        priceIndicator: j['priceIndicator'] as String?,
      );
}
