import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@environments/environment';

// ─── Enums (espejo del backend) ──────────────────────────────────────────────
export type VehicleCondition = 'New' | 'Used' | 'Km0';
export type FuelType = 'Diesel' | 'Essence' | 'Hybride' | 'HybrideRechargeable' | 'Electrique' | 'Autre';
export type TransmissionType = 'Manuel' | 'Automatique';
export type BodyType =
  | 'Citadine' | 'Berline' | 'Break' | 'Suv' | 'Coupe'
  | 'Cabriolet' | 'Monospace' | 'PickUp' | 'Utilitaire' | 'Autre';
export type VehicleStatus = 'Brouillon' | 'Actif' | 'EnPause' | 'Reserve' | 'Vendu' | 'Archive';
export type Drivetrain = 'Avant' | 'Arriere' | 'Integrale';

/** Etiquetas en francés de los enums, para no repetirlas en cada componente. */
export const FUEL_LABELS: Record<FuelType, string> = {
  Diesel: 'Diesel',
  Essence: 'Essence',
  Hybride: 'Hybride',
  HybrideRechargeable: 'Hybride rechargeable',
  Electrique: 'Électrique',
  Autre: 'Autre'
};

export const TRANSMISSION_LABELS: Record<TransmissionType, string> = {
  Manuel: 'Manuelle',
  Automatique: 'Automatique'
};

export const BODY_LABELS: Record<BodyType, string> = {
  Citadine: 'Citadine',
  Berline: 'Berline',
  Break: 'Break',
  Suv: 'SUV / 4x4',
  Coupe: 'Coupé',
  Cabriolet: 'Cabriolet',
  Monospace: 'Monospace',
  PickUp: 'Pick-up',
  Utilitaire: 'Fourgon / Utilitaire',
  Autre: 'Autre'
};

export const DRIVETRAIN_LABELS: Record<Drivetrain, string> = {
  Avant: 'Traction avant',
  Arriere: 'Propulsion',
  Integrale: '4x4 / AWD'
};

/** Indicador estadístico de precio. `null` cuando no hay comparables suficientes. */
export type PriceIndicator = 'BonneAffaire' | 'PrixCorrect' | 'PrixEleve';

export const PRICE_INDICATOR_LABELS: Record<PriceIndicator, string> = {
  BonneAffaire: 'Bonne affaire',
  PrixCorrect: 'Prix correct',
  PrixEleve: 'Prix élevé'
};

/** Colores del badge: verde para la buena oferta, neutro y ámbar. */
export const PRICE_INDICATOR_CLASSES: Record<PriceIndicator, string> = {
  BonneAffaire: 'bg-green-100 text-green-800',
  PrixCorrect: 'bg-navy/10 text-navy',
  PrixEleve: 'bg-amber-100 text-amber-800'
};

export const STATUS_LABELS: Record<VehicleStatus, string> = {
  Brouillon: 'Brouillon',
  Actif: 'Actif',
  EnPause: 'En pause',
  Reserve: 'Réservé',
  Vendu: 'Vendu',
  Archive: 'Archivé'
};

// ─── DTOs ─────────────────────────────────────────────────────────────────────
export interface FeaturedVehicle {
  id: string;
  slug: string;
  title: string;
  makeName: string;
  modelName: string | null;
  year: number;
  mileage: number | null;
  price: number;
  currency: string;
  countryOrigin: string;
  countryFlagEmoji: string | null;
  condition: VehicleCondition;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  primaryImageUrl: string | null;
  thumbnailUrl: string | null;
  favoritesCount: number;
  viewsCount: number;
  createdAt: string;
}

export interface VehicleListItem {
  id: string;
  /** Réf. Yoon: "YU12345" */
  publicReference: string;
  slug: string;
  title: string;
  makeName: string;
  modelName: string | null;
  version: string | null;
  year: number;
  mileage: number | null;
  price: number;
  currency: string;
  region: string | null;
  city: string | null;
  condition: VehicleCondition;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  bodyType: BodyType | null;
  primaryImageUrl: string | null;
  thumbnailUrl: string | null;
  /** Fotos ordenadas, para deslizarlas dentro de la propia tarjeta. */
  images: string[];
  imageCount: number;
  isFeatured: boolean;
  favoritesCount: number;
  viewsCount: number;
  createdAt: string;
  status?: VehicleStatus;
  sellerId?: string;
  /** `null` si no hay suficientes vehículos comparables. */
  priceIndicator: PriceIndicator | null;
}

