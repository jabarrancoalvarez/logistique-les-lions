import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit, HostListener
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NegotiationService } from '@core/services/negotiation.service';
import {
  VehicleService, VehicleDetail, VehicleListItem,
  FUEL_LABELS, TRANSMISSION_LABELS, BODY_LABELS, DRIVETRAIN_LABELS,
  PRICE_INDICATOR_LABELS, PRICE_INDICATOR_CLASSES, PublicTransparency
} from '@core/services/vehicle.service';
import { AuthService } from '@core/auth/auth.service';
import { MessagingService } from '@core/services/messaging.service';
import { ComparatorService } from '@core/services/comparator.service';
import { ShareService } from '@core/services/share.service';
import { VehicleCardComponent } from '@shared/components/vehicle-card/vehicle-card.component';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/** Una fila del bloque «Caractéristiques». */
interface Spec {
  label: string;
  value: string;
}

/**
 * Acciones rápidas de «Besoin de plus d'informations ?».
 * Abren el chat con un mensaje ya redactado.
 */
const INFO_REQUESTS: readonly { label: string; message: string }[] = [
  { label: 'Photos supplémentaires', message: 'Bonjour, pouvez-vous m\'envoyer des photos supplémentaires ?' },
  { label: 'Photo du moteur',        message: 'Bonjour, pouvez-vous m\'envoyer une photo du moteur ?' },
  { label: "Photo de l'intérieur",   message: 'Bonjour, pouvez-vous m\'envoyer une photo de l\'intérieur ?' },
  { label: 'Photo du VIN',           message: 'Bonjour, pouvez-vous m\'envoyer une photo du numéro VIN ?' },
  { label: 'Vidéo du véhicule',      message: 'Bonjour, pouvez-vous m\'envoyer une vidéo du véhicule ?' },
  { label: 'Vidéo au démarrage',     message: 'Bonjour, pouvez-vous m\'envoyer une vidéo du moteur au démarrage ?' }
];

