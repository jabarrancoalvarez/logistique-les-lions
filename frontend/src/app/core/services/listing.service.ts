import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { VehicleStatus, FeaturedTier } from './vehicle.service';

/**
 * «Mes annonces»: los vehículos que el usuario está vendiendo o ha vendido.
 *
 * Es lo contrario de Mon Garage, que son los coches que posee. Un mismo vehículo puede
 * estar en los dos sitios.
 */
export interface MyListing {
  id: string;
  slug: string;
  publicReference: string;
  title: string;
  status: VehicleStatus;
  price: number;
  mileage: number | null;
  thumbnailUrl: string | null;

  viewsCount: number;
  favoritesCount: number;
  contactsCount: number;
  /** Negociaciones abiertas: desde aquí se llega a ellas. */
  negotiationCount: number;

  qualityScore: number;
  createdAt: string;
  publishedAt: string | null;
  soldAt: string | null;

  /** Nivel de destacado vigente ('Aucune' si no lo está o ha caducado). */
  featuredTier: FeaturedTier;
  /** Hasta cuándo dura el destacado, para «En vedette jusqu'au…». */
  featuredUntil: string | null;
}

export interface MyListings {
  /** Para las pestañas: cuántos anuncios hay en cada estado. */
  countByStatus: Partial<Record<VehicleStatus, number>>;
  listings: MyListing[];
}

// ─── Qualité de l'annonce ──────────────────────────────────────────────────
export type ListingQualityCheck =
  | 'Photos' | 'Description' | 'Price' | 'Mileage'
  | 'Location' | 'Specifications' | 'Equipment';

export type ListingQualityStatus = 'Missing' | 'Partial' | 'Complete';

export interface ListingQualityItem {
  check: ListingQualityCheck;
  status: ListingQualityStatus;
  points: number;
  maxPoints: number;
  detail: number | null;
}

export interface ListingQuality {
  score: number;
  items: ListingQualityItem[];
}

@Injectable({ providedIn: 'root' })
export class ListingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/listings`;

  getMyListings(status?: VehicleStatus): Observable<MyListings> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<MyListings>(this.baseUrl, { params });
  }

  getQuality(id: string): Observable<ListingQuality> {
    return this.http.get<ListingQuality>(`${this.baseUrl}/${id}/quality`);
  }

  /** Publier · Pausar · Reactivar · Réservé · Vendu · Archiver. */
  changeStatus(id: string, status: VehicleStatus): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/status`, { status });
  }

  updatePrice(id: string, price: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/price`, { price });
  }

  updateMileage(id: string, mileage: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/mileage`, { mileage });
  }

  /** «Mettre en avant»: destaca el anuncio (En vedette / À la une, 15 o 30 días). */
  feature(id: string, tier: Exclude<FeaturedTier, 'Aucune'>, durationDays: 15 | 30): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/feature`, { tier, durationDays });
  }

  /** «Retirer la mise en avant». */
  unfeature(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/feature`);
  }

  duplicate(id: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/${id}/duplicate`, null);
  }

  /** Orden completo; la primera fotografía pasa a ser la principal. */
  reorderImages(id: string, imageIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/images/order`, { imageIds });
  }
}
