import { Component, OnInit, ChangeDetectionStrategy, inject, signal } from '@angular/core';
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

  /**
   * Vacío al arrancar. ❌ Nunca con datos de ejemplo: antes se sembraba con coches
   * europeos de muestra (BMW, Mercedes… en euros), que se veían mientras la API
   * respondía —sobre todo tras el arranque en frío de Render— y contradecían el
   * catálogo real de Senegal. Ahora se muestra un esqueleto de carga hasta que llegan
   * los anuncios de verdad.
   */
  readonly vehicles = signal<FeaturedVehicle[]>([]);
  readonly loading = signal(true);

  /** Marcadores de posición para el esqueleto mientras carga. */
  readonly skeletons = Array.from({ length: 6 });

  ngOnInit(): void {
    this.vehicleService.getFeaturedVehicles(6).subscribe({
      next: list => { this.vehicles.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }
}
