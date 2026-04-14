import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

// ─── Enums (mirror from backend) ─────────────────────────────────────────────
export type VehicleCondition = 'New' | 'Used' | 'Km0';
export type FuelType = 'Gasoline' | 'Diesel' | 'Electric' | 'Hybrid' | 'PluginHybrid' | 'Lpg' | 'Cng' | 'Hydrogen';
export type TransmissionType = 'Manual' | 'Automatic' | 'SemiAutomatic' | 'Cvt';
export type BodyType = 'Sedan' | 'Hatchback' | 'Suv' | 'Coupe' | 'Convertible' | 'Wagon' | 'Van' | 'Truck' | 'Minivan' | 'Pickup';
export type VehicleStatus = 'Reviewing' | 'Active' | 'Sold' | 'Paused' | 'Rejected' | 'Expired';

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
  slug: string;
  title: string;
  makeName: string;
  modelName: string | null;
  year: number;
  mileage: number | null;
  price: number;
  currency: string;
  countryOrigin: string;
  flagEmoji: string | null;
  condition: VehicleCondition;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  bodyType: BodyType | null;
  primaryImageUrl: string | null;
  thumbnailUrl: string | null;
  isFeatured: boolean;
  isExportReady: boolean;
  favoritesCount: number;
  viewsCount: number;
  createdAt: string;
  status?: VehicleStatus;
  sellerId?: string;
}

export interface VehicleImage {
  id: string;
  url: string;
  thumbnailUrl: string | null;
  isPrimary: boolean;
  sortOrder: number;
  altText: string | null;
}

export interface VehicleDetail {
  id: string;
  slug: string;
  title: string;
  descriptionEs: string | null;
  descriptionEn: string | null;
  makeId: string;
  makeName: string;
  modelId: string | null;
  modelName: string | null;
  year: number;
  mileage: number | null;
  condition: VehicleCondition;
  bodyType: BodyType | null;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  color: string | null;
  vin: string | null;
  price: number;
  currency: string;
  priceNegotiable: boolean;
  countryOrigin: string;
  countryName: string | null;
  flagEmoji: string | null;
  city: string | null;
  postalCode: string | null;
  status: VehicleStatus;
  isFeatured: boolean;
  isExportReady: boolean;
  specs: Record<string, unknown> | null;
  features: string[] | null;
  viewsCount: number;
  favoritesCount: number;
  contactsCount: number;
  expiresAt: string | null;
  soldAt: string | null;
  createdAt: string;
  images: VehicleImage[];
  sellerId: string;
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

export interface VehicleFilters {
  search?: string;
  makeId?: string;
  modelId?: string;
  yearFrom?: number;
  yearTo?: number;
  priceFrom?: number;
  priceTo?: number;
  countryOrigin?: string;
  condition?: VehicleCondition;
  fuelType?: FuelType;
  transmission?: TransmissionType;
  bodyType?: BodyType;
  isExportReady?: boolean;
  isFeatured?: boolean;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
  sellerId?: string;
  status?: VehicleStatus;
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

  // ─── M2 endpoints ──────────────────────────────────────────────────────
  getVehicles(filters: VehicleFilters = {}): Observable<PagedResult<VehicleListItem>> {
    let params = new HttpParams();
    Object.entries(filters).forEach(([key, val]) => {
      if (val !== undefined && val !== null && val !== '') {
        params = params.set(key, String(val));
      }
    });
    return this.http.get<PagedResult<VehicleListItem>>(this.baseUrl, { params });
  }

  getVehicleBySlug(slug: string): Observable<VehicleDetail> {
    return this.http.get<VehicleDetail>(`${this.baseUrl}/${slug}`);
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

  deleteVehicle(id: string, requesterId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`, {
      params: { requesterId }
    });
  }

  uploadImage(vehicleId: string, data: unknown): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/${vehicleId}/images`, data);
  }

  toggleFavorite(vehicleId: string, userId: string): Observable<{ isSaved: boolean }> {
    return this.http.post<{ isSaved: boolean }>(`${this.baseUrl}/${vehicleId}/favorite`, null, {
      params: { userId }
    });
  }

  getMyFavorites(userId: string): Observable<VehicleListItem[]> {
    return this.http.get<VehicleListItem[]>(`${this.baseUrl}/favorites`, {
      params: { userId }
    });
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
