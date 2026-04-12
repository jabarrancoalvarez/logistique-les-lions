import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VehicleService, VehicleListItem, VehicleFilters } from '@core/services/vehicle.service';
import { VehicleCardComponent } from '@shared/components/vehicle-card/vehicle-card.component';
import { FilterPanelComponent } from '../filter-panel/filter-panel.component';

@Component({
  selector: 'lll-vehicle-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, FormsModule, VehicleCardComponent, FilterPanelComponent],
  templateUrl: './vehicle-list.component.html'
})
export class VehicleListComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

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

  readonly sortOptions = [
    { value: 'createdAt-desc', label: 'Más recientes' },
    { value: 'price-asc', label: 'Precio: menor a mayor' },
    { value: 'price-desc', label: 'Precio: mayor a menor' },
    { value: 'year-desc', label: 'Año: más nuevo primero' },
    { value: 'mileage-asc', label: 'Menos kilometraje' },
    { value: 'views-desc', label: 'Más vistos' },
  ];

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const f: VehicleFilters = {
        search: params['search'],
        makeId: params['makeId'],
        countryOrigin: params['countryOrigin'],
        priceFrom: params['priceFrom'] ? +params['priceFrom'] : undefined,
        priceTo: params['priceTo'] ? +params['priceTo'] : undefined,
        yearFrom: params['yearFrom'] ? +params['yearFrom'] : undefined,
        yearTo: params['yearTo'] ? +params['yearTo'] : undefined,
        page: params['page'] ? +params['page'] : 1,
        pageSize: 20,
        sortBy: params['sortBy'] ?? 'createdAt',
        sortDesc: params['sortDesc'] !== 'false'
      };
      this.filters.set(f);
      this.loadVehicles();
    });
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
        this.error.set('Error al cargar los vehículos. Inténtalo de nuevo.');
        this.isLoading.set(false);
      }
    });
  }

  onFiltersChange(newFilters: Partial<VehicleFilters>): void {
    this.filters.update(f => ({ ...f, ...newFilters, page: 1 }));
    this.updateQueryParams();
    this.loadVehicles();
  }

  onSortChange(sortValue: string): void {
    const [sortBy, sortDir] = sortValue.split('-');
    this.filters.update(f => ({ ...f, sortBy, sortDesc: sortDir === 'desc', page: 1 }));
    this.loadVehicles();
  }

  goToPage(page: number): void {
    this.filters.update(f => ({ ...f, page }));
    this.updateQueryParams();
    this.loadVehicles();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private updateQueryParams(): void {
    const f = this.filters();
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ...f },
      queryParamsHandling: 'merge'
    });
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
