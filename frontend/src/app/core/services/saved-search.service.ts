import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { VehicleFilters } from './vehicle.service';

export interface SavedSearch {
  id: string;
  name: string;
  /** Filtros exactos guardados: sirven para el resumen y para reabrir la búsqueda. */
  filters: VehicleFilters;
  alertEnabled: boolean;
  /** "23 véhicules disponibles" en el momento de consultar. */
  resultsCount: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class SavedSearchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/saved-searches`;

  getAll(): Observable<SavedSearch[]> {
    return this.http.get<SavedSearch[]>(this.baseUrl);
  }

  create(name: string, filters: VehicleFilters, alertEnabled = true): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, { name, filters, alertEnabled });
  }

  update(id: string, name: string, filters: VehicleFilters): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, { name, filters });
  }

  /** Alerte nouveaux véhicules: ON/OFF. */
  setAlert(id: string, enabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/alert`, { enabled });
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