@Component({
  selector: 'lll-vehicle-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, VehicleCardComponent, FcfaPipe],
  templateUrl: './vehicle-detail.component.html'
})
export class VehicleDetailComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly route          = inject(ActivatedRoute);
  private readonly router         = inject(Router);
  readonly auth                   = inject(AuthService);
  private readonly messaging      = inject(MessagingService);
  readonly comparator             = inject(ComparatorService);
  private readonly share          = inject(ShareService);
  private readonly negotiations   = inject(NegotiationService);
  private readonly fcfa           = new FcfaPipe();

  readonly vehicle          = signal<VehicleDetail | null>(null);
  readonly isLoading        = signal(true);
  readonly error            = signal<string | null>(null);
  readonly similarVehicles  = signal<VehicleListItem[]>([]);

  readonly isFavorited      = signal(false);
  readonly contacting       = signal(false);
  readonly copied           = signal(false);
  readonly shareOpen        = signal(false);
  readonly comparatorFull   = signal(false);
  readonly requestSent      = signal<string | null>(null);

  // ─── Galería ───────────────────────────────────────────────────────────
  readonly activeImageIndex = signal(0);
  /** Vista a pantalla completa. */
  readonly lightboxOpen     = signal(false);

  readonly images = computed(() => this.vehicle()?.images ?? []);
  readonly imageCount = computed(() => this.images().length);

  readonly activeImage = computed(() => this.images()[this.activeImageIndex()] ?? null);

  /** "3 / 12" */
  readonly imageCounter = computed(() =>
    this.imageCount() > 0 ? `${this.activeImageIndex() + 1} / ${this.imageCount()}` : '');

  selectImage(index: number): void {
    this.activeImageIndex.set(index);
  }

  nextImage(): void {
    if (this.imageCount() === 0) return;
    this.activeImageIndex.update(i => (i + 1) % this.imageCount());
  }

  prevImage(): void {
    if (this.imageCount() === 0) return;
    this.activeImageIndex.update(i => (i - 1 + this.imageCount()) % this.imageCount());
  }

  openLightbox(): void {
    if (this.imageCount() > 0) this.lightboxOpen.set(true);
  }

  closeLightbox(): void {
    this.lightboxOpen.set(false);
  }

  /** Flechas y Escape gobiernan la galería a pantalla completa. */
  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.lightboxOpen()) return;
    if (event.key === 'Escape')     { this.closeLightbox(); event.preventDefault(); }
    if (event.key === 'ArrowRight') { this.nextImage();     event.preventDefault(); }
    if (event.key === 'ArrowLeft')  { this.prevImage();     event.preventDefault(); }
  }

  private touchStartX = 0;

  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.changedTouches[0].screenX;
  }

  onTouchEnd(event: TouchEvent): void {
    const delta = event.changedTouches[0].screenX - this.touchStartX;
    if (Math.abs(delta) < 40) return;
    delta < 0 ? this.nextImage() : this.prevImage();
  }

  // ─── Estado del anuncio ────────────────────────────────────────────────
  readonly isSold     = computed(() => this.vehicle()?.status === 'Vendu');
  readonly isReserved = computed(() => this.vehicle()?.status === 'Reserve');

  /**
   * Un anuncio vendido conserva su ficha por favoritos, comparaciones y contratos,
   * pero deja de admitir ofertas y nuevos contactos.
   */
  readonly acceptsContact = computed(() => !this.isSold());

  // ─── Bloques de información ────────────────────────────────────────────
  /** "Toyota RAV4 2.0 VVT-i" */
  readonly headline = computed(() => {
    const v = this.vehicle();
    if (!v) return '';
    return [v.makeName, v.modelName, v.version].filter(Boolean).join(' ');
  });

  /** "2019 · 126.000 km · Essence · Automatique" */
  readonly summaryLine = computed(() => {
    const v = this.vehicle();
    if (!v) return '';
    return [
      v.year.toString(),
      v.mileage !== null ? `${v.mileage.toLocaleString('fr-FR')} km` : null,
      v.fuelType ? FUEL_LABELS[v.fuelType] : null,
      v.transmission ? TRANSMISSION_LABELS[v.transmission] : null
    ].filter(Boolean).join(' · ');
  });

  /**
   * Indicador estadístico de precio. `null` cuando no hay comparables suficientes:
   * la especificación prohíbe mostrar un indicador inventado.
   */
  readonly priceIndicatorLabel = computed(() => {
    const i = this.vehicle()?.priceIndicator;
    return i ? PRICE_INDICATOR_LABELS[i] : null;
  });

  readonly priceIndicatorClass = computed(() => {
    const i = this.vehicle()?.priceIndicator;
    return i ? PRICE_INDICATOR_CLASSES[i] : '';
  });

  /**
   * Bloque «Caractéristiques → Informations générales».
   * Los campos opcionales sin rellenar se omiten en lugar de llenar la pantalla
   * de «Non renseigné», como indica la especificación.
   */
  readonly generalSpecs = computed<Spec[]>(() => {
    const v = this.vehicle();
    if (!v) return [];
    const specs: Spec[] = [
      { label: 'Marque', value: v.makeName }
    ];
    if (v.modelName)    specs.push({ label: 'Modèle', value: v.modelName });
    if (v.version)      specs.push({ label: 'Version', value: v.version });
    specs.push({ label: 'Année', value: v.year.toString() });
    if (v.mileage !== null) specs.push({ label: 'Kilométrage', value: `${v.mileage.toLocaleString('fr-FR')} km` });
    if (v.fuelType)     specs.push({ label: 'Carburant', value: FUEL_LABELS[v.fuelType] });
    if (v.transmission) specs.push({ label: 'Boîte de vitesses', value: TRANSMISSION_LABELS[v.transmission] });
    if (v.bodyType)     specs.push({ label: 'Carrosserie', value: BODY_LABELS[v.bodyType] });
    if (v.color)        specs.push({ label: 'Couleur', value: v.color });
    if (v.doors !== null) specs.push({ label: 'Portes', value: v.doors.toString() });
    if (v.seats !== null) specs.push({ label: 'Places', value: v.seats.toString() });
    return specs;
  });

  /** Bloque «Caractéristiques → Moteur». */
  readonly engineSpecs = computed<Spec[]>(() => {
    const v = this.vehicle();
    if (!v) return [];
    const specs: Spec[] = [];
    if (v.powerCv !== null)              specs.push({ label: 'Puissance', value: `${v.powerCv} CV` });
    if (v.engineDisplacementCc !== null) specs.push({ label: 'Cylindrée', value: `${v.engineDisplacementCc.toLocaleString('fr-FR')} cm³` });
    if (v.drivetrain)                    specs.push({ label: 'Transmission', value: DRIVETRAIN_LABELS[v.drivetrain] });
    if (v.engineName)                    specs.push({ label: 'Motorisation', value: v.engineName });
    return specs;
  });

  // ─── Vendu par ─────────────────────────────────────────────────────────
  readonly sellerMemberSince = computed(() => {
    const d = this.vehicle()?.sellerMemberSince;
    return d ? new Date(d).getFullYear() : null;
  });

  // ─── Évolution du prix ─────────────────────────────────────────────────
  /** Bajada acumulada respecto al precio inicial, si la hubo. */
  readonly priceDrop = computed(() => {
    const v = this.vehicle();
    if (!v?.initialPrice || v.initialPrice <= v.price) return null;
    return v.initialPrice - v.price;
  });

  // ─── Ciclo de vida ─────────────────────────────────────────────────────
  readonly infoRequests = INFO_REQUESTS;

  ngOnInit(): void {
    // El slug cambia al navegar entre vehículos similares sin recrear el componente.
    this.route.paramMap.subscribe(params => {
      const slug = params.get('slug');
      if (slug) this.load(slug);
    });
  }

  private load(slug: string): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.activeImageIndex.set(0);
    this.similarVehicles.set([]);

    this.vehicleService.getVehicleBySlug(slug).subscribe({
      next: v => {
        this.vehicle.set(v);
        this.isLoading.set(false);
        this.loadSimilar(v.id);
        this.loadTransparency(v.id);
        // El contador es informativo: un fallo aquí no debe afectar a la ficha.
        this.vehicleService.registerView(v.id).subscribe({ error: () => {} });
      },
      error: () => {
        this.error.set('Ce véhicule est introuvable.');
        this.isLoading.set(false);
      }
    });
  }

  // ─── Signaler l'annonce ────────────────────────────────────────────────
  /**
   * Motivos de la especificación. El desplegable los muestra en francés y el backend
   * recibe el nombre del enum.
   */
  readonly reportReasons: readonly { value: string; label: string }[] = [
    { value: 'AnnonceSuspecte',    label: 'Annonce suspecte' },
    { value: 'InformationFausse',  label: 'Information fausse' },
    { value: 'PrixTrompeur',       label: 'Prix trompeur' },
    { value: 'PhotosIncorrectes',  label: 'Photographies incorrectes' },
    { value: 'VehiculeInexistant', label: 'Véhicule inexistant' },
    { value: 'TentativeDeFraude',  label: 'Tentative de fraude' },
    { value: 'Spam',               label: 'Spam' },
    { value: 'Autre',              label: 'Autre motif' }
  ];

  readonly reportOpen = signal(false);
  readonly reportSent = signal<string | null>(null);
  readonly reportBusy = signal(false);
  readonly reportError = signal<string | null>(null);

  reportReason = 'AnnonceSuspecte';
  reportDescription = '';

  /** Reportar exige cuenta: sin ella no hay a quién responder. */
  toggleReport(): void {
    if (!this.requireAccount()) return;
    this.reportOpen.update(v => !v);
    this.reportError.set(null);
  }

  submitReport(): void {
    const v = this.vehicle();
    if (!v || this.reportBusy()) return;

    this.reportBusy.set(true);
    this.reportError.set(null);

    this.vehicleService.report({
      targetType: 'Listing',
      targetId: v.id,
      reason: this.reportReason,
      description: this.reportDescription.trim() || null
    }).subscribe({
      next: r => {
        this.reportBusy.set(false);
        this.reportOpen.set(false);
        this.reportDescription = '';
        this.reportSent.set(r.reference);
      },
      error: () => {
        this.reportBusy.set(false);
        this.reportError.set("Signalement impossible. Peut-être l'avez-vous déjà signalé.");
      }
    });
  }

  // ─── Transparence du véhicule ──────────────────────────────────────────
  /** El historial que quien vende ha decidido enseñar. `null` si no comparte nada. */
  readonly transparency = signal<PublicTransparency | null>(null);

  private loadTransparency(vehicleId: string): void {
    this.transparency.set(null);
    this.vehicleService.getTransparency(vehicleId).subscribe({
      next: t => this.transparency.set(t),
      error: () => this.transparency.set(null)
    });
  }

  invoiceUrl(documentId: string): string {
    const v = this.vehicle();
    return v ? this.vehicleService.sharedInvoiceUrl(v.id, documentId) : '';
  }

  private loadSimilar(vehicleId: string): void {
    this.vehicleService.getSimilarVehicles(vehicleId, 8).subscribe({
      next: items => this.similarVehicles.set(items),
      error: () => this.similarVehicles.set([])
    });
  }

  // ─── Acciones ──────────────────────────────────────────────────────────
  /** Favoritos, comparador, oferta y contacto exigen cuenta. */
  private requireAccount(): boolean {
    if (this.auth.isAuthenticated()) return true;
    const v = this.vehicle();
    this.router.navigate(['/auth/register'], {
      queryParams: { returnUrl: v ? `/vehiculos/${v.slug}` : '/vehiculos' }
    });
    return false;
  }

  toggleFavorite(): void {
    const v = this.vehicle();
    if (!v || !this.requireAccount()) return;

    const previous = this.isFavorited();
    this.isFavorited.set(!previous);

    this.vehicleService.toggleFavorite(v.id).subscribe({
      next: r => this.isFavorited.set(r.isSaved),
      error: () => this.isFavorited.set(previous)
    });
  }

  readonly isCompared = computed(() => {
    const v = this.vehicle();
    return v ? this.comparator.has(v.id) : false;
  });

  toggleCompare(): void {
    const v = this.vehicle();
    if (!v || !this.requireAccount()) return;

    if (this.comparator.toggle(v.id) === 'full') {
      this.comparatorFull.set(true);
      setTimeout(() => this.comparatorFull.set(false), 3000);
    }
  }

  // ─── Partager: no requiere registro ────────────────────────────────────
  toggleShare(): void {
    this.shareOpen.update(v => !v);
  }

  private get shareTarget() {
    const v = this.vehicle()!;
    return {
      title: this.headline(),
      url: this.share.vehicleUrl(v.slug),
      price: this.fcfa.transform(v.price)
    };
  }

  shareWhatsApp(): void { this.share.whatsapp(this.shareTarget); this.shareOpen.set(false); }
  shareEmail(): void    { this.share.email(this.shareTarget);    this.shareOpen.set(false); }
  shareNative(): void   { void this.share.native(this.shareTarget); this.shareOpen.set(false); }

  get supportsNativeShare(): boolean { return this.share.supportsNativeShare; }

  async copyLink(): Promise<void> {
    if (await this.share.copyLink(this.shareTarget)) {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    }
    this.shareOpen.set(false);
  }

  // ─── Contacto ──────────────────────────────────────────────────────────
  contactSeller(): void {
    this.sendToSeller(
      `Bonjour, je suis intéressé par votre ${this.headline()} (Réf. ${this.vehicle()?.publicReference}). ` +
      'Pourriez-vous me donner plus d\'informations ?'
    );
  }

  /** «Besoin de plus d'informations ?»: abre el chat con el mensaje ya redactado. */
  requestInfo(request: { label: string; message: string }): void {
    const v = this.vehicle();
    if (!v) return;
    this.sendToSeller(`${request.message} (Réf. ${v.publicReference})`, request.label);
  }

  // ─── Faire une offre ───────────────────────────────────────────────────
  readonly offerFormOpen = signal(false);
  readonly sendingOffer = signal(false);
  readonly offerError = signal<string | null>(null);
  offerAmount = '';
  offerMessage = '';

  toggleOfferForm(): void {
    if (!this.requireAccount()) return;
    this.offerFormOpen.update(v => !v);
    this.offerError.set(null);
  }

  /**
   * El importe es opcional según la especificación: sin él, el formulario solo abre la
   * conversación con el vendedor.
   */
  sendOffer(): void {
    const v = this.vehicle();
    if (!v || this.sendingOffer() || !this.requireAccount()) return;

    const amount = this.offerAmount ? Number(this.offerAmount) : null;
    const message = this.offerMessage.trim() || null;

    if (amount === null && message === null) {
      this.offerError.set('Indiquez un montant ou écrivez un message.');
      return;
    }
    if (amount !== null && (!Number.isFinite(amount) || amount <= 0)) {
      this.offerError.set('Le montant doit être supérieur à 0.');
      return;
    }

    this.sendingOffer.set(true);
    this.offerError.set(null);

    this.negotiations.makeOffer(v.id, amount, message).subscribe({
      next: r => {
        this.sendingOffer.set(false);
        this.offerFormOpen.set(false);
        this.offerAmount = '';
        this.offerMessage = '';
        this.router.navigate(['/mis-negociaciones', r.negotiationId]);
      },
      error: err => {
        this.sendingOffer.set(false);
        this.offerError.set(this.offerErrorMessage(err?.error));
      }
    });
  }

  private offerErrorMessage(code: string | undefined): string {
    switch (code) {
      case 'Offer.CannotOfferOnOwnVehicle':
        return 'Vous ne pouvez pas faire une offre sur votre propre annonce.';
      case 'Vehicle.NotOpenForNegotiation':
        return "Ce véhicule n'accepte plus d'offres.";
      case 'Offer.InvalidAmount':
        return 'Le montant doit être supérieur à 0.';
      default:
        return "Impossible d'envoyer l'offre. Veuillez réessayer.";
    }
  }

  private sendToSeller(body: string, requestLabel?: string): void {
    const v = this.vehicle();
    if (!v || !this.acceptsContact() || !this.requireAccount()) return;

    this.contacting.set(true);
    this.messaging.sendMessageRest(v.sellerId, v.id, body).subscribe({
      next: () => {
        this.contacting.set(false);
        if (requestLabel) this.requestSent.set(requestLabel);
        this.router.navigate(['/mis-negociaciones']);
      },
      error: () => {
        // La conversación puede existir ya: se abre igualmente la bandeja.
        this.contacting.set(false);
        this.router.navigate(['/mis-negociaciones']);
      }
    });
  }
}
