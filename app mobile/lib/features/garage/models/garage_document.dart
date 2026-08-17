/// Documento del historial de un vehículo del garaje (`GarageDocumentDto`).
/// ❌ Sin StorageKey: el archivo se descarga por endpoint autenticado.
class GarageDocument {
  final String id;
  final String type;
  final String name;
  final DateTime? documentDate;
  final String fileName;
  final String contentType;
  final int sizeBytes;
  final String? notes;
  final DateTime uploadedAt;

  const GarageDocument({
    required this.id,
    required this.type,
    required this.name,
    required this.fileName,
    required this.contentType,
    required this.sizeBytes,
    required this.uploadedAt,
    this.documentDate,
    this.notes,
  });

  bool get isPdf => contentType == 'application/pdf';

  factory GarageDocument.fromJson(Map<String, dynamic> j) => GarageDocument(
        id: j['id'] as String,
        type: (j['type'] ?? 'Autre') as String,
        name: (j['name'] ?? '') as String,
        documentDate: DateTime.tryParse((j['documentDate'] ?? '') as String),
        fileName: (j['fileName'] ?? '') as String,
        contentType: (j['contentType'] ?? '') as String,
        sizeBytes: j['sizeBytes'] as int? ?? 0,
        notes: j['notes'] as String?,
        uploadedAt:
            DateTime.tryParse((j['uploadedAt'] ?? '') as String) ?? DateTime.now(),
      );
}

const garageDocumentTypeValues = <String>[
  'ContratDeVente', 'CarteGrise', 'Douane', 'Assurance', 'ControleTechnique',
  'FactureEntretien', 'FactureReparation', 'FactureAchat', 'Autre',
];

String garageDocumentTypeLabel(String? t) => switch (t) {
      'ContratDeVente' => 'Contrat de vente',
      'CarteGrise' => 'Carte grise',
      'Douane' => 'Douane',
      'Assurance' => 'Assurance',
      'ControleTechnique' => 'Contrôle technique',
      'FactureEntretien' => 'Facture d’entretien',
      'FactureReparation' => 'Facture de réparation',
      'FactureAchat' => 'Facture d’achat',
      'Autre' => 'Autre',
      _ => 'Document',
    };
