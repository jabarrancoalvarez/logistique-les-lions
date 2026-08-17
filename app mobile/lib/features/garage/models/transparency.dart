import 'garage_enums.dart';

/// «Transparence du véhicule»: qué parte del historial privado se enseña en el
/// anuncio (`TransparencySettingsDto`). Nada se comparte sin marcarlo.
class TransparencySettings {
  final String vehicleId;
  final bool showMaintenanceHistory;
  final bool showMaintenanceDetails;
  final bool showMileageEvolution;
  final List<TransparencyRecord> records;

  const TransparencySettings({
    required this.vehicleId,
    required this.showMaintenanceHistory,
    required this.showMaintenanceDetails,
    required this.showMileageEvolution,
    required this.records,
  });

  factory TransparencySettings.fromJson(Map<String, dynamic> j) =>
      TransparencySettings(
        vehicleId: (j['vehicleId'] ?? '') as String,
        showMaintenanceHistory: j['showMaintenanceHistory'] as bool? ?? false,
        showMaintenanceDetails: j['showMaintenanceDetails'] as bool? ?? false,
        showMileageEvolution: j['showMileageEvolution'] as bool? ?? false,
        records: (j['records'] as List<dynamic>? ?? const [])
            .map((e) => TransparencyRecord.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class TransparencyRecord {
  final String maintenanceRecordId;
  final String type;
  final DateTime performedAt;
  final int? mileage;
  final String description;
  final bool hasInvoice;
  bool shared;
  bool shareInvoice;

  TransparencyRecord({
    required this.maintenanceRecordId,
    required this.type,
    required this.performedAt,
    required this.description,
    required this.hasInvoice,
    required this.shared,
    required this.shareInvoice,
    this.mileage,
  });

  String get typeLabel => maintenanceTypeLabel(type);

  factory TransparencyRecord.fromJson(Map<String, dynamic> j) =>
      TransparencyRecord(
        maintenanceRecordId: (j['maintenanceRecordId'] ?? '') as String,
        type: (j['type'] ?? 'Autre') as String,
        performedAt:
            DateTime.tryParse((j['performedAt'] ?? '') as String) ?? DateTime.now(),
        mileage: j['mileage'] as int?,
        description: (j['description'] ?? '') as String,
        hasInvoice: j['hasInvoice'] as bool? ?? false,
        shared: j['shared'] as bool? ?? false,
        shareInvoice: j['shareInvoice'] as bool? ?? false,
      );

  Map<String, dynamic> toInput() => {
        'maintenanceRecordId': maintenanceRecordId,
        'shared': shared,
        'shareInvoice': shareInvoice,
      };
}