export interface VehicleImage {
  id: string;
  url: string;
  thumbnailUrl: string | null;
  isPrimary: boolean;
  sortOrder: number;
  altText: string | null;
}

export interface VehicleEquipmentItem {
  id: string;
  code: string;
  name: string;
}

export interface VehicleDetail {
  id: string;
  publicReference: string;
  slug: string;
  title: string;
  /** Bloque «Description du vendeur». Texto íntegro del usuario. */
  description: string | null;

  makeId: string;
  makeName: string;
  modelId: string | null;
  modelName: string | null;
  version: string | null;
  year: number;
  mileage: number | null;
  condition: VehicleCondition;
  bodyType: BodyType | null;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  color: string | null;
  doors: number | null;
  seats: number | null;
  vin: string | null;

  powerCv: number | null;
  engineDisplacementCc: number | null;
  drivetrain: Drivetrain | null;
  engineName: string | null;


  price: number;
  currency: string;
  priceNegotiable: boolean;
  /** Primer precio registrado — «Prix initial». */
  initialPrice: number | null;
  priceChangedAt: string | null;
  /** `null` si no hay suficientes vehículos comparables. */
  priceIndicator: PriceIndicator | null;
  /** Anuncios usados como referencia, para explicar el cálculo. */
  priceComparablesCount: number;

  region: string | null;
  city: string | null;
  district: string | null;

  status: VehicleStatus;
  isFeatured: boolean;
  publishedAt: string | null;
  reservedAt: string | null;
  soldAt: string | null;

  viewsCount: number;
  favoritesCount: number;
  contactsCount: number;
  createdAt: string;

  images: VehicleImage[];
  equipments: VehicleEquipmentItem[];

  // ─── Vendu par ─────────────────────────────────────────────────────────
  sellerId: string;
  sellerName: string;
  sellerAccountType: string;
  sellerCity: string | null;
  sellerPhoneVerified: boolean;
  sellerVerifiedSalesCount: number;
  sellerMemberSince: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export type AccountTypeFilter = 'Particulier' | 'Professionnel';

/** Filtros del Marketplace, alineados con la especificación funcional. */
export interface VehicleFilters {
  search?: string;

  makeId?: string;
  modelId?: string;

  priceFrom?: number;
  priceTo?: number;
  yearFrom?: number;
  yearTo?: number;
  mileageFrom?: number;
  mileageTo?: number;

  region?: string;
  city?: string;

  fuelType?: FuelType;
  transmission?: TransmissionType;
  bodyType?: BodyType;
  drivetrain?: Drivetrain;
  condition?: VehicleCondition;

  powerFrom?: number;
  powerTo?: number;
  displacementFrom?: number;
  displacementTo?: number;

  doorsFrom?: number;
  doorsTo?: number;
  seatsFrom?: number;
  seatsTo?: number;

  color?: string;
  /** Selección múltiple: el anuncio debe declararlos todos. */
  equipmentIds?: string[];

  sellerAccountType?: AccountTypeFilter;

  countryOrigin?: string;
  isFeatured?: boolean;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
  sellerId?: string;
  status?: VehicleStatus;
}

/** Un vehículo guardado en Favoris. */
export interface FavoriteItem {
  /** Datos actuales del anuncio: el favorito nunca guarda una copia. */
  vehicle: VehicleListItem;
  priceWhenSaved: number;
  /** Bajada acumulada desde que se guardó, o `null` si no ha bajado. */
  priceDrop: number | null;
  alertEnabled: boolean;
  savedAt: string;
}

export interface Favorites {
  /** Interruptor general: todos los favoritos reciben alertas. */
  alertsAllEnabled: boolean;
  items: FavoriteItem[];
}

/** Ficha reducida de un vehículo dentro del comparador. */
export interface VehicleComparison {
  id: string;
  publicReference: string;
  slug: string;
  makeName: string;
  modelName: string | null;
  version: string | null;
  primaryImageUrl: string | null;

