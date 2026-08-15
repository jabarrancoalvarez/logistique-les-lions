import { Component, ChangeDetectionStrategy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { VehicleService } from '@core/services/vehicle.service';
import { ListingService } from '@core/services/listing.service';
import { GarageService } from '@core/services/garage.service';
import { NegotiationService } from '@core/services/negotiation.service';

/**
 * Pantalla a la que se llega al identificarse: la puerta a los cuatro espacios.
 *
 * No es una sección más del menú, sino un resumen de lo que el usuario tiene en marcha
 * en cada uno de ellos: lo que quiere comprar, lo que negocia, lo que tiene y lo que
 * está vendiendo.
 */
@Component({
  selector: 'lll-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly vehicles = inject(VehicleService);
  private readonly listings = inject(ListingService);
  private readonly garage = inject(GarageService);
  private readonly negotiations = inject(NegotiationService);

  readonly user = computed(() => this.auth.user());
  readonly displayName = computed(() => {
    const u = this.user();
    if (!u) return '';
    return u.displayName || u.phone || u.email || '';
  });

  readonly favoritesCount = signal<number | null>(null);
  readonly negotiationsCount = signal<number | null>(null);
  readonly garageCount = signal<number | null>(null);
  readonly activeListingsCount = signal<number | null>(null);

  /** Lo que reclama atención: rappels vencidos y negociaciones esperando respuesta. */
  readonly openReminders = signal(0);
  readonly waitingNegotiations = signal(0);

  ngOnInit(): void {
    // Cada espacio se consulta por separado: si uno falla, los demás siguen mostrándose.
    this.vehicles.getMyFavorites().subscribe({
      next: f => this.favoritesCount.set(f.items.length),
      error: () => this.favoritesCount.set(null)
    });

    this.negotiations.getAll().subscribe({
      next: list => {
        this.negotiationsCount.set(list.filter(n => n.status !== 'Terminee').length);
        this.waitingNegotiations.set(list.filter(n => n.status === 'EnAttente').length);
      },
      error: () => this.negotiationsCount.set(null)
    });

    this.garage.getMyGarage().subscribe({
      next: g => {
        this.garageCount.set(g.vehicleCount);
        this.openReminders.set(g.openReminderCount);
      },
      error: () => this.garageCount.set(null)
    });

    this.listings.getMyListings().subscribe({
      next: l => this.activeListingsCount.set(l.countByStatus.Actif ?? 0),
      error: () => this.activeListingsCount.set(null)
    });
  }
}
