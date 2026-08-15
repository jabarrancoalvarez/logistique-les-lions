import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PlatformService, UpcomingFeature } from '@core/services/platform.service';
import { AuthService } from '@core/auth/auth.service';

/**
 * «Prochainement»: lo que viene, y quién lo quiere.
 *
 * No es un escaparate: cada «Ça m'intéresse» es un voto que decide qué se desarrolla
 * antes. Por eso hace falta cuenta para pulsarlo — un interés sin persona detrás no se
 * puede contar ni segmentar.
 */
@Component({
  selector: 'lll-upcoming',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  templateUrl: './upcoming.component.html'
})
export class UpcomingComponent implements OnInit {
  private readonly platform = inject(PlatformService);
  private readonly auth = inject(AuthService);

  readonly features = signal<UpcomingFeature[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly pendingId = signal<string | null>(null);

  readonly isAuthenticated = this.auth.isAuthenticated;

  ngOnInit(): void {
    this.platform.getUpcoming().subscribe({
      next: r => { this.features.set(r.items); this.loading.set(false); },
      error: () => {
        this.error.set('Impossible de charger les nouveautés.');
        this.loading.set(false);
      }
    });
  }

  toggle(feature: UpcomingFeature): void {
    if (this.pendingId()) return;

    const interested = !feature.isInterested;
    this.pendingId.set(feature.id);

    this.platform.setInterest(feature.id, interested).subscribe({
      next: r => {
        this.features.update(list => list.map(f =>
          f.id === feature.id
            ? { ...f, isInterested: interested, interestedCount: r.interestedCount }
            : f));
        this.pendingId.set(null);
      },
      error: () => {
        this.error.set('Impossible d\'enregistrer votre choix.');
        this.pendingId.set(null);
      }
    });
  }
}
