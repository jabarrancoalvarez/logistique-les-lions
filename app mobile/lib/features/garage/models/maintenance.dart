class MaintenanceHistory {
  final int recordCount;
  final num totalCost;
  final int? lastMileage;
  final List<MaintenanceYear> years;

  const MaintenanceHistory({
    required this.recordCount,
    required this.totalCost,
    required this.years,
    this.lastMileage,
  });

  factory MaintenanceHistory.fromJson(Map<String, dynamic> j) =>
      MaintenanceHistory(
        recordCount: j['recordCount'] as int? ?? 0,
        totalCost: (j['totalCost'] as num?) ?? 0,
        lastMileage: j['lastMileage'] as int?,
        years: (j['years'] as List<dynamic>? ?? const [])
            .map((e) => MaintenanceYear.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class MaintenanceYear {
  final int year;
  final List<MaintenanceRecord> records;
  const MaintenanceYear({required this.year, required this.records});

  factory MaintenanceYear.fromJson(Map<String, dynamic> j) => MaintenanceYear(
        year: j['year'] as int? ?? 0,
        records: (j['records'] as List<dynamic>? ?? const [])
            .map((e) => MaintenanceRecord.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class MaintenanceRecord {
  final String id;
  final String type;
  final DateTime performedAt;
  final int? mileage;
  final String description;
  final num? cost;
  final String? workshop;
  final String? notes;
  final bool hasInvoice;
  final String? documentId;

  const MaintenanceRecord({
    required this.id,
    required this.type,
    required this.performedAt,
    required this.description,
    required this.hasInvoice,
    this.mileage,
    this.cost,
    this.workshop,
    this.notes,
    this.documentId,
  });

  factory MaintenanceRecord.fromJson(Map<String, dynamic> j) => MaintenanceRecord(
        id: j['id'] as String,
        type: (j['type'] ?? 'Autre') as String,
        performedAt:
            DateTime.tryParse((j['performedAt'] ?? '') as String) ?? DateTime.now(),
        mileage: j['mileage'] as int?,
        description: (j['description'] ?? '') as String,
        cost: j['cost'] as num?,
        workshop: j['workshop'] as String?,
        notes: j['notes'] as String?,
        hasInvoice: j['hasInvoice'] as bool? ?? false,
        documentId: j['documentId'] as String?,
      );
}
