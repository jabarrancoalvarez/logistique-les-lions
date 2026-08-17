// Modelos de Mon Garage. Los endpoints de /garage devuelven el valor directo.

class GarageSummary {
  final int vehicleCount;
  final int openReminderCount;
  final num? totalEstimatedValue;
  final List<GarageVehicleCard> vehicles;

  const GarageSummary({
    required this.vehicleCount,
    required this.openReminderCount,
    required this.vehicles,
    this.totalEstimatedValue,
  });

  factory GarageSummary.fromJson(Map<String, dynamic> j) => GarageSummary(
        vehicleCount: j['vehicleCount'] as int? ?? 0,
        openReminderCount: j['openReminderCount'] as int? ?? 0,
        totalEstimatedValue: j['totalEstimatedValue'] as num?,
        vehicles: (j['vehicles'] as List<dynamic>? ?? const [])
            .map((e) => GarageVehicleCard.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class GarageVehicleCard {
  final String id;
  final String title;
  final int year;
  final int? mileage;
  final String? color;
  final String? registrationPlate;
  final String? primaryImageId;
  final bool boughtOnYoonUAuto;
  final DateTime? purchaseDate;
  final CardReminder? nextReminder;
  final num? estimatedValue;
  final int completenessScore;

  const GarageVehicleCard({
    required this.id,
    required this.title,
    required this.year,
    required this.boughtOnYoonUAuto,
    required this.completenessScore,
    this.mileage,
    this.color,
    this.registrationPlate,
    this.primaryImageId,
    this.purchaseDate,
    this.nextReminder,
    this.estimatedValue,
  });

  factory GarageVehicleCard.fromJson(Map<String, dynamic> j) => GarageVehicleCard(
        id: j['id'] as String,
        title: (j['title'] ?? '') as String,
        year: j['year'] as int? ?? 0,
        mileage: j['mileage'] as int?,
        color: j['color'] as String?,
        registrationPlate: j['registrationPlate'] as String?,
        primaryImageId: j['primaryImageId'] as String?,
        boughtOnYoonUAuto: j['boughtOnYoonUAuto'] as bool? ?? false,
        purchaseDate: DateTime.tryParse((j['purchaseDate'] ?? '') as String),
        nextReminder: j['nextReminder'] == null
            ? null
            : CardReminder.fromJson(j['nextReminder'] as Map<String, dynamic>),
        estimatedValue: j['estimatedValue'] as num?,
        completenessScore: j['completenessScore'] as int? ?? 0,
      );
}

class CardReminder {
  final String id;
  final String type;
  final String label;
  final String status;
  final DateTime? dueDate;
  final int? dueMileage;
  final int? daysRemaining;
  final int? mileageRemaining;

  const CardReminder({
    required this.id,
    required this.type,
    required this.label,
    required this.status,
    this.dueDate,
    this.dueMileage,
    this.daysRemaining,
    this.mileageRemaining,
  });

  factory CardReminder.fromJson(Map<String, dynamic> j) => CardReminder(
        id: j['id'] as String,
        type: (j['type'] ?? '') as String,
        label: (j['label'] ?? '') as String,
        status: (j['status'] ?? '') as String,
        dueDate: DateTime.tryParse((j['dueDate'] ?? '') as String),
        dueMileage: j['dueMileage'] as int?,
        daysRemaining: j['daysRemaining'] as int?,
        mileageRemaining: j['mileageRemaining'] as int?,
      );
}

class GarageVehicleDetail {
  final String id;
  final String title;
  final String makeId;
  final String makeName;
  final String? modelId;
  final String? modelName;
  final String? version;
  final int year;
  final int? mileage;
  final String? fuelType;
  final String? transmission;
  final String? bodyType;
  final int? powerCv;
  final int? engineDisplacementCc;
  final String? color;
  final String? registrationPlate;
  final String? vin;
  final DateTime? purchaseDate;
  final num? purchasePrice;
  final bool boughtOnYoonUAuto;
  final String? listedVehicleId;
  final String? listedVehicleSlug;
  final String? listedVehicleStatus;
  final List<String> imageIds;

  const GarageVehicleDetail({
    required this.id,
    required this.title,
    required this.makeId,
    required this.makeName,
    required this.year,
    required this.boughtOnYoonUAuto,
    required this.imageIds,
    this.modelId,
    this.modelName,
    this.version,
    this.mileage,
    this.fuelType,
    this.transmission,
    this.bodyType,
    this.powerCv,
    this.engineDisplacementCc,
    this.color,
    this.registrationPlate,
    this.vin,
    this.purchaseDate,
    this.purchasePrice,
    this.listedVehicleId,
    this.listedVehicleSlug,
    this.listedVehicleStatus,
  });

  factory GarageVehicleDetail.fromJson(Map<String, dynamic> j) =>
      GarageVehicleDetail(
        id: j['id'] as String,
        title: (j['title'] ?? '') as String,
        makeId: (j['makeId'] ?? '') as String,
        makeName: (j['makeName'] ?? '') as String,
        modelId: j['modelId'] as String?,
        modelName: j['modelName'] as String?,
        version: j['version'] as String?,
        year: j['year'] as int? ?? 0,
        mileage: j['mileage'] as int?,
        fuelType: j['fuelType'] as String?,
        transmission: j['transmission'] as String?,
        bodyType: j['bodyType'] as String?,
        powerCv: j['powerCv'] as int?,
        engineDisplacementCc: j['engineDisplacementCc'] as int?,
        color: j['color'] as String?,
        registrationPlate: j['registrationPlate'] as String?,
        vin: j['vin'] as String?,
        purchaseDate: DateTime.tryParse((j['purchaseDate'] ?? '') as String),
        purchasePrice: j['purchasePrice'] as num?,
        boughtOnYoonUAuto: j['boughtOnYoonUAuto'] as bool? ?? false,
        listedVehicleId: j['listedVehicleId'] as String?,
        listedVehicleSlug: j['listedVehicleSlug'] as String?,
        listedVehicleStatus: j['listedVehicleStatus'] as String?,
        imageIds: (j['images'] as List<dynamic>? ?? const [])
            .map((e) => (e as Map<String, dynamic>)['id'] as String)
            .toList(),
      );
}
