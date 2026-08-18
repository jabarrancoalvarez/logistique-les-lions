import { Component, Input, ChangeDetectionStrategy, signal, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  FeaturedVehicle, VehicleListItem, VehicleService,
  FUEL_LABELS, STATUS_LABELS, FEATURED_LABELS,
  PRICE_INDICATOR_LABELS, PRICE_INDICATOR_CLASSES
} from '../../../core/services/vehicle.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ComparatorService } from '../../../core/services/comparator.service';
import { ShareService } from '../../../core/services/share.service';
import { FcfaPipe } from '../../pipes/fcfa.pipe';

export type VehicleCardData = FeaturedVehicle | VehicleListItem;

@Component({
  selector: 'lll-vehicle-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule, FcfaPipe],
  templateUrl: './vehicle-card.component.html'
})
export class VehicleCardComponent {
  @Input({ required: true }) vehicle!: VehicleCardData;

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly vehicleService = inject(VehicleService);
  private readonly comparator = inject(ComparatorService);
  private readonly share = inject(ShareService);
  private readonly fcfa = new FcfaPipe();

  readonly isFavorited = signal(false);
  readonly imageError = signal(false);
  readonly activeImage = signal(0);
  readonly shareOpen = signal(false);
  readonly copied = signal(false);
  /** Aviso al intentar añadir un cuarto vehículo al comparador. */
  readonly comparatorFull = signal(false);

  // ─── Galería deslizable ────────────────────────────────────────────────
  /**
   * Fotos de la tarjeta. `FeaturedVehicle` solo trae una imagen, así que se
   * normalizan ambos casos a una lista.
   */
  get images(): string[] {
    const list = (this.vehicle as VehicleListItem).images;
    if (list?.length) return list;
    const single = this.vehicle.thumbnailUrl ?? this.vehicle.primaryImageUrl;
    return single ? [single] : [];
  }

  get hasGallery(): boolean {
    return this.images.length > 1;
  }

  get currentImage(): string | null {
    return this.images[this.activeImage()] ?? null;
  }

  nextImage(event: Event): void {
    this.stop(event);
    this.activeImage.update(i => (i + 1) % this.images.length);
  }

  prevImage(event: Event): void {
    this.stop(event);
    this.activeImage.update(i => (i - 1 + this.images.length) % this.images.length);
  }

  goToImage(index: number, event: Event): void {
    this.stop(event);
    this.activeImage.set(index);
  }

