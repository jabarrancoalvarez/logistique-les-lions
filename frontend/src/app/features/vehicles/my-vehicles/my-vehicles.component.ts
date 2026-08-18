import { Component, ChangeDetectionStrategy, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  ListingService, MyListings, MyListing, ListingQuality, ListingQualityItem,
  ListingQualityCheck
} from '@core/services/listing.service';
import { VehicleStatus, STATUS_LABELS } from '@core/services/vehicle.service';
import { ShareService } from '@core/services/share.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/** Pestañas de «Mes annonces», en el orden del documento. */
const TABS: readonly VehicleStatus[] =
  ['Brouillon', 'Actif', 'EnPause', 'Reserve', 'Vendu', 'Archive'];

/**
 * «Mes annonces» — los vehículos que el usuario está vendiendo o ha vendido.
 */
@Component({
  selector: 'lll-my-vehicles',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, FcfaPipe],
  templateUrl: './my-vehicles.component.html'
})
export class MyVehiclesComponent implements OnInit {
  private readonly service = inject(ListingService);
  private readonly share = inject(ShareService);
  private readonly router = inject(Router);

  readonly data = signal<MyListings | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  /** `null` = todas. */
  readonly activeTab = signal<VehicleStatus | null>(null);
  readonly tabs = TABS;

