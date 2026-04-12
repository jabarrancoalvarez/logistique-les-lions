import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

export interface SupportedCountry {
  code: string;
  nameEs: string;
  nameEn: string;
  flagEmoji: string | null;
  currency: string;
  isEuMember: boolean;
  supportsImport: boolean;
  supportsExport: boolean;
  displayOrder: number;
}

@Injectable({ providedIn: 'root' })
export class CountryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/countries`;

  getSupportedCountries(): Observable<SupportedCountry[]> {
    return this.http.get<SupportedCountry[]>(`${this.baseUrl}/supported`);
  }
}
