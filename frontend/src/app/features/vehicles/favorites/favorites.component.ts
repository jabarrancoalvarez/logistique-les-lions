import { Component, ChangeDetectionStrategy, OnInit, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  VehicleService, FavoriteItem, STATUS_LABELS,
  PRICE_INDICATOR_LABELS, PRICE_INDICATOR_CLASSES
} from '@core/services/vehicle.service';
import { ComparatorService } from '@core/services/comparator.service';
import { ShareService } from '@core/services/share.service';
import { MessagingService } from '@core/services/messaging.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

@Component({
  selector: 'lll-favorites',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, FcfaPipe],
  templateUrl: './favorites.component.html'
})
export class FavoritesComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly comparator = inject(ComparatorService);
  private readonly share = inject(ShareService);
  private readonly messaging = inject(MessagingService);
  private readonly router = inject(Router);
  private readonly fcfa = new FcfaPipe();

  readonly items = signal<FavoriteItem[]>([]);
  readonly alertsAllEnabled = signal(true);
  readonly loading = signal(true);
  readonly hasError = signal(false);
  readonly copiedId = signal<string | null>(null);
  readonly tooManySelected = signal(false);

  readonly maxCompared = this.comparator.max;

  /** Selección para «Comparer la sélection». */
  private readonly selected = signal<Set<string>>(new Set());
  readonly selectedCount = computed(() => this.selected().size);

  isSelected(vehicleId: string): boolean {
    return this.selected().has(vehicleId);
  }

  toggleSelected(vehicleId: string): void {
    this.selected.update(current => {
      const next = new Set(current);
      next.has(vehicleId) ? next.delete(vehicleId) : next.add(vehicleId);
      return next;
    });
    // El aviso solo se muestra mientras haya exceso de selección.
    this.tooManySelected.set(false);
  }

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.vehicleService.getMyFavorites().subscribe({
      next: favorites => {
        this.items.set(favorites.items);
        this.alertsAllEnabled.set(favorites.alertsAllEnabled);
        this.loading.set(false);
      },
      error: () => {
        this.hasError.set(true);
        this.loading.set(false);
      }
    });
  }

  // ─── Alertas ───────────────────────────────────────────────────────────
  toggleAllAlerts(): void {
    const next = !this.alertsAllEnabled();
    const previous = this.alertsAllEnabled();
    this.alertsAllEnabled.set(next);

    this.vehicleService.setAllFavoriteAlerts(next).subscribe({
      // Al reactivar el interruptor general el backend restablece las alertas
      // individuales, así que hay que releer para reflejarlo.
      next: () => { if (next) this.load(); },
      error: () => this.alertsAllEnabled.set(previous)
    });
  }

  toggleAlert(item: FavoriteItem): void {
    const next = !item.alertEnabled;
    this.patchItem(item.vehicle.id, { alertEnabled: next });

    this.vehicleService.setFavoriteAlert(item.vehicle.id, next).subscribe({
      error: () => this.patchItem(item.vehicle.id, { alertEnabled: !next })
    });
  }

  private patchItem(vehicleId: string, changes: Partial<FavoriteItem>): void {
    this.items.update(list =>
      list.map(i => (i.vehicle.id === vehicleId ? { ...i, ...changes } : i)));
  }

  // ─── Acciones sobre un favorito ────────────────────────────────────────
  remove(vehicleId: string): void {
    const previous = this.items();
    this.items.update(list => list.filter(i => i.vehicle.id !== vehicleId));

    this.vehicleService.toggleFavorite(vehicleId).subscribe({
      error: () => this.items.set(previous)
    });
  }

  toggleCompare(vehicleId: string): void {
    if (this.comparator.toggle(vehicleId) === 'full') {
      this.tooManySelected.set(true);
      setTimeout(() => this.tooManySelected.set(false), 3000);
    }
  }

  isCompared(vehicleId: string): boolean {
    return this.comparator.has(vehicleId);
  }

  /**
   * «Comparer la sélection». Si se han marcado más de tres, se avisa en lugar de
   * quedarse con los tres primeros en silencio.
   */
  compareSelection(): void {
    if (this.selectedCount() === 0) return;

    if (this.selectedCount() > this.comparator.max()) {
      this.tooManySelected.set(true);
      return;
    }

    this.comparator.clear();
    for (const id of this.selected()) this.comparator.toggle(id);
    this.selected.set(new Set());
  }

  async shareItem(item: FavoriteItem): Promise<void> {
    const target = {
      title: `${item.vehicle.makeName} ${item.vehicle.modelName ?? ''}`.trim(),
      url: this.share.vehicleUrl(item.vehicle.slug),
      price: this.fcfa.transform(item.vehicle.price)
    };
    if (await this.share.copyLink(target)) {
      this.copiedId.set(item.vehicle.id);
      setTimeout(() => this.copiedId.set(null), 2000);
    }
  }

  contact(item: FavoriteItem): void {
    const v = item.vehicle;
    if (!v.sellerId) return;

    this.messaging.sendMessageRest(
      v.sellerId,
      v.id,
      `Bonjour, je suis intéressé par votre ${v.makeName} ${v.modelName ?? ''} ` +
      `(Réf. ${v.publicReference}).`
    ).subscribe({
      next: () => this.router.navigate(['/mensajes']),
      error: () => this.router.navigate(['/mensajes'])
    });
  }

  // ─── Etiquetas ─────────────────────────────────────────────────────────
  /** Un favorito vendido o reservado se sigue mostrando, con su estado. */
  statusNotice(item: FavoriteItem): string | null {
    const status = item.vehicle.status;
    return status === 'Vendu' || status === 'Reserve' ? STATUS_LABELS[status] : null;
  }

  isSold(item: FavoriteItem): boolean {
    return item.vehicle.status === 'Vendu';
  }

  priceIndicatorLabel(item: FavoriteItem): string | null {
    const i = item.vehicle.priceIndicator;
    return i ? PRICE_INDICATOR_LABELS[i] : null;
  }

  priceIndicatorClass(item: FavoriteItem): string {
    const i = item.vehicle.priceIndicator;
    return i ? PRICE_INDICATOR_CLASSES[i] : '';
  }
}
