import {
  Component, ChangeDetectionStrategy, OnInit, signal, computed, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, Statistics, LabelCount } from '@core/services/admin.service';
import { FUEL_LABELS, CUSTOMS_LABELS } from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';
import { SENEGAL_REGIONS } from '@shared/data/senegal-geo';

/**
 * «Statistiques» del backoffice.
 *
 * Cuatro lecturas: quién entra, qué se publica, qué se busca y hasta dónde llega la
 * gente. El bloque que importa de verdad es el desajuste entre búsquedas y oferta.
 */
@Component({
  selector: 'lll-admin-statistics',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, FcfaPipe],
  templateUrl: './admin-statistics.component.html'
})
export class AdminStatisticsComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly stats = signal<Statistics | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly periods = [
    { days: 7,   label: '7 jours' },
    { days: 30,  label: '30 jours' },
    { days: 90,  label: '90 jours' },
    { days: 365, label: '12 mois' }
  ];

  days = 30;

  /**
   * Los pasos del embudo, en orden.
   *
   * `from` es el paso anterior, con el que se calcula el porcentaje que sobrevive;
   * el primero no tiene de qué venir.
   */
  readonly funnelSteps = computed(() => {
    const f = this.stats()?.funnel;
    if (!f) return [];

    return [
      { label: 'Vues d’annonces',  value: f.views,          from: null as number | null },
      { label: 'Favoris',          value: f.favorites,      from: f.views },
      { label: 'Conversations',    value: f.negotiations,   from: f.favorites },
      { label: 'Offres',           value: f.offers,         from: f.negotiations },
      { label: 'Accords',          value: f.acceptedOffers, from: f.offers },
      { label: 'Contrats',         value: f.contracts,      from: f.acceptedOffers },
      { label: 'Ventes vérifiées', value: f.verifiedSales,  from: f.contracts }
    ];
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.admin.getStatistics(this.days).subscribe({
      next: d => { this.stats.set(d); this.loading.set(false); },
      error: () => {
        this.error.set('Impossible de charger les statistiques.');
        this.loading.set(false);
      }
    });
  }

  changePeriod(days: number): void {
    this.days = days;
    this.load();
  }

  /** Anchura de la barra de un ranking, en relación con la fila más alta. */
  barWidth(rows: LabelCount[], count: number): string {
    const max = rows.reduce((m, r) => Math.max(m, r.count), 0);
    return max > 0 ? `${Math.round((count / max) * 100)}%` : '0%';
  }

  /** Cuánto se conserva de un paso al siguiente del embudo. */
  rate(from: number, to: number): string {
    if (from <= 0) return '—';
    return `${Math.round((to / from) * 1000) / 10} %`;
  }

  /**
   * Las agregaciones llegan con el nombre del enum —«NonDedouane»—, que es lo correcto:
   * el servidor devuelve un código estable e independiente del idioma. Traducirlo es
   * cosa de la pantalla.
   */
  fuelLabel(code: string): string {
    return (FUEL_LABELS as Record<string, string>)[code] ?? code;
  }

  customsLabel(code: string): string {
    return (CUSTOMS_LABELS as Record<string, string>)[code] ?? code;
  }

  regionName(code: string): string {
    return SENEGAL_REGIONS.find(r => r.code === code)?.name ?? code;
  }
}
