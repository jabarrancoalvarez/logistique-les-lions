// Checklist privada de inspección (`InspectionDto`). Los 11 puntos del documento.

/// Los 11 puntos de la checklist, en el orden del documento funcional.
const inspectionItemTypes = <String>[
  'Moteur',
  'Carrosserie',
  'Pneus',
  'Interieur',
  'Climatisation',
  'Feux',
  'Freins',
  'Direction',
  'Documents',
  'Vin',
  'EssaiRoutier',
];

String inspectionItemLabel(String type) => switch (type) {
      'Moteur' => 'Moteur',
      'Carrosserie' => 'Carrosserie',
      'Pneus' => 'Pneus',
      'Interieur' => 'Intérieur',
      'Climatisation' => 'Climatisation',
      'Feux' => 'Feux',
      'Freins' => 'Freins',
      'Direction' => 'Direction',
      'Documents' => 'Documents',
      'Vin' => 'Numéro de châssis (VIN)',
      'EssaiRoutier' => 'Essai routier',
      _ => type,
    };

String inspectionResultLabel(String? r) => switch (r) {
      'Bon' => 'Bon',
      'Moyen' => 'Moyen',
      'Mauvais' => 'Mauvais',
      _ => 'Non évalué',
    };

class Inspection {
  final String? id;
  final DateTime? visitedAt;
  final int? observedMileage;
  final String? notes;
  final List<InspectionItem> items;
  final DateTime? updatedAt;

  const Inspection({
    required this.items,
    this.id,
    this.visitedAt,
    this.observedMileage,
    this.notes,
    this.updatedAt,
  });

  factory Inspection.fromJson(Map<String, dynamic> j) => Inspection(
        id: j['id'] as String?,
        visitedAt: DateTime.tryParse((j['visitedAt'] ?? '') as String),
        observedMileage: j['observedMileage'] as int?,
        notes: j['notes'] as String?,
        items: (j['items'] as List<dynamic>? ?? const [])
            .map((e) => InspectionItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        updatedAt: DateTime.tryParse((j['updatedAt'] ?? '') as String),
      );
}

class InspectionItem {
  final String type;
  final String? result; // Bon | Moyen | Mauvais | null
  final String? notes;

  const InspectionItem({required this.type, this.result, this.notes});

  factory InspectionItem.fromJson(Map<String, dynamic> j) => InspectionItem(
        type: (j['type'] ?? '') as String,
        result: j['result'] as String?,
        notes: j['notes'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'type': type,
        'result': result,
        'notes': notes,
      };

  InspectionItem copyWith({String? result, String? notes, bool clearResult = false}) =>
      InspectionItem(
        type: type,
        result: clearResult ? null : (result ?? this.result),
        notes: notes ?? this.notes,
      );
}
