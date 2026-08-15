import {
  Component, ChangeDetectionStrategy, signal, computed, inject,
  input, output, OnInit, effect
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '@core/auth/auth.service';
import { SavedSearchService } from '@core/services/saved-search.service';
import { suggestSearchName } from '@shared/data/search-summary';
import {
  VehicleFilters, VehicleMake, VehicleService, VehicleModelOption,
  FUEL_LABELS, TRANSMISSION_LABELS, BODY_LABELS, CUSTOMS_LABELS, DRIVETRAIN_LABELS,
  FuelType, TransmissionType, BodyType, CustomsStatus, Drivetrain, AccountTypeFilter
} from '@core/services/vehicle.service';
import { SENEGAL_REGIONS, citiesOfRegion } from '@shared/data/senegal-geo';

/** Convierte un `Record<K, string>` de etiquetas en una lista para un `<select>`. */
function optionsFrom<K extends string>(labels: Record<K, string>): { value: K; label: string }[] {
  return (Object.keys(labels) as K[]).map(value => ({ value, label: labels[value] }));
}

/** Los tramos de puertas y plazas de la especificación. */
const DOOR_RANGES = [
  { value: '2-3', label: '2 / 3', from: 2, to: 3 },
  { value: '4-5', label: '4 / 5', from: 4, to: 5 }
];

const SEAT_RANGES = [
  { value: '2',  label: '2',  from: 2, to: 2 },
  { value: '3',  label: '3',  from: 3, to: 3 },
  { value: '4',  label: '4',  from: 4, to: 4 },
  { value: '5',  label: '5',  from: 5, to: 5 },
  { value: '7',  label: '7',  from: 7, to: 7 },
  { value: '8+', label: '8 et plus', from: 8, to: undefined }
];

@Component({
  selector: 'lll-filter-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink],
  templateUrl: './filter-panel.component.html'
})
export class FilterPanelComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly savedSearches = inject(SavedSearchService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly filters = input<VehicleFilters>({});
  readonly filtersChange = output<Partial<VehicleFilters>>();

  // ─── Catálogos ─────────────────────────────────────────────────────────
  readonly makes = signal<VehicleMake[]>([]);
  readonly models = signal<VehicleModelOption[]>([]);
  readonly equipments = signal<{ id: string; code: string; name: string }[]>([]);
  readonly colors = signal<string[]>([]);
  readonly regions = SENEGAL_REGIONS;
  readonly cities = computed(() => citiesOfRegion(this.region));

  readonly fuelTypes = optionsFrom(FUEL_LABELS);
  readonly transmissions = optionsFrom(TRANSMISSION_LABELS);
  readonly bodyTypes = optionsFrom(BODY_LABELS).filter(o => o.value !== 'Autre');
  readonly customsStatuses = optionsFrom(CUSTOMS_LABELS);
  readonly drivetrains = optionsFrom(DRIVETRAIN_LABELS);
  readonly doorRanges = DOOR_RANGES;
  readonly seatRanges = SEAT_RANGES;

  readonly sellerTypes: { value: '' | AccountTypeFilter; label: string }[] = [
    { value: '',              label: 'Tous' },
    { value: 'Particulier',   label: 'Particulier' },
    { value: 'Professionnel', label: 'Professionnel' }
  ];

  readonly currentYear = new Date().getFullYear();
  readonly years = Array.from({ length: this.currentYear - 1989 }, (_, i) => this.currentYear - i);

  /** Los filtros avanzados se pliegan para no saturar la pantalla en móvil. */
  readonly advancedOpen = signal(false);

  // ─── Estado editable, aún sin aplicar ──────────────────────────────────
  search = '';
  makeId = '';
  modelId = '';
  priceFrom = '';
  priceTo = '';
  yearFrom = '';
  yearTo = '';
  mileageFrom = '';
  mileageTo = '';
  region = '';
  city = '';
  customsStatus: '' | CustomsStatus = '';
  fuelType: '' | FuelType = '';
  transmission: '' | TransmissionType = '';
  bodyType: '' | BodyType = '';
  drivetrain: '' | Drivetrain = '';
  powerFrom = '';
  powerTo = '';
  displacementFrom = '';
  displacementTo = '';
  doorRange = '';
  seatRange = '';
  color = '';
  sellerAccountType: '' | AccountTypeFilter = '';
  selectedEquipments = new Set<string>();

  /**
   * Recuento en vivo: la especificación exige indicar siempre cuántos resultados
   * producen los filtros antes o inmediatamente después de aplicarlos.
   */
  readonly previewCount = signal<number | null>(null);
  readonly countingError = signal(false);
  private readonly recount$ = new Subject<VehicleFilters>();

  constructor() {
    this.recount$
      .pipe(
        // Evita una petición por cada tecla mientras se escribe en la búsqueda.
        debounceTime(350),
        switchMap(f => this.vehicleService.countVehicles(f)),
        takeUntilDestroyed()
      )
      .subscribe({
        next: count => { this.previewCount.set(count); this.countingError.set(false); },
        error: () => { this.previewCount.set(null); this.countingError.set(true); }
      });

    // Al cambiar de marca hay que recargar los modelos y olvidar el anterior.
    effect(() => {
      const makeId = this.makeIdSignal();
      if (!makeId) { this.models.set([]); return; }
      this.vehicleService.getModels(makeId).subscribe(m => this.models.set(m));
    });
  }

  /** Espejo del `makeId` en un signal para poder reaccionar desde un `effect`. */
  private readonly makeIdSignal = signal('');

  ngOnInit(): void {
    this.vehicleService.getMakes(false).subscribe(makes => this.makes.set(makes));
    this.vehicleService.getFilterOptions().subscribe(o => {
      this.equipments.set(o.equipments);
      this.colors.set(o.colors);
    });

    this.hydrate(this.filters());
    this.requestCount();
  }

  /** Carga el estado editable desde los filtros ya aplicados (p. ej. al volver por URL). */
  private hydrate(f: VehicleFilters): void {
    this.search = f.search ?? '';
    this.makeId = f.makeId ?? '';
    this.makeIdSignal.set(this.makeId);
    this.modelId = f.modelId ?? '';
    this.priceFrom = f.priceFrom?.toString() ?? '';
    this.priceTo = f.priceTo?.toString() ?? '';
    this.yearFrom = f.yearFrom?.toString() ?? '';
    this.yearTo = f.yearTo?.toString() ?? '';
    this.mileageFrom = f.mileageFrom?.toString() ?? '';
    this.mileageTo = f.mileageTo?.toString() ?? '';
    this.region = f.region ?? '';
    this.city = f.city ?? '';
    this.customsStatus = f.customsStatus ?? '';
    this.fuelType = f.fuelType ?? '';
    this.transmission = f.transmission ?? '';
    this.bodyType = f.bodyType ?? '';
    this.drivetrain = f.drivetrain ?? '';
    this.powerFrom = f.powerFrom?.toString() ?? '';
    this.powerTo = f.powerTo?.toString() ?? '';
    this.displacementFrom = f.displacementFrom?.toString() ?? '';
    this.displacementTo = f.displacementTo?.toString() ?? '';
    this.color = f.color ?? '';
    this.sellerAccountType = f.sellerAccountType ?? '';
    this.selectedEquipments = new Set(f.equipmentIds ?? []);
    this.doorRange = DOOR_RANGES.find(r => r.from === f.doorsFrom)?.value ?? '';
    this.seatRange = SEAT_RANGES.find(r => r.from === f.seatsFrom)?.value ?? '';
  }

  onMakeChange(): void {
    this.makeIdSignal.set(this.makeId);
    // El modelo depende de la marca: al cambiarla, la selección previa ya no es válida.
    this.modelId = '';
    this.requestCount();
  }

  onRegionChange(): void {
    this.city = '';
    this.requestCount();
  }

  toggleEquipment(id: string): void {
    this.selectedEquipments.has(id)
      ? this.selectedEquipments.delete(id)
      : this.selectedEquipments.add(id);
    this.requestCount();
  }

  isEquipmentSelected(id: string): boolean {
    return this.selectedEquipments.has(id);
  }

  toggleAdvanced(): void {
    this.advancedOpen.update(v => !v);
  }

  /** Filtros tal y como se enviarán. */
  private build(): Partial<VehicleFilters> {
    const num = (v: string) => (v ? +v : undefined);
    const doors = DOOR_RANGES.find(r => r.value === this.doorRange);
    const seats = SEAT_RANGES.find(r => r.value === this.seatRange);

    return {
      search:            this.search.trim() || undefined,
      makeId:            this.makeId || undefined,
      modelId:           this.modelId || undefined,
      priceFrom:         num(this.priceFrom),
      priceTo:           num(this.priceTo),
      yearFrom:          num(this.yearFrom),
      yearTo:            num(this.yearTo),
      mileageFrom:       num(this.mileageFrom),
      mileageTo:         num(this.mileageTo),
      region:            this.region || undefined,
      city:              this.city || undefined,
      customsStatus:     this.customsStatus || undefined,
      fuelType:          this.fuelType || undefined,
      transmission:      this.transmission || undefined,
      bodyType:          this.bodyType || undefined,
      drivetrain:        this.drivetrain || undefined,
      powerFrom:         num(this.powerFrom),
      powerTo:           num(this.powerTo),
      displacementFrom:  num(this.displacementFrom),
      displacementTo:    num(this.displacementTo),
      doorsFrom:         doors?.from,
      doorsTo:           doors?.to,
      seatsFrom:         seats?.from,
      seatsTo:           seats?.to,
      color:             this.color || undefined,
      sellerAccountType: this.sellerAccountType || undefined,
      equipmentIds:      this.selectedEquipments.size > 0
        ? [...this.selectedEquipments]
        : undefined
    };
  }

  /** Pide el recuento con los filtros actuales, sin aplicarlos todavía. */
  requestCount(): void {
    this.recount$.next(this.build() as VehicleFilters);
  }

  apply(): void {
    this.filtersChange.emit(this.build());
  }

  reset(): void {
    this.hydrate({});
    this.models.set([]);
    this.filtersChange.emit({
      search: undefined, makeId: undefined, modelId: undefined,
      priceFrom: undefined, priceTo: undefined,
      yearFrom: undefined, yearTo: undefined,
      mileageFrom: undefined, mileageTo: undefined,
      region: undefined, city: undefined, customsStatus: undefined,
      fuelType: undefined, transmission: undefined, bodyType: undefined,
      drivetrain: undefined, powerFrom: undefined, powerTo: undefined,
      displacementFrom: undefined, displacementTo: undefined,
      doorsFrom: undefined, doorsTo: undefined,
      seatsFrom: undefined, seatsTo: undefined,
      color: undefined, sellerAccountType: undefined, equipmentIds: undefined
    });
    this.requestCount();
  }

  // ─── Enregistrer la recherche ──────────────────────────────────────────
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly saveError = signal<string | null>(null);

  /** Nombre propuesto a partir de la marca y el modelo seleccionados. */
  private suggestedName(): string {
    const make = this.makes().find(m => m.id === this.makeId)?.name ?? null;
    const model = this.models().find(m => m.id === this.modelId)?.name ?? null;
    return suggestSearchName(this.build() as VehicleFilters, make, model);
  }

  /**
   * Guarda los filtros actuales. Sin cuenta, lleva al registro conservando la búsqueda
   * en la URL para poder retomarla al volver.
   */
  saveSearch(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/auth/register'], {
        queryParams: { returnUrl: this.router.url }
      });
      return;
    }

    if (this.saving()) return;

    this.saving.set(true);
    this.saveError.set(null);

    this.savedSearches
      .create(this.suggestedName(), this.build() as VehicleFilters)
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.saved.set(true);
          setTimeout(() => this.saved.set(false), 4000);
        },
        error: err => {
          this.saving.set(false);
          this.saveError.set(err?.error === 'SavedSearch.LimitReached'
            ? 'Vous avez atteint le nombre maximum de recherches enregistrées.'
            : 'Impossible d\'enregistrer la recherche.');
        }
      });
  }
}
