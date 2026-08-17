import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

// Etiquetas del backoffice (la API envía los enums como strings).

String accountStatusLabel(String? s) => switch (s) {
      'Active' => 'Actif',
      'Suspended' => 'Suspendu',
      'Blocked' => 'Bloqué',
      _ => '—',
    };

Color accountStatusColor(String? s) => switch (s) {
      'Active' => AppColors.success,
      'Suspended' => AppColors.warning,
      'Blocked' => AppColors.error,
      _ => AppColors.steel,
    };

const accountStatusValues = <String>['Active', 'Suspended', 'Blocked'];

String accountTypeLabel(String? t) => switch (t) {
      'Particulier' => 'Particulier',
      'Professionnel' => 'Professionnel',
      _ => '—',
    };

String userRoleLabel(String? r) => switch (r) {
      'Admin' => 'Administrateur',
      'User' => 'Utilisateur',
      _ => r ?? '—',
    };

/// Acciones de moderación de un anuncio.
const listingActionValues = <String>[
  'Hide', 'Reactivate', 'Flag', 'Unflag', 'Archive', 'Delete',
];

String listingActionLabel(String a) => switch (a) {
      'Hide' => 'Masquer',
      'Reactivate' => 'Réafficher',
      'Flag' => 'Marquer pour révision',
      'Unflag' => 'Retirer la marque',
      'Archive' => 'Archiver',
      'Delete' => 'Supprimer',
      _ => a,
    };

String reportStatusLabel(String? s) => switch (s) {
      'Nouveau' => 'Nouveau',
      'EnExamen' => 'En examen',
      'Resolu' => 'Résolu',
      'Rejete' => 'Rejeté',
      _ => '—',
    };

Color reportStatusColor(String? s) => switch (s) {
      'Nouveau' => AppColors.warning,
      'EnExamen' => AppColors.azureDark,
      'Resolu' => AppColors.success,
      'Rejete' => AppColors.steel,
      _ => AppColors.steel,
    };

const reportStatusValues = <String>['Nouveau', 'EnExamen', 'Resolu', 'Rejete'];

String reportReasonLabel(String? r) => switch (r) {
      'AnnonceSuspecte' => 'Annonce suspecte',
      'InformationFausse' => 'Information fausse',
      'PrixTrompeur' => 'Prix trompeur',
      'PhotosIncorrectes' => 'Photos incorrectes',
      'VehiculeInexistant' => 'Véhicule inexistant',
      'TentativeDeFraude' => 'Tentative de fraude',
      'ComportementInapproprie' => 'Comportement inapproprié',
      'Spam' => 'Spam',
      'Autre' => 'Autre',
      _ => 'Signalement',
    };

String reportTargetLabel(String? t) => switch (t) {
      'Listing' => 'Annonce',
      'User' => 'Utilisateur',
      'Negotiation' => 'Négociation',
      _ => '—',
    };

/// Descripción legible de una acción registrada en `admin_actions`.
String adminActionLabel(String? t) => switch (t) {
      'AccountActivated' => 'Compte réactivé',
      'AccountSuspended' => 'Compte suspendu',
      'AccountBlocked' => 'Compte bloqué',
      'ListingHidden' => 'Annonce masquée',
      'ListingReactivated' => 'Annonce réaffichée',
      'ListingFlagged' => 'Annonce marquée',
      'ListingArchived' => 'Annonce archivée',
      'ListingDeleted' => 'Annonce supprimée',
      'ListingCorrectionRequested' => 'Correction demandée',
      'NegotiationContentAccessed' => 'Contenu de négociation consulté',
      'ContractInvalidated' => 'Contrat invalidé',
      'ContractDocumentAccessed' => 'Document de contrat consulté',
      'ReportResolved' => 'Signalement résolu',
      'UserWarned' => 'Utilisateur averti',
      'ReportInfoRequested' => 'Information demandée',
      'ReportUnderReview' => 'Signalement en examen',
      'PointsAdjusted' => 'Points ajustés',
      'SettingsChanged' => 'Paramètres modifiés',
      'FeatureFlagToggled' => 'Indicateur modifié',
      'CatalogChanged' => 'Catalogue modifié',
      _ => 'Action',
    };