  /** Desplazamiento horizontal con el dedo. */
  private touchStartX = 0;

  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.changedTouches[0].screenX;
  }

  onTouchEnd(event: TouchEvent): void {
    if (!this.hasGallery) return;
    const delta = event.changedTouches[0].screenX - this.touchStartX;
    // Umbral para no confundir un deslizamiento con un toque.
    if (Math.abs(delta) < 40) return;
    delta < 0 ? this.nextImage(event) : this.prevImage(event);
  }

  onImageError(): void {
    this.imageError.set(true);
  }

  // ─── Datos mostrados ───────────────────────────────────────────────────
  get city(): string | null {
    return (this.vehicle as VehicleListItem).city ?? null;
  }

  get version(): string | null {
    return (this.vehicle as VehicleListItem).version ?? null;
  }

  /** "Toyota RAV4 2.0 VVT-i" */
  get modelLine(): string {
    const v = this.vehicle;
    return [v.makeName, v.modelName, this.version].filter(Boolean).join(' ');
  }

  /**
   * Distintivo «Sponsorisé» de la tarjeta. Los ítems de la portada son siempre «À la
   * une»; los del listado traen su nivel vigente en `featuredTier`.
   */
  get featuredBadge(): string | null {
    if ('featuredTier' in this.vehicle) {
      const t = (this.vehicle as VehicleListItem).featuredTier;
      return t === 'ALaUne' ? FEATURED_LABELS.ALaUne
           : t === 'EnVedette' ? FEATURED_LABELS.EnVedette
           : null;
    }
    return FEATURED_LABELS.ALaUne;
  }

  /** Se avisa cuando el anuncio ya no está simplemente disponible. */
  get statusNotice(): string | null {
    const status = (this.vehicle as VehicleListItem).status;
    if (status === 'Reserve' || status === 'Vendu') return STATUS_LABELS[status];
    return null;
  }

  get fuelLabel(): string {
    return this.vehicle.fuelType ? (FUEL_LABELS[this.vehicle.fuelType] ?? '') : '';
  }

  /**
   * Indicador de precio. Devuelve `null` cuando no hay comparables suficientes: en ese
   * caso no se muestra nada, nunca un valor inventado.
   */
  get priceIndicatorLabel(): string | null {
    const indicator = (this.vehicle as VehicleListItem).priceIndicator;
    return indicator ? PRICE_INDICATOR_LABELS[indicator] : null;
  }

  get priceIndicatorClass(): string {
    const indicator = (this.vehicle as VehicleListItem).priceIndicator;
    return indicator ? PRICE_INDICATOR_CLASSES[indicator] : '';
  }

  get formattedMileage(): string {
    if (this.vehicle.mileage === null || this.vehicle.mileage === undefined) return '';
    return `${this.vehicle.mileage.toLocaleString('fr-FR')} km`;
  }

  get daysAgo(): string {
    const diff = Date.now() - new Date(this.vehicle.createdAt).getTime();
    const days = Math.floor(diff / 86400000);
    if (days === 0) return "Aujourd'hui";
    if (days === 1) return 'Hier';
    return `Il y a ${days} jours`;
  }

  // ─── Acciones ──────────────────────────────────────────────────────────
  readonly isCompared = computed(() => this.comparator.has(this.vehicle?.id ?? ''));

  /**
   * Favoritos y comparador exigen cuenta: un visitante que los pulse va al registro,
   * conservando el anuncio como destino de vuelta.
   */
  private requireAccount(): boolean {
    if (this.auth.isAuthenticated()) return true;
    this.router.navigate(['/auth/register'], {
      queryParams: { returnUrl: `/vehiculos/${this.vehicle.slug}` }
    });
    return false;
  }

  toggleFavorite(event: Event): void {
    this.stop(event);
    if (!this.requireAccount()) return;

    const previous = this.isFavorited();
    // Respuesta inmediata; si el servidor rechaza, se revierte.
    this.isFavorited.set(!previous);

    this.vehicleService.toggleFavorite(this.vehicle.id).subscribe({
      next: r => this.isFavorited.set(r.isSaved),
      error: () => this.isFavorited.set(previous)
    });
  }

  toggleCompare(event: Event): void {
    this.stop(event);
    if (!this.requireAccount()) return;

    if (this.comparator.toggle(this.vehicle.id) === 'full') {
      this.comparatorFull.set(true);
      setTimeout(() => this.comparatorFull.set(false), 3000);
    }
  }

  /** Compartir no requiere registro. */
  toggleShare(event: Event): void {
    this.stop(event);
    this.shareOpen.update(v => !v);
  }

  private get shareTarget() {
    return {
      title: this.vehicle.title,
      url: this.share.vehicleUrl(this.vehicle.slug),
      price: this.fcfa.transform(this.vehicle.price)
    };
  }

  shareWhatsApp(event: Event): void {
    this.stop(event);
    this.share.whatsapp(this.shareTarget);
    this.shareOpen.set(false);
  }

  shareEmail(event: Event): void {
    this.stop(event);
    this.share.email(this.shareTarget);
    this.shareOpen.set(false);
  }

  shareNative(event: Event): void {
    this.stop(event);
    void this.share.native(this.shareTarget);
    this.shareOpen.set(false);
  }

  get supportsNativeShare(): boolean {
    return this.share.supportsNativeShare;
  }

  async copyLink(event: Event): Promise<void> {
    this.stop(event);
    if (await this.share.copyLink(this.shareTarget)) {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    }
    this.shareOpen.set(false);
  }

  /** La tarjeta entera es un enlace: los botones no deben navegar. */
  private stop(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
  }
}
