import {
  Component, OnInit, ChangeDetectionStrategy, inject, signal, computed
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { VehicleService, VehicleMake } from '../../../core/services/vehicle.service';
import { CountryService, SupportedCountry } from '../../../core/services/country.service';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'lll-hero-search',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './hero-search.component.html'
})
export class HeroSearchComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly vehicleService = inject(VehicleService);
  private readonly countryService = inject(CountryService);

  readonly searchForm: FormGroup = this.fb.group({
    make: [''],
    model: [''],
    originCountry: [''],
    destinationCountry: [''],
    priceMin: [''],
    priceMax: [''],
    condition: ['']
  });

  // Datos del formulario
  readonly makes = signal<VehicleMake[]>([]);
  readonly countries = signal<SupportedCountry[]>([]);
  readonly filteredMakes = signal<VehicleMake[]>([]);
  readonly makeSearch = signal('');
  readonly isLoading = signal(false);

  // Palabras del hero que rotan
  private readonly heroWords = ['inteligente', 'internacional', 'documentada', 'garantizada'];
  readonly heroWordIndex = signal(0);
  readonly currentHeroWord = computed(() => this.heroWords[this.heroWordIndex()]);

  ngOnInit(): void {
    // Cargar datos
    this.vehicleService.getMakes(true).subscribe(makes => {
      this.makes.set(makes);
      this.filteredMakes.set(makes);
    });

    this.countryService.getSupportedCountries().subscribe(countries => {
      this.countries.set(countries);
    });

    // Filtrado reactivo de marcas
    this.searchForm.get('make')!.valueChanges.pipe(
      debounceTime(200),
      distinctUntilChanged()
    ).subscribe(value => {
      if (!value) {
        this.filteredMakes.set(this.makes());
        return;
      }
      const q = value.toLowerCase();
      this.filteredMakes.set(
        this.makes().filter(m => m.name.toLowerCase().includes(q))
      );
    });

    // Rotar palabras del hero cada 3 segundos
    setInterval(() => {
      this.heroWordIndex.update(i => (i + 1) % this.heroWords.length);
    }, 3000);
  }

  readonly quickSearches: { label: string; params: Record<string, string> }[] = [
    { label: 'BMW Alemania', params: { make: 'BMW', country: 'DE' } },
    { label: 'Tesla eléctrico', params: { make: 'Tesla', fuelType: 'Electric' } },
    { label: 'SUV Francia', params: { bodyType: 'SUV', country: 'FR' } },
    { label: 'Japoneses importados', params: { country: 'JP' } }
  ];

  readonly trustBadges = [
    { text: 'Documentación 100% gestionada', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z' },
    { text: 'Pago con escrow seguro', icon: 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z' },
    { text: '+1.200 transacciones', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z' },
    { text: 'Soporte en 5 idiomas', icon: 'M3 5h12M9 3v2m1.048 9.5A18.022 18.022 0 016.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129' }
  ];

  applyQuickSearch(quick: { label: string; params: Record<string, string> }): void {
    this.router.navigate(['/vehiculos'], { queryParams: quick.params });
  }

  onSearch(): void {
    const params: Record<string, string> = {};
    const form = this.searchForm.value;

    if (form.make) params['make'] = form.make;
    if (form.model) params['model'] = form.model;
    if (form.originCountry) params['country'] = form.originCountry;
    if (form.destinationCountry) params['destination'] = form.destinationCountry;
    if (form.priceMin) params['priceMin'] = form.priceMin;
    if (form.priceMax) params['priceMax'] = form.priceMax;
    if (form.condition) params['condition'] = form.condition;

    this.router.navigate(['/vehiculos'], { queryParams: params });
  }
}
