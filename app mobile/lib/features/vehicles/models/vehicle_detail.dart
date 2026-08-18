import '../../../core/util/image_url.dart';

/// Ficha completa del vehículo (proyección de `VehicleDetailDto`).
class VehicleDetail {
  final String id;
  final String publicReference;
  final String slug;
  final String title;
  final String? description;

  final String makeName;
  final String? modelName;
  final String? version;
  final int year;
  final int? mileage;
  final String condition;
  final String? bodyType;
  final String? fuelType;
  final String? transmission;
  final String? color;
  final int? doors;
  final int? seats;
  final String? vin;

  final int? powerCv;
  final int? engineDisplacementCc;
  final String? drivetrain;
  final String? engineName;

  final num price;
  final bool priceNegotiable;
  final num? initialPrice;
  final String? priceIndicator;
  final int priceComparablesCount;

  final String? region;
  final String? city;
  final String? district;

  final String status;
  final String featuredTier;

  final int viewsCount;
  final int favoritesCount;

  final List<VehicleImage> images;
  final List<VehicleEquipment> equipments;

  final String sellerId;
  final String sellerName;
  final String sellerAccountType;
  final String? sellerCity;
  final bool sellerPhoneVerified;
  final int sellerVerifiedSalesCount;
  final DateTime sellerMemberSince;

  const VehicleDetail({
    required this.id,
    required this.publicReference,
    required this.slug,
    required this.title,
    required this.makeName,
    required this.year,
    required this.condition,
    required this.price,
    required this.priceNegotiable,
    required this.priceComparablesCount,
    required this.status,
    this.featuredTier = 'Aucune',
    required this.viewsCount,
    required this.favoritesCount,
    required this.images,
    required this.equipments,
    required this.sellerId,
    required this.sellerName,
    required this.sellerAccountType,
    required this.sellerPhoneVerified,
    required this.sellerVerifiedSalesCount,
    required this.sellerMemberSince,
    this.description,
    this.modelName,
    this.version,
    this.mileage,
    this.bodyType,
    this.fuelType,
    this.transmission,
    this.color,
    this.doors,
    this.seats,
    this.vin,
    this.powerCv,
    this.engineDisplacementCc,
    this.drivetrain,
    this.engineName,
    this.initialPrice,
    this.priceIndicator,
    this.region,
    this.city,
    this.district,
    this.sellerCity,
  });

  factory VehicleDetail.fromJson(Map<String, dynamic> j) => VehicleDetail(
        id: j['id'] as String,
        publicReference: (j['publicReference'] ?? '') as String,
        slug: j['slug'] as String,
        title: (j['title'] ?? '') as String,
        description: j['description'] as String?,
        makeName: (j['makeName'] ?? '') as String,
        modelName: j['modelName'] as String?,
        version: j['version'] as String?,
        year: j['year'] as int? ?? 0,
        mileage: j['mileage'] as int?,
        condition: (j['condition'] ?? 'Used') as String,
        bodyType: j['bodyType'] as String?,
        fuelType: j['fuelType'] as String?,
        transmission: j['transmission'] as String?,
        color: j['color'] as String?,
        doors: j['doors'] as int?,
        seats: j['seats'] as int?,
        vin: j['vin'] as String?,
        powerCv: j['powerCv'] as int?,
        engineDisplacementCc: j['engineDisplacementCc'] as int?,
        drivetrain: j['drivetrain'] as String?,
        engineName: j['engineName'] as String?,
        price: (j['price'] as num?) ?? 0,
        priceNegotiable: j['priceNegotiable'] as bool? ?? false,
        initialPrice: j['initialPrice'] as num?,
        priceIndicator: j['priceIndicator'] as String?,
        priceComparablesCount: j['priceComparablesCount'] as int? ?? 0,
        region: j['region'] as String?,
        city: j['city'] as String?,
        district: j['district'] as String?,
        status: (j['status'] ?? 'Actif') as String,
        featuredTier: (j['featuredTier'] ?? 'Aucune') as String,
        viewsCount: j['viewsCount'] as int? ?? 0,
        favoritesCount: j['favoritesCount'] as int? ?? 0,
        images: (j['images'] as List<dynamic>? ?? const [])
            .map((e) => VehicleImage.fromJson(e as Map<String, dynamic>))
            .toList(),
        equipments: (j['equipments'] as List<dynamic>? ?? const [])
            .map((e) => VehicleEquipment.fromJson(e as Map<String, dynamic>))
            .toList(),
        sellerId: (j['sellerId'] ?? '') as String,
        sellerName: (j['sellerName'] ?? '') as String,
        sellerAccountType: (j['sellerAccountType'] ?? '') as String,
        sellerCity: j['sellerCity'] as String?,
        sellerPhoneVerified: j['sellerPhoneVerified'] as bool? ?? false,
        sellerVerifiedSalesCount: j['sellerVerifiedSalesCount'] as int? ?? 0,
        sellerMemberSince: DateTime.tryParse(
                (j['sellerMemberSince'] ?? '') as String) ??
            DateTime.now(),
      );

  /// URLs de las imágenes ordenadas (la primaria primero), resueltas a absolutas.
  List<String> get imageUrls {
    final sorted = [...images]..sort((a, b) {
        if (a.isPrimary != b.isPrimary) return a.isPrimary ? -1 : 1;
        return a.sortOrder.compareTo(b.sortOrder);
      });
    return sorted
        .map((e) => resolveImageUrl(e.url))
        .whereType<String>()
        .toList();
  }
}

class VehicleImage {
  final String id;
  final String url;
  final String? thumbnailUrl;
  final bool isPrimary;
  final int sortOrder;
  final String? altText;

  const VehicleImage({
    required this.id,
    required this.url,
    required this.isPrimary,
    required this.sortOrder,
    this.thumbnailUrl,
    this.altText,
  });

  factory VehicleImage.fromJson(Map<String, dynamic> j) => VehicleImage(
        id: j['id'] as String,
        url: j['url'] as String,
        thumbnailUrl: j['thumbnailUrl'] as String?,
        isPrimary: j['isPrimary'] as bool? ?? false,
        sortOrder: j['sortOrder'] as int? ?? 0,
        altText: j['altText'] as String?,
      );
}

class VehicleEquipment {
  final String id;
  final String code;
  final String name;

  const VehicleEquipment(
      {required this.id, required this.code, required this.name});

  factory VehicleEquipment.fromJson(Map<String, dynamic> j) =>
      VehicleEquipment(
        id: j['id'] as String,
        code: (j['code'] ?? '') as String,
        name: (j['name'] ?? '') as String,
      );
}
