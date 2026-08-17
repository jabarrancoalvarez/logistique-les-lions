// Etiquetas en francés de los enums de Mon Garage (la API los envía como strings).

String maintenanceTypeLabel(String? t) => switch (t) {
      'Vidange' => 'Vidange',
      'Filtres' => 'Filtres',
      'Pneus' => 'Pneus',
      'Freins' => 'Freins',
      'Batterie' => 'Batterie',
      'Distribution' => 'Distribution',
      'Embrayage' => 'Embrayage',
      'Suspension' => 'Suspension',
      'Climatisation' => 'Climatisation',
      'ReparationMoteur' => 'Réparation moteur',
      'RevisionGenerale' => 'Révision générale',
      'Autre' => 'Autre',
      _ => 'Intervention',
    };

const maintenanceTypeValues = <String>[
  'Vidange', 'Filtres', 'Pneus', 'Freins', 'Batterie', 'Distribution',
  'Embrayage', 'Suspension', 'Climatisation', 'ReparationMoteur',
  'RevisionGenerale', 'Autre',
];

String reminderTypeLabel(String? t) => switch (t) {
      'Vidange' => 'Vidange',
      'Assurance' => 'Assurance',
      'Inspection' => 'Inspection technique',
      'Pneus' => 'Pneus',
      'Distribution' => 'Distribution',
      'Freins' => 'Freins',
      'Revision' => 'Révision',
      'Autre' => 'Autre',
      _ => 'Rappel',
    };

const reminderTypeValues = <String>[
  'Vidange', 'Assurance', 'Inspection', 'Pneus', 'Distribution', 'Freins',
  'Revision', 'Autre',
];

String reminderStatusLabel(String? s) => switch (s) {
      'AVenir' => 'À venir',
      'AFaire' => 'À faire',
      'Termine' => 'Terminé',
      'Annule' => 'Annulé',
      _ => '—',
    };

bool reminderIsOpen(String? s) => s == 'AVenir' || s == 'AFaire';

String completenessLevelLabel(String? l) => switch (l) {
      'AComplete' => 'À compléter',
      'Correct' => 'Correct',
      'TresBien' => 'Très bien',
      'Excellent' => 'Excellent',
      _ => '—',
    };

String completenessCheckLabel(String? c) => switch (c) {
      'MainInformation' => 'Informations principales',
      'MileageUpToDate' => 'Kilométrage à jour',
      'Vin' => 'Numéro de châssis (VIN)',
      'Photos' => 'Photos',
      'Documents' => 'Documents',
      'MaintenanceHistory' => 'Historique d’entretien',
      'Reminders' => 'Rappels',
      'MaintenanceInvoices' => 'Factures d’entretien',
      _ => c ?? '—',
    };
