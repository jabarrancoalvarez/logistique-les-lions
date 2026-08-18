import { Component, OnInit, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { VehicleService, FeaturedVehicle } from '../../../core/services/vehicle.service';
import { VehicleCardComponent } from '../../../shared/components/vehicle-card/vehicle-card.component';

@Component({
  selector: 'lll-featured-vehicles',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule, VehicleCardComponent],
  templateUrl: './featured-vehicles.component.html'
})
export class FeaturedVehiclesComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);

  /** Seis huecos por página; las flechas rotan por el resto de anuncios «À la une». */
  private static readonly PageSize = 6;

  /**
   * Vacío al arrancar. ❌ Nunca con datos de ejemplo: antes se sembraba con coches
   * europeos de muestra (BMW, Mercedes… en euros), que se veían mientras la API
   * respondía —sobre todo tras el arranque en frío de Render— y contradecían el
   * catálogo real de Senegal. Ahora se muestra un esqueleto de carga hasta que llegan
   * los anuncios de verdad.
   */
  readonly vehicles = signal<FeaturedVehicle[]>([]);
  readonly loading = signal(true);
  readonly page = signal(0);

  /** Marcadores de posición para el esqueleto mientras carga. */
  readonly skeletons = Array.from({ length: FeaturedVehiclesComponent.PageSize });

  readonly pageCount = computed(() =>
    Math.max(1, Math.ceil(this.vehicles().length / FeaturedVehiclesComponent.PageSize)));

  /** Solo tiene sentido rotar cuando hay más de una página. */
  readonly hasPages = computed(() => this.pageCount() > 1);

  /** Índices de página, para los puntos de paginación. */
  readonly pageIndexes = computed(() => Array.from({ length: this.pageCount() }, (_, i) => i));

  /** Los seis anuncios visibles de la página actual. */
  readonly pageVehicles = computed(() => {
    const start = this.page() * FeaturedVehiclesComponent.PageSize;
    return this.vehicles().slice(start, start + FeaturedVehiclesComponent.PageSize);
  });

  ngOnInit(): void {
    this.vehicleService.getFeaturedVehicles(24).subscribe({
      next: list => { this.vehicles.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  prev(): void {
    this.page.update(p => (p - 1 + this.pageCount()) % this.pageCount());
  }

  next(): void {
    this.page.update(p => (p + 1) % this.pageCount());
  }

  goTo(index: number): void {
    this.page.set(index);
  }
}
