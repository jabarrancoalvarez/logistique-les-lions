import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, of } from 'rxjs';
import { environment } from '@environments/environment';

/**
 * Lo que la aplicación necesita saber de sí misma.
 *
 * Los parámetros públicos vienen del servidor y no de constantes del código: el
 * documento pide poder cambiarlos «sin modificar el código». Se piden una vez y se
 * guardan en un signal, porque cambian cada varios meses, no cada petición.
 */
export interface PublicSettings {
  comparatorMaxVehicles: number;
  maxImagesPerListing: number;
  legalTermsVersion: string;
  features: Record<string, boolean>;
}

export interface UpcomingFeature {
  id: string;
  code: string;
  name: string;
  description: string | null;
  interestedCount: number;
  isInterested: boolean;
}

/** Valores de respaldo mientras la respuesta no llega o si el servidor falla. */
const FALLBACK: PublicSettings = {
  comparatorMaxVehicles: 3,
  maxImagesPerListing: 20,
  legalTermsVersion: '1.0',
  features: {}
};

@Injectable({ providedIn: 'root' })
export class PlatformService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/platform`;

  readonly settings = signal<PublicSettings>(FALLBACK);

  private loaded = false;

  /** Se pide una sola vez por sesión. */
  load(): Observable<PublicSettings> {
    if (this.loaded) return of(this.settings());

    return this.http.get<PublicSettings>(`${this.apiUrl}/settings`).pipe(
      tap(s => { this.settings.set(s); this.loaded = true; })
    );
  }

  /** Si el flag no existe todavía en base de datos, se considera encendido. */
  isEnabled(key: string): boolean {
    return this.settings().features[key] ?? true;
  }

  getUpcoming(): Observable<{ items: UpcomingFeature[] }> {
    return this.http.get<{ items: UpcomingFeature[] }>(`${this.apiUrl}/upcoming`);
  }

  setInterest(featureId: string, interested: boolean): Observable<{ interestedCount: number }> {
    return this.http.post<{ interestedCount: number }>(
      `${this.apiUrl}/upcoming/${featureId}/interest`, { interested });
  }
}