  price: number;
  priceIndicator: PriceIndicator | null;
  city: string | null;
  status: VehicleStatus;

  year: number;
  mileage: number | null;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  bodyType: BodyType | null;
  powerCv: number | null;
  engineDisplacementCc: number | null;
  drivetrain: Drivetrain | null;
  doors: number | null;
  seats: number | null;
  color: string | null;

  /** Códigos del catálogo, p. ej. `CLIMATISATION`. */
  equipmentCodes: string[];

  sellerId: string;
}

export interface FilterOptions {
  equipments: { id: string; code: string; name: string }[];
  colors: string[];
}

export interface VehicleModelOption {
  id: string;
  name: string;
  vehiclesCount: number;
}

export interface VehicleStats {
  activeVehicles: number;
  supportedCountries: number;
  completedTransactions: number;
  registeredDealers: number;
  totalMakes: number;
}

export interface VehicleMake {
  id: string;
  name: string;
  country: string | null;
  logoUrl: string | null;
  isPopular: boolean;
  modelsCount: number;
}

/**
 * «Transparence du véhicule» — el historial de Mon Garage que quien vende ha decidido
 * enseñar en el anuncio. Nada se publica sin marcarlo expresamente.
 */
export interface PublicMaintenance {
  type: string;
  description: string;
  /** `null` si el vendedor comparte el historial pero no las fechas. */
  performedAt: string | null;
  mileage: number | null;
  /** Identificador de la factura compartida, si la ha compartido. */
  invoiceDocumentId: string | null;
}

export interface PublicTransparency {
  /** «7 entretiens enregistrés sur Yoon u Auto». */
  maintenanceCount: number;
  showDetails: boolean;
  records: PublicMaintenance[];
  mileageEvolution: { date: string; mileage: number }[];
}

// ─── Service ──────────────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class VehicleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/vehicles`;

  // ─── Landing endpoints ──────────────────────────────────────────────────
  getFeaturedVehicles(count = 6): Observable<FeaturedVehicle[]> {
    return this.http.get<FeaturedVehicle[]>(`${this.baseUrl}/featured`, {
      params: { count }
    });
  }

  getStats(): Observable<VehicleStats> {
    return this.http.get<VehicleStats>(`${this.baseUrl}/stats`);
  }

  getMakes(onlyPopular = false): Observable<VehicleMake[]> {
    return this.http.get<VehicleMake[]>(`${this.baseUrl}/makes`, {
      params: { onlyPopular }
    });
  }

  /** Modelos de una marca, para el desplegable dependiente del filtro. */
  getModels(makeId: string): Observable<VehicleModelOption[]> {
    return this.http.get<VehicleModelOption[]>(`${this.baseUrl}/makes/${makeId}/models`);
  }

  /** Datos actuales de los vehículos del comparador. */
  compareVehicles(ids: string[]): Observable<VehicleComparison[]> {
    let params = new HttpParams();
    ids.forEach(id => { params = params.append('ids', id); });
    return this.http.get<VehicleComparison[]>(`${this.baseUrl}/compare`, { params });
  }

  /** Catálogo de equipamiento y colores presentes en los anuncios. */
  getFilterOptions(): Observable<FilterOptions> {
    return this.http.get<FilterOptions>(`${this.baseUrl}/filter-options`);
  }

  /**
   * Convierte los filtros a query string. Los arrays se repiten como parámetro
   * (`equipmentIds=a&equipmentIds=b`), que es lo que espera el binding del backend.
   */
  private toParams(filters: VehicleFilters): HttpParams {
    let params = new HttpParams();
    Object.entries(filters).forEach(([key, val]) => {
      if (val === undefined || val === null || val === '') return;
      if (Array.isArray(val)) {
        val.forEach(v => { params = params.append(key, String(v)); });
      } else {
        params = params.set(key, String(val));
      }
    });
    return params;
  }

  // ─── M2 endpoints ──────────────────────────────────────────────────────
  getVehicles(filters: VehicleFilters = {}): Observable<PagedResult<VehicleListItem>> {
    return this.http.get<PagedResult<VehicleListItem>>(this.baseUrl, {
      params: this.toParams(filters)
    });
  }

