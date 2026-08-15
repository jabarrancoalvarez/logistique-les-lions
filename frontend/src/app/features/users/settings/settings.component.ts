import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService, ProfileData } from '@core/auth/auth.service';
import { VehicleService } from '@core/services/vehicle.service';

/**
 * «Paramètres» — deliberadamente pequeño.
 *
 * Las preferencias que pertenecen a un módulo se quedan en su módulo: la alerta de un
 * favorito se gestiona en Favoris, y la de una búsqueda guardada en Recherches
 * enregistrées. Aquí solo está lo transversal.
 */
@Component({
  selector: 'lll-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  templateUrl: './settings.component.html'
})
export class SettingsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly vehicles = inject(VehicleService);

  readonly profile = signal<ProfileData | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  /** Interruptor general de las alertas de Favoris. */
  readonly favoriteAlerts = signal<boolean | null>(null);

  ngOnInit(): void {
    this.auth.getProfile().subscribe({
      next: p => { this.profile.set(p); this.loading.set(false); },
      error: () => { this.error.set('Impossible de charger vos paramètres.'); this.loading.set(false); }
    });

    this.vehicles.getMyFavorites().subscribe({
      next: f => this.favoriteAlerts.set(f.alertsAllEnabled),
      error: () => this.favoriteAlerts.set(null)
    });
  }

  /** «Autoriser le contact par WhatsApp». La messagerie interne sigue siendo el canal principal. */
  toggleWhatsApp(): void {
    const p = this.profile();
    if (!p || this.saving()) return;

    const next = !p.allowWhatsAppContact;
    this.saving.set(true);

    this.auth.updateProfile({ allowWhatsAppContact: next }).subscribe({
      next: () => {
        this.profile.set({ ...p, allowWhatsAppContact: next });
        this.saving.set(false);
      },
      error: () => { this.saving.set(false); this.error.set('Modification impossible.'); }
    });
  }

  toggleFavoriteAlerts(): void {
    const current = this.favoriteAlerts();
    if (current === null || this.saving()) return;

    this.saving.set(true);
    this.vehicles.setAllFavoriteAlerts(!current).subscribe({
      next: () => { this.favoriteAlerts.set(!current); this.saving.set(false); },
      error: () => { this.saving.set(false); this.error.set('Modification impossible.'); }
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
