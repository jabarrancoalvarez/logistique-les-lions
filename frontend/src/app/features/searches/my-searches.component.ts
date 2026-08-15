import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VehicleService } from '@core/services/vehicle.service';
import { SavedSearchService } from '@core/services/saved-search.service';
import { VehicleRequestService } from '@core/services/vehicle-request.service';
import { ComparatorService } from '@core/services/comparator.service';

/**
 * «Mes recherches» — el centro de todo lo relacionado con encontrar un vehículo.
 *
 * Reúne las cuatro secciones que la especificación agrupa aquí:
 * Favoris · Recherches enregistrées · Comparateur · Mes demandes.
 * Es la prolongación personal de la Etapa 1.
 */
@Component({
  selector: 'lll-my-searches',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-searches.component.html'
})
export class MySearchesComponent implements OnInit {
  private readonly vehicles = inject(VehicleService);
  private readonly savedSearches = inject(SavedSearchService);
  private readonly requests = inject(VehicleRequestService);
  readonly comparator = inject(ComparatorService);

  readonly favoritesCount = signal<number | null>(null);
  readonly savedSearchesCount = signal<number | null>(null);
  readonly requestsCount = signal<number | null>(null);

  /** Búsquedas guardadas con alerta activa: lo que de verdad avisa al usuario. */
  readonly activeAlerts = signal(0);
  /** Propuestas sin ver en las solicitudes «Trouvez-moi une voiture». */
  readonly newProposals = signal(0);

  ngOnInit(): void {
    // Cada contador es independiente: si uno falla, las demás tarjetas siguen bien.
    this.vehicles.getMyFavorites().subscribe({
      next: f => this.favoritesCount.set(f.items.length),
      error: () => this.favoritesCount.set(null)
    });

    this.savedSearches.getAll().subscribe({
      next: list => {
        this.savedSearchesCount.set(list.length);
        this.activeAlerts.set(list.filter(s => s.alertEnabled).length);
      },
      error: () => this.savedSearchesCount.set(null)
    });

    this.requests.getAll().subscribe({
      next: list => {
        this.requestsCount.set(list.length);
        this.newProposals.set(list.reduce((sum, r) => sum + r.unseenProposals, 0));
      },
      error: () => this.requestsCount.set(null)
    });
  }
}