  /**
   * Cuántos resultados producen unos filtros, sin traer los anuncios. Permite mostrar
   * el recuento en el propio panel antes de aplicarlos.
   */
  countVehicles(filters: VehicleFilters = {}): Observable<number> {
    return this.http
      .get<{ count: number }>(`${this.baseUrl}/count`, { params: this.toParams(filters) })
      .pipe(map(r => r.count));
  }

  /**
   * «Transparence du véhicule»: el historial que quien vende ha decidido enseñar.
   * `null` si no ha compartido nada.
   */
  getTransparency(vehicleId: string): Observable<PublicTransparency | null> {
    return this.http.get<PublicTransparency | null>(
      `${this.baseUrl}/${vehicleId}/transparency`);
  }

  /**
   * «Signaler» — reportar un anuncio, a una persona o una conversación.
   * Devuelve la referencia del signalement (`SG00042`).
   */
  report(payload: {
    targetType: 'Listing' | 'User' | 'Negotiation';
    targetId: string;
    reason: string;
    description: string | null;
  }): Observable<{ reference: string }> {
    return this.http.post<{ reference: string }>(`${this.baseUrl}/reports`, payload);
  }

  /** URL de una factura compartida. Pública solo porque se compartió expresamente. */
  sharedInvoiceUrl(vehicleId: string, documentId: string): string {
    return `${this.baseUrl}/${vehicleId}/transparency/invoices/${documentId}`;
  }

  getVehicleBySlug(slug: string): Observable<VehicleDetail> {
    return this.http.get<VehicleDetail>(`${this.baseUrl}/${slug}`);
  }

  /** «Véhicules similaires»: reglas de base de datos, sin IA. */
  getSimilarVehicles(vehicleId: string, take = 8): Observable<VehicleListItem[]> {
    return this.http.get<VehicleListItem[]>(`${this.baseUrl}/${vehicleId}/similar`, {
      params: { take }
    });
  }

  /** Contador informativo: no debe hacer fallar la carga de la ficha. */
  registerView(vehicleId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${vehicleId}/view`, null);
  }

  getVehicleHistory(id: string): Observable<unknown[]> {
    return this.http.get<unknown[]>(`${this.baseUrl}/${id}/history`);
  }

  createVehicle(data: unknown): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, data);
  }

  updateVehicle(id: string, data: unknown): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, data);
  }

  deleteVehicle(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  uploadImage(vehicleId: string, data: unknown): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/${vehicleId}/images`, data);
  }

  /** El usuario sale del token; no se envía en la petición. */
  toggleFavorite(vehicleId: string): Observable<{ isSaved: boolean }> {
    return this.http.post<{ isSaved: boolean }>(`${this.baseUrl}/${vehicleId}/favorite`, null);
  }

  getMyFavorites(): Observable<Favorites> {
    return this.http.get<Favorites>(`${this.baseUrl}/favorites`);
  }

  /** Alerta de bajada de precio de un favorito concreto. */
  setFavoriteAlert(vehicleId: string, enabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${vehicleId}/favorite/alert`, { enabled });
  }

  /** Interruptor general: todos los favoritos reciben alertas. */
  setAllFavoriteAlerts(enabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/favorites/alerts`, { enabled });
  }

  // ─── IA generativa ──────────────────────────────────────────────────────
  previewAiDescription(context: VehicleAiContext): Observable<AiVehicleDescription> {
    return this.http.post<AiVehicleDescription>(`${this.baseUrl}/ai/preview-description`, context);
  }

  extractDocument(file: File): Observable<AiDocumentExtraction> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<AiDocumentExtraction>(`${this.baseUrl}/ai/extract-document`, form);
  }
}

export interface VehicleAiContext {
  make: string;
  model: string | null;
  year: number;
  mileage: number | null;
  fuelType: string | null;
  transmission: string | null;
  bodyType: string | null;
  color: string | null;
  condition: string;
  price: number;
  currency: string;
  countryOrigin: string;
  isExportReady: boolean;
}

export interface AiVehicleDescription {
  descriptionEs: string;
  descriptionEn: string;
}

export interface AiDocumentExtraction {
  vin: string | null;
  make: string | null;
  model: string | null;
  year: number | null;
  licensePlate: string | null;
  color: string | null;
  mileage: number | null;
  fuelType: string | null;
  rawJson: string | null;
}
