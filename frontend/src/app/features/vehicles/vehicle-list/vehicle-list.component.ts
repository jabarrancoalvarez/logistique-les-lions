import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VehicleService, VehicleListItem, VehicleFilters } from '@core/services/vehicle.service';
import { ComparatorService } from '@core/services/comparator.service';
import { VehicleCardComponent } from '@shared/components/vehicle-card/vehicle-card.component';
import { FilterPanelComponent } from '../filter-panel/filter-panel.component';

@Component({
  selector: 'lll-vehicle-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, VehicleCardComponent, FilterPanelComponent],
  templateUrl: './vehicle-list.component.html'
})
export class VehicleListComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  /** Barra flotante «Comparer (n/3)» cuando hay selección. */
  readonly comparator = inject(ComparatorService);

  readonly vehicles = signal<VehicleListItem[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly showFilters = signal(false);

  readonly filters = signal<VehicleFilters>({
    page: 1,
    pageSize: 20,
    sortBy: 'createdAt',
    sortDesc: true
  });

  readonly currentPage = computed(() => this.filters().page ?? 1);
  readonly hasResults = computed(() => this.vehicles().length > 0);
  readonly isEmpty = computed(() => !this.isLoading() && this.vehicles().length === 0);

  /** Las cinco ordenaciones de la especificación funcional, en este orden. */
  readonly sortOptions = [
    { value: 'createdAt-desc', label: 'Plus récentes' },
    { value: 'price-asc',      label: 'Prix : du moins cher au plus cher' },
    { value: 'price-desc',     label: 'Prix : du plus cher au moins cher' },
    { value: 'mileage-asc',    label: 'Kilométrage : du plus faible au plus élevé' },
    { value: 'year-desc',      label: 'Année : les plus récentes' },
  ];

  /** Filtros que llegan como número en la URL. */
  private static readonly NUMERIC_KEYS = [
    'priceFrom', 'priceTo', 'yearFrom', 'yearTo', 'mileageFrom', 'mileageTo',
    'powerFrom', 'powerTo', 'displacementFrom', 'displacementTo',
    'doorsFrom', 'doorsTo', 'seatsFrom', 'seatsTo'
  ] as const;

  ngOnInit(): void {
    // La URL es la única fuente de verdad: así los filtros sobreviven a un refresco,
    // al botón atrás y al compartir el enlace de una búsqueda.
    this.route.queryParams.subscribe(params => {
      this.filters.set(this.parseParams(params));
      this.loadVehicles();
    });
  }

  private parseParams(params: Record<string, unknown>): VehicleFilters {
    const f: Record<string, unknown> = {};

    for (const [key, raw] of Object.entries(params)) {
      if (raw === undefined || raw === null || raw === '') continue;

      if (key === 'equipmentIds') {
        // Angular entrega un string si el parámetro aparece una sola vez.
        f[key] = Array.isArray(raw) ? raw : [raw];
      } else if ((VehicleListComponent.NUMERIC_KEYS as readonly string[]).includes(key)) {
        const n = Number(raw);
        if (Number.isFinite(n)) f[key] = n;
      } else if (key === 'sortDesc') {
        f[key] = raw !== 'false';
      } else if (key !== 'page' && key !== 'pageSize') {
        f[key] = raw;
      }
    }

    return {
      ...f,
      page:     params['page'] ? Number(params['page']) : 1,
      pageSize: 20,
      sortBy:   (params['sortBy'] as string) ?? 'createdAt',
      sortDesc: params['sortDesc'] !== 'false'
    } as VehicleFilters;
  }

  loadVehicles(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.vehicleService.getVehicles(this.filters()).subscribe({
      next: result => {
        this.vehicles.set(result.items);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Impossible de charger les véhicules. Veuillez réessayer.');
        this.isLoading.set(false);
      }
    });
  }

  onFiltersChange(newFilters: Partial<VehicleFilters>): void {
    this.navigateWith({ ...newFilters, page: 1 });
  }

  onSortChange(sortValue: string): void {
    const [sortBy, sortDir] = sortValue.split('-');
    this.navigateWith({ sortBy, sortDesc: sortDir === 'desc', page: 1 });
  }

  goToPage(page: number): void {
    this.navigateWith({ page });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /**
   * Actualiza la URL con los filtros nuevos. No recarga aquí: la suscripción a
   * `queryParams` es la que dispara la carga, de modo que no haya dos peticiones
   * por cada cambio de filtro.
   */
  private navigateWith(changes: Partial<VehicleFilters>): void {
    const merged: Record<string, unknown> = { ...this.filters(), ...changes };

    // `null` borra el parámetro de la URL; dejarlo en undefined lo conservaría.
    const queryParams: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(merged)) {
      if (key === 'pageSize') continue;
      queryParams[key] = value === undefined || value === '' ? null : value;
    }

    this.router.navigate([], { relativeTo: this.route, queryParams });
  }

  get sortValue(): string {
    const f = this.filters();
    return `${f.sortBy}-${f.sortDesc ? 'desc' : 'asc'}`;
  }

  get pageRange(): number[] {
    const current = this.currentPage();
    const total = this.totalPages();
    const range: number[] = [];
    const delta = 2;
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      range.push(i);
    }
    return range;
  }

  toggleFilters(): void {
    this.showFilters.update(v => !v);
  }
}
