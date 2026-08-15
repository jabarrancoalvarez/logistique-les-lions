import {
  Component, OnInit, OnDestroy, ChangeDetectionStrategy, inject, signal, computed
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { VehicleService, VehicleMake } from '@core/services/vehicle.service';
import { SENEGAL_REGIONS } from '@shared/data/senegal-geo';

/**
 * Buscador de la portada.
 *
 * Los campos son los que importan en Senegal: marca, región, presupuesto en FCFA y
 * estado aduanero. Se han retirado «país de origen» y «país destino» del producto
 * anterior: aquí el vehículo ya está en el país.
 */
@Component({
  selector: 'lll-hero-search',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './hero-search.component.html'
})
export class HeroSearchComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly vehicleService = inject(VehicleService);

  readonly searchForm: FormGroup = this.fb.group({
    make: [''],
    region: [''],
    priceMax: [''],
    customsStatus: ['']
  });

  readonly makes = signal<VehicleMake[]>([]);
  readonly filteredMakes = signal<VehicleMake[]>([]);
  readonly regions = SENEGAL_REGIONS;

  /** Palabras que rotan en el titular. */
  private readonly heroWords = ['en confiance', 'sans frais', 'au Sénégal', 'entre nous'];
  readonly heroWordIndex = signal(0);
  readonly currentHeroWord = computed(() => this.heroWords[this.heroWordIndex()]);

  private rotation?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.vehicleService.getMakes(true).subscribe(makes => {
      this.makes.set(makes);
      this.filteredMakes.set(makes);
    });

    this.searchForm.get('make')!.valueChanges.pipe(
      debounceTime(200),
      distinctUntilChanged()
    ).subscribe(value => {
      if (!value) {
        this.filteredMakes.set(this.makes());
        return;
      }
      const q = value.toLowerCase();
      this.filteredMakes.set(this.makes().filter(m => m.name.toLowerCase().includes(q)));
    });

    this.rotation = setInterval(() => {
      this.heroWordIndex.update(i => (i + 1) % this.heroWords.length);
    }, 3000);
  }

  // Sin esto el intervalo sigue vivo tras salir de la portada.
  ngOnDestroy(): void {
    if (this.rotation) clearInterval(this.rotation);
  }

  /** Los parámetros deben coincidir con los que entiende el Marketplace. */
  readonly quickSearches: { label: string; params: Record<string, string> }[] = [
    { label: 'Toyota Hilux', params: { search: 'Hilux' } },
    { label: 'Dédouané',     params: { customsStatus: 'Dedouane' } },
    { label: 'SUV / 4x4',    params: { bodyType: 'Suv' } },
    { label: 'Dakar',        params: { region: 'DK' } }
  ];

  /**
   * Lo que la plataforma garantiza de verdad.
   *
   * ❌ Nada de cifras de transacciones ni de servicios que no existen: cada línea de
   * aquí corresponde a algo implementado.
   */
  readonly trustBadges = [
    {
      text: 'Gratuit et sans limite',
      icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'
    },
    {
      text: 'Contrat vérifié par les deux parties',
      icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z'
    },
    {
      text: 'Statut douanier sur chaque annonce',
      icon: 'M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z'
    }
  ];

  applyQuickSearch(quick: { label: string; params: Record<string, string> }): void {
    this.router.navigate(['/vehiculos'], { queryParams: quick.params });
  }

  onSearch(): void {
    const params: Record<string, string> = {};
    const form = this.searchForm.value;

    // La marca va a la barra de búsqueda, que ya busca sobre marca, modelo y versión:
    // enviarla como makeId exigiría resolver aquí el identificador del catálogo.
    if (form.make?.trim()) params['search'] = form.make.trim();
    if (form.region) params['region'] = form.region;
    if (form.priceMax) params['priceTo'] = form.priceMax;
    if (form.customsStatus) params['customsStatus'] = form.customsStatus;

    this.router.navigate(['/vehiculos'], { queryParams: params });
  }
}
