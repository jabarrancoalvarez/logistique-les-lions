import { Component, OnInit, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CountryService, SupportedCountry } from '../../../core/services/country.service';

@Component({
  selector: 'lll-country-map',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule],
  templateUrl: './country-map.component.html'
})
export class CountryMapComponent implements OnInit {
  private readonly countryService = inject(CountryService);

  // Inicializamos con datos de ejemplo — se reemplazan si la API responde
  readonly countries = signal<SupportedCountry[]>(this.getMockCountries());
  readonly selectedCountry = signal<SupportedCountry | null>(null);

  ngOnInit(): void {
    this.countryService.getSupportedCountries().subscribe({
      next: countries => {
        if (countries.length > 0) {
          this.countries.set(countries);
        }
        // Si la API devuelve vacío, mantenemos los ejemplos ya cargados
      },
      error: () => {
        // Mantenemos los ejemplos ya cargados, no hay nada que hacer
      }
    });
  }

  selectCountry(country: SupportedCountry): void {
    this.selectedCountry.set(
      this.selectedCountry()?.code === country.code ? null : country
    );
  }

  private getMockCountries(): SupportedCountry[] {
    return [
      { code: 'ES', nameEs: 'España', nameEn: 'Spain', flagEmoji: '🇪🇸', currency: 'EUR', isEuMember: true, supportsImport: true, supportsExport: true, displayOrder: 1 },
      { code: 'DE', nameEs: 'Alemania', nameEn: 'Germany', flagEmoji: '🇩🇪', currency: 'EUR', isEuMember: true, supportsImport: true, supportsExport: true, displayOrder: 2 },
      { code: 'FR', nameEs: 'Francia', nameEn: 'France', flagEmoji: '🇫🇷', currency: 'EUR', isEuMember: true, supportsImport: true, supportsExport: true, displayOrder: 3 },
      { code: 'IT', nameEs: 'Italia', nameEn: 'Italy', flagEmoji: '🇮🇹', currency: 'EUR', isEuMember: true, supportsImport: true, supportsExport: true, displayOrder: 4 },
      { code: 'PT', nameEs: 'Portugal', nameEn: 'Portugal', flagEmoji: '🇵🇹', currency: 'EUR', isEuMember: true, supportsImport: true, supportsExport: true, displayOrder: 5 },
      { code: 'NL', nameEs: 'Países Bajos', nameEn: 'Netherlands', flagEmoji: '🇳🇱', currency: 'EUR', isEuMember: true, supportsImport: true, supportsExport: true, displayOrder: 6 },
      { code: 'BE', nameEs: 'Bélgica', nameEn: 'Belgium', flagEmoji: '🇧🇪', currency: 'EUR', isEuMember: true, supportsImport: true, supportsExport: true, displayOrder: 7 },
      { code: 'GB', nameEs: 'Reino Unido', nameEn: 'United Kingdom', flagEmoji: '🇬🇧', currency: 'GBP', isEuMember: false, supportsImport: true, supportsExport: true, displayOrder: 8 },
      { code: 'US', nameEs: 'Estados Unidos', nameEn: 'United States', flagEmoji: '🇺🇸', currency: 'USD', isEuMember: false, supportsImport: true, supportsExport: false, displayOrder: 9 },
      { code: 'MA', nameEs: 'Marruecos', nameEn: 'Morocco', flagEmoji: '🇲🇦', currency: 'MAD', isEuMember: false, supportsImport: false, supportsExport: true, displayOrder: 10 },
      { code: 'JP', nameEs: 'Japón', nameEn: 'Japan', flagEmoji: '🇯🇵', currency: 'JPY', isEuMember: false, supportsImport: true, supportsExport: false, displayOrder: 11 },
      { code: 'CH', nameEs: 'Suiza', nameEn: 'Switzerland', flagEmoji: '🇨🇭', currency: 'CHF', isEuMember: false, supportsImport: true, supportsExport: true, displayOrder: 12 }
    ];
  }
}