  readonly listings = computed(() => this.data()?.listings ?? []);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.service.getMyListings(this.activeTab() ?? undefined).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); this.busy.set(false); },
      error: () => {
        this.error.set('Impossible de charger vos annonces.');
        this.loading.set(false);
        this.busy.set(false);
      }
    });
  }

  selectTab(tab: VehicleStatus | null): void {
    this.activeTab.set(tab);
    this.closeAll();
    this.load();
  }

  countFor(status: VehicleStatus): number {
    return this.data()?.countByStatus?.[status] ?? 0;
  }

  totalCount(): number {
    const counts = this.data()?.countByStatus ?? {};
    return Object.values(counts).reduce((sum, n) => sum + (n ?? 0), 0);
  }

  statusLabel(status: VehicleStatus): string {
    return STATUS_LABELS[status];
  }

  statusClass(status: VehicleStatus): string {
    const map: Record<VehicleStatus, string> = {
      Brouillon: 'bg-navy/10 text-navy/70',
      Actif:     'bg-green-100 text-green-800',
      EnPause:   'bg-amber-100 text-amber-800',
      Reserve:   'bg-orange-100 text-orange-800',
      Vendu:     'bg-blue-100 text-blue-800',
      Archive:   'bg-navy/10 text-navy/50'
    };
    return map[status];
  }

  // ─── Acciones de estado ──────────────────────────────────────────────────
  /** Transiciones que el backend admite desde cada estado. */
  actionsFor(listing: MyListing): { status: VehicleStatus; label: string }[] {
    const map: Record<VehicleStatus, { status: VehicleStatus; label: string }[]> = {
      Brouillon: [{ status: 'Actif', label: "Publier l'annonce" },
                  { status: 'Archive', label: 'Archiver' }],
      Actif:     [{ status: 'EnPause', label: 'Mettre en pause' },
                  { status: 'Reserve', label: 'Marquer réservé' },
                  { status: 'Vendu', label: 'Marquer vendu' },
                  { status: 'Archive', label: 'Archiver' }],
      EnPause:   [{ status: 'Actif', label: 'Réactiver' },
                  { status: 'Vendu', label: 'Marquer vendu' },
                  { status: 'Archive', label: 'Archiver' }],
      Reserve:   [{ status: 'Actif', label: 'Remettre en vente' },
                  { status: 'Vendu', label: 'Marquer vendu' },
                  { status: 'Archive', label: 'Archiver' }],
      Vendu:     [{ status: 'Archive', label: 'Archiver' }],
      Archive:   [{ status: 'Brouillon', label: 'Remettre en brouillon' }]
    };
    return map[listing.status];
  }

  changeStatus(listing: MyListing, status: VehicleStatus): void {
    if (this.busy()) return;

    this.busy.set(true);
    this.error.set(null);

    this.service.changeStatus(listing.id, status).subscribe({
      next: () => this.load(),
      error: () => {
        this.busy.set(false);
        this.error.set(status === 'Actif'
          ? "Publication impossible. Vérifiez que l'annonce a un prix."
          : 'Action impossible.');
      }
    });
  }

  duplicate(listing: MyListing): void {
    if (this.busy()) return;

    this.busy.set(true);
    this.service.duplicate(listing.id).subscribe({
      next: () => { this.selectTab('Brouillon'); },
      error: () => { this.busy.set(false); this.error.set('Duplication impossible.'); }
    });
  }

  readonly shareCopied = signal<string | null>(null);

  /** Compartir copia el enlace; si el dispositivo tiene menú propio, lo usa. */
  async shareListing(listing: MyListing): Promise<void> {
    const target = { title: listing.title, url: this.share.vehicleUrl(listing.slug) };

    if (this.share.supportsNativeShare && await this.share.native(target)) return;

    if (await this.share.copyLink(target)) {
      this.shareCopied.set(listing.id);
      setTimeout(() => this.shareCopied.set(null), 2500);
    }
  }

  goToNegotiations(): void {
    void this.router.navigate(['/mis-negociaciones']);
  }

  // ─── Prix et kilométrage ─────────────────────────────────────────────────
  /** Identificador del anuncio cuyo formulario rápido está abierto. */
  readonly editingPrice = signal<string | null>(null);
  readonly editingMileage = signal<string | null>(null);
  priceValue = 0;
  mileageValue = 0;

  openPrice(listing: MyListing): void {
    this.closeAll();
    this.priceValue = listing.price;
    this.editingPrice.set(listing.id);
  }

  openMileage(listing: MyListing): void {
    this.closeAll();
    this.mileageValue = listing.mileage ?? 0;
    this.editingMileage.set(listing.id);
  }

  savePrice(listing: MyListing): void {
    if (this.busy() || !(this.priceValue > 0)) return;

    this.busy.set(true);
    this.service.updatePrice(listing.id, this.priceValue).subscribe({
      next: () => { this.editingPrice.set(null); this.load(); },
      error: () => { this.busy.set(false); this.error.set('Prix non enregistré.'); }
    });
  }

  saveMileage(listing: MyListing): void {
    if (this.busy()) return;

    this.busy.set(true);
    this.service.updateMileage(listing.id, this.mileageValue).subscribe({
      next: () => { this.editingMileage.set(null); this.load(); },
      error: () => {
        this.busy.set(false);
        this.error.set('Le kilométrage ne peut pas diminuer.');
      }
    });
  }

  // ─── Qualité de l'annonce ────────────────────────────────────────────────
  readonly qualityFor = signal<string | null>(null);
  readonly quality = signal<ListingQuality | null>(null);

  toggleQuality(listing: MyListing): void {
    if (this.qualityFor() === listing.id) {
      this.qualityFor.set(null);
      return;
    }

    this.closeAll();
    this.qualityFor.set(listing.id);
    this.quality.set(null);

    this.service.getQuality(listing.id).subscribe({
      next: q => this.quality.set(q),
      error: () => this.quality.set(null)
    });
  }

  /** Texto de cada línea del desglose de calidad. */
  qualityText(item: ListingQualityItem): string {
    const n = item.detail ?? 0;

    const texts: Record<ListingQualityCheck, string> = {
      Photos: item.status === 'Complete'
        ? `${n} photos`
        : n > 0 ? `${n} photo${n > 1 ? 's' : ''} — ajoutez-en davantage` : 'Aucune photo',
      Description: item.status === 'Complete'
        ? 'Description détaillée'
        : n > 0 ? 'Description trop courte' : 'Aucune description',
      Price: item.status === 'Complete' ? 'Prix renseigné' : 'Prix à renseigner',
      Mileage: item.status === 'Complete' ? 'Kilométrage renseigné' : 'Kilométrage à renseigner',
      Location: item.status === 'Complete'
        ? 'Localisation complète' : 'Localisation à compléter',
      Specifications: item.status === 'Complete'
        ? 'Fiche technique complète' : 'Fiche technique à compléter',
      // Como en «Photos»: cuando falta, se dice qué falta. Un recuento a secas junto a
      // un ⚠ se lee como si tener tres equipamientos fuera el problema.
      Equipment: item.status === 'Complete'
        ? `${n} équipements`
        : n > 0
          ? `${n} équipement${n > 1 ? 's' : ''} — cochez-en davantage`
          : 'Aucun équipement'
    };

    return texts[item.check];
  }

  qualityIcon(item: ListingQualityItem): string {
    return item.status === 'Complete' ? '✓' : '⚠';
  }

  private closeAll(): void {
    this.editingPrice.set(null);
    this.editingMileage.set(null);
    this.qualityFor.set(null);
  }
}
