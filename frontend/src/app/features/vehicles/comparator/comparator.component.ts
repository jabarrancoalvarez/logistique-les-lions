import { Component, ChangeDetectionStrategy, OnInit, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  VehicleService, VehicleComparison, VehicleListItem, FilterOptions,
  FUEL_LABELS, TRANSMISSION_LABELS, BODY_LABELS, CUSTOMS_LABELS, DRIVETRAIN_LABELS,
  PRICE_INDICATOR_LABELS, PRICE_INDICATOR_CLASSES, STATUS_LABELS
} from '@core/services/vehicle.service';
import { ComparatorService } from '@core/services/comparator.service';
import { ShareService } from '@core/services/share.service';
import { MessagingService } from '@core/services/messaging.service';
import { AuthService } from '@core/auth/auth.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/** Una fila de la tabla comparativa. */
export interface ComparisonRow {
  label: string;
  /** Un valor por vehículo, en el mismo orden. `null` = Non renseigné. */
  values: (string | null)[];
  /** Los vehículos difieren en esta característica. */
  differs: boolean;
}

export interface ComparisonBlock {
  title: string;
  rows: ComparisonRow[];
}

@Component({
  selector: 'lll-comparator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, FcfaPipe],
  templateUrl: './comparator.component.html'
})
export class ComparatorComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  readonly comparator = inject(ComparatorService);
  private readonly share = inject(ShareService);
  private readonly messaging = inject(MessagingService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fcfa = new FcfaPipe();

  readonly vehicles = signal<VehicleComparison[]>([]);
  readonly loading = signal(true);
  readonly hasError = signal(false);
  readonly copiedId = signal<string | null>(null);

  readonly maxVehicles = this.comparator.max;
  readonly hasFreeSlot = computed(() => this.vehicles().length < this.comparator.max());

  /** Catálogo de equipamiento, para etiquetar los códigos. */
  private readonly equipmentCatalog = signal<FilterOptions['equipments']>([]);

  ngOnInit(): void {
    this.vehicleService.getFilterOptions().subscribe({
      next: o => this.equipmentCatalog.set(o.equipments),
      error: () => this.equipmentCatalog.set([])
    });
    this.load();
  }

  /**
   * Se consultan siempre los datos actuales: si el precio cambió o el anuncio pasó a
   * reservado, la comparación lo refleja.
   */
  private load(): void {
    const ids = this.comparator.ids();
    if (ids.length === 0) {
      this.vehicles.set([]);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.vehicleService.compareVehicles(ids).subscribe({
      next: items => {
        this.vehicles.set(items);
        // Un anuncio retirado del todo desaparece de la selección guardada.
        const alive = new Set(items.map(i => i.id));
        for (const id of ids) if (!alive.has(id)) this.comparator.remove(id);
        this.loading.set(false);
      },
      error: () => {
        this.hasError.set(true);
        this.loading.set(false);
      }
    });
  }

  // ─── Tabla comparativa ─────────────────────────────────────────────────
  /**
   * Bloques de la especificación. Un campo sin declarar se muestra como
   * «Non renseigné»: nunca se infiere ni se inventa información.
   */
  readonly blocks = computed<ComparisonBlock[]>(() => {
    const list = this.vehicles();
    if (list.length === 0) return [];

    const row = (label: string, pick: (v: VehicleComparison) => string | null): ComparisonRow => {
      const values = list.map(pick);
      // Solo tiene sentido señalar diferencias si hay más de un vehículo.
      const differs = values.length > 1 && new Set(values.map(v => v ?? '—')).size > 1;
      return { label, values, differs };
    };

    const number = (n: number | null, suffix = ''): string | null =>
      n === null || n === undefined ? null : `${n.toLocaleString('fr-FR')}${suffix}`;

    return [
      {
        title: 'Caractéristiques principales',
        rows: [
          row('Année',             v => v.year.toString()),
          row('Kilométrage',       v => number(v.mileage, ' km')),
          row('Carburant',         v => v.fuelType ? FUEL_LABELS[v.fuelType] : null),
          row('Boîte de vitesses', v => v.transmission ? TRANSMISSION_LABELS[v.transmission] : null),
          row('Carrosserie',       v => v.bodyType ? BODY_LABELS[v.bodyType] : null),
          row('Puissance',         v => number(v.powerCv, ' CV')),
          row('Cylindrée',         v => number(v.engineDisplacementCc, ' cm³')),
          row('Transmission',      v => v.drivetrain ? DRIVETRAIN_LABELS[v.drivetrain] : null),
          row('Portes',            v => number(v.doors)),
          row('Places',            v => number(v.seats)),
          row('Couleur',           v => v.color)
        ]
      },
      {
        title: 'Situation administrative',
        rows: [
          row('Statut douanier', v => v.customsStatus ? CUSTOMS_LABELS[v.customsStatus] : null)
        ]
      },
      {
        title: 'Équipements',
        rows: this.equipmentCatalog()
          // Solo se listan los equipamientos que al menos un vehículo declara: mostrar
          // el catálogo entero llenaría la tabla de filas vacías.
          .filter(e => list.some(v => v.equipmentCodes.includes(e.code)))
          .map(e => row(e.name, v => (v.equipmentCodes.includes(e.code) ? '✓' : null)))
      }
    ].filter(block => block.rows.length > 0);
  });

  // ─── Cabecera de cada columna ──────────────────────────────────────────
  headline(v: VehicleComparison): string {
    return [v.makeName, v.modelName, v.version].filter(Boolean).join(' ');
  }

  statusLabel(v: VehicleComparison): string {
    return STATUS_LABELS[v.status];
  }

  /** Un vehículo vendido conserva su ficha pero no admite oferta ni contacto. */
  isSold(v: VehicleComparison): boolean {
    return v.status === 'Vendu';
  }

  priceIndicatorLabel(v: VehicleComparison): string | null {
    return v.priceIndicator ? PRICE_INDICATOR_LABELS[v.priceIndicator] : null;
  }

  priceIndicatorClass(v: VehicleComparison): string {
    return v.priceIndicator ? PRICE_INDICATOR_CLASSES[v.priceIndicator] : '';
  }

  // ─── Acciones ──────────────────────────────────────────────────────────
  removeFromComparator(vehicleId: string): void {
    this.comparator.remove(vehicleId);
    this.vehicles.update(list => list.filter(v => v.id !== vehicleId));
  }

  clearAll(): void {
    this.comparator.clear();
    this.vehicles.set([]);
  }

  toggleFavorite(v: VehicleComparison): void {
    if (!this.requireAccount()) return;
    this.vehicleService.toggleFavorite(v.id).subscribe({ error: () => {} });
  }

  async shareVehicle(v: VehicleComparison): Promise<void> {
    const ok = await this.share.copyLink({
      title: this.headline(v),
      url: this.share.vehicleUrl(v.slug),
      price: this.fcfa.transform(v.price)
    });
    if (ok) {
      this.copiedId.set(v.id);
      setTimeout(() => this.copiedId.set(null), 2000);
    }
  }

  contact(v: VehicleComparison): void {
    if (this.isSold(v) || !this.requireAccount()) return;

    this.messaging.sendMessageRest(
      v.sellerId, v.id,
      `Bonjour, je suis intéressé par votre ${this.headline(v)} (Réf. ${v.publicReference}).`
    ).subscribe({
      next: () => this.router.navigate(['/mensajes']),
      error: () => this.router.navigate(['/mensajes'])
    });
  }

  private requireAccount(): boolean {
    if (this.auth.isAuthenticated()) return true;
    this.router.navigate(['/auth/register'], { queryParams: { returnUrl: '/comparateur' } });
    return false;
  }

  // ─── Sustituir un vehículo sin salir de la comparación ─────────────────
  readonly searchOpen = signal(false);
  readonly searchResults = signal<VehicleListItem[]>([]);
  readonly searching = signal(false);
  searchTerm = '';

  private readonly search$ = new Subject<string>();

  constructor() {
    this.search$
      .pipe(
        debounceTime(300),
        switchMap(term => {
          this.searching.set(true);
          return this.vehicleService.getVehicles({ search: term, pageSize: 6 });
        }),
        takeUntilDestroyed()
      )
      .subscribe({
        next: page => {
          // No se ofrecen los que ya están en la comparación.
          const current = new Set(this.comparator.ids());
          this.searchResults.set(page.items.filter(i => !current.has(i.id)));
          this.searching.set(false);
        },
        error: () => {
          this.searchResults.set([]);
          this.searching.set(false);
        }
      });
  }

  openSearch(): void {
    this.searchOpen.set(true);
    this.searchTerm = '';
    this.searchResults.set([]);
  }

  closeSearch(): void {
    this.searchOpen.set(false);
  }

  onSearchTermChange(): void {
    const term = this.searchTerm.trim();
    if (term.length < 2) {
      this.searchResults.set([]);
      return;
    }
    this.search$.next(term);
  }

  addToComparison(vehicleId: string): void {
    if (this.comparator.toggle(vehicleId) === 'added') {
      this.closeSearch();
      this.load();
    }
  }
}
