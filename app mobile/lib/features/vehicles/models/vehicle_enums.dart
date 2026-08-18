import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

/// Etiquetas en francés de los enums del dominio (la API los envía como strings,
/// p. ej. `"Diesel"`, `"Suv"`, `"BonneAffaire"`). Nunca mostramos el código crudo.

String fuelLabel(String? v) => switch (v) {
      'Diesel' => 'Diesel',
      'Essence' => 'Essence',
      'Hybride' => 'Hybride',
      'HybrideRechargeable' => 'Hybride rechargeable',
      'Electrique' => 'Électrique',
      'Autre' => 'Autre',
      _ => '—',
    };

String transmissionLabel(String? v) => switch (v) {
      'Manuel' => 'Manuelle',
      'Automatique' => 'Automatique',
      _ => '—',
    };

String bodyLabel(String? v) => switch (v) {
      'Citadine' => 'Citadine',
      'Berline' => 'Berline',
      'Break' => 'Break',
      'Suv' => 'SUV / 4x4',
      'Coupe' => 'Coupé',
      'Cabriolet' => 'Cabriolet',
      'Monospace' => 'Monospace',
      'PickUp' => 'Pick-up',
      'Utilitaire' => 'Utilitaire',
      'Autre' => 'Autre',
      _ => '—',
    };

String conditionLabel(String? v) => switch (v) {
      'New' => 'Neuf',
      'Used' => 'Occasion',
      'Km0' => '0 km',
      _ => '—',
    };

String drivetrainLabel(String? v) => switch (v) {
      'Avant' => 'Traction avant',
      'Arriere' => 'Propulsion',
      'Integrale' => 'Intégrale (4x4)',
      _ => '—',
    };

String statusLabel(String? v) => switch (v) {
      'Brouillon' => 'Brouillon',
      'Actif' => 'Actif',
      'EnPause' => 'En pause',
      'Reserve' => 'Réservé',
      'Vendu' => 'Vendu',
      'Archive' => 'Archivé',
      _ => '—',
    };

/// Indicador de precio (estadístico, sin IA). `null` cuando no hay comparables.
class PriceIndicatorStyle {
  final String label;
  final Color color;
  final Color background;
  final IconData icon;
  const PriceIndicatorStyle(this.label, this.color, this.background, this.icon);
}

PriceIndicatorStyle? priceIndicatorStyle(String? v) => switch (v) {
      'BonneAffaire' => const PriceIndicatorStyle(
          'Bonne affaire',
          AppColors.success,
          Color(0xFFE7F6EC),
          Icons.trending_down),
      'PrixCorrect' => const PriceIndicatorStyle(
          'Prix correct',
          AppColors.azureDark,
          Color(0xFFE6F4FA),
          Icons.check_circle_outline),
      'PrixEleve' => const PriceIndicatorStyle(
          'Prix élevé',
          AppColors.warning,
          Color(0xFFFBF0E0),
          Icons.trending_up),
      _ => null,
    };
