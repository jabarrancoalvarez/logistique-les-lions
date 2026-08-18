import {
  VehicleFilters,
  FUEL_LABELS, TRANSMISSION_LABELS, BODY_LABELS, DRIVETRAIN_LABELS
} from '@core/services/vehicle.service';
import { SENEGAL_REGIONS } from './senegal-geo';

/** Formato del documento: 12.000.000 */
function fcfa(amount: number): string {
  return `${Math.round(amount).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.')} FCFA`;
}

function regionName(code: string): string {
  return SENEGAL_REGIONS.find(r => r.code === code)?.name ?? code;
}

/**
 * Resumen legible de unos filtros, como en la especificación:
 * `2017–2022 · ≤150.000 km · ≤12.000.000 FCFA · Dakar`
 *
 * Vive en el frontend porque los nombres de región y las etiquetas de los enums están
 * aquí: pedírselos al backend obligaría a duplicar allí el catálogo de Senegal.
 */
export function summarizeFilters(f: VehicleFilters): string {
  const parts: string[] = [];

  // Años: se muestran como rango cuando hay ambos extremos.
  if (f.yearFrom && f.yearTo) parts.push(`${f.yearFrom}–${f.yearTo}`);
  else if (f.yearFrom) parts.push(`à partir de ${f.yearFrom}`);
  else if (f.yearTo) parts.push(`jusqu'à ${f.yearTo}`);

  if (f.mileageTo) parts.push(`≤${f.mileageTo.toLocaleString('fr-FR')} km`);
  else if (f.mileageFrom) parts.push(`≥${f.mileageFrom.toLocaleString('fr-FR')} km`);

  if (f.priceFrom && f.priceTo) parts.push(`${fcfa(f.priceFrom)} – ${fcfa(f.priceTo)}`);
  else if (f.priceTo) parts.push(`≤${fcfa(f.priceTo)}`);
  else if (f.priceFrom) parts.push(`≥${fcfa(f.priceFrom)}`);

  if (f.fuelType)      parts.push(FUEL_LABELS[f.fuelType]);
  if (f.transmission)  parts.push(TRANSMISSION_LABELS[f.transmission]);
  if (f.bodyType)      parts.push(BODY_LABELS[f.bodyType]);
  if (f.drivetrain)    parts.push(DRIVETRAIN_LABELS[f.drivetrain]);

  // La ciudad es más precisa que la región: si hay ambas, basta con la ciudad.
  if (f.city) parts.push(f.city);
  else if (f.region) parts.push(regionName(f.region));

  if (f.color) parts.push(f.color);
  if (f.sellerAccountType) parts.push(f.sellerAccountType);

  const equipmentCount = f.equipmentIds?.length ?? 0;
  if (equipmentCount > 0) {
    parts.push(`${equipmentCount} équipement${equipmentCount > 1 ? 's' : ''}`);
  }

  return parts.join(' · ');
}

/**
 * Nombre propuesto para una búsqueda nueva: marca y modelo si los hay, si no el texto
 * buscado, y en último caso un nombre genérico.
 */
export function suggestSearchName(
  f: VehicleFilters,
  makeName: string | null,
  modelName: string | null
): string {
  const parts = [makeName, modelName].filter(Boolean);
  if (parts.length > 0) return parts.join(' ');
  if (f.search?.trim()) return f.search.trim();
  if (f.city) return `Véhicules à ${f.city}`;
  if (f.region) return `Véhicules en ${regionName(f.region)}`;
  return 'Ma recherche';
}
