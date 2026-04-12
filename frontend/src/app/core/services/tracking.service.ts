import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@environments/environment';

export interface PublicTracking {
  trackingCode: string;
  status: string;
  completionPercent: number;
  originCountry: string;
  destinationCountry: string;
  processType: string;
  startedAt?: string;
  completedAt?: string;
  lastUpdatedAt: string;
  vehicleTitle: string;
}

@Injectable({ providedIn: 'root' })
export class TrackingService {
  private readonly http   = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/public/tracking`;

  getByCode(code: string): Observable<PublicTracking> {
    return this.http.get<{ isSuccess: boolean; value: PublicTracking }>(
      `${this.apiUrl}/${encodeURIComponent(code.trim().toUpperCase())}`
    ).pipe(map(r => r.value));
  }
}
