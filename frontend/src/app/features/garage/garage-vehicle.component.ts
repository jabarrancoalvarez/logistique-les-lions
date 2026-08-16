import {
  Component, ChangeDetectionStrategy, OnInit, OnDestroy, signal, computed, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  GarageService, GarageVehicleForm, GarageVehicleDetail, GarageVehicleImage,
  GarageDocument, GarageDocumentType, GARAGE_DOCUMENT_LABELS,
  MaintenanceHistory, MaintenanceRecord, MaintenanceForm, MaintenanceType,
  MAINTENANCE_LABELS,
  Reminder, ReminderForm, ReminderType, ReminderStatus,
  REMINDER_LABELS, REMINDER_STATUS_LABELS,
  VehicleValuation, VALUATION_CRITERIA,
  Completeness, CompletenessItem, CompletenessCheck, COMPLETENESS_LEVEL_LABELS,
  CreateListingResult, TransparencySettings, TransparencyRecord
} from '@core/services/garage.service';
import {
  VehicleService, VehicleMake, VehicleModelOption,
  FuelType, TransmissionType, BodyType,
  FUEL_LABELS, TRANSMISSION_LABELS, BODY_LABELS
} from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/** Ficha vacía: solo el año arranca con un valor razonable. */
const EMPTY_FORM: GarageVehicleForm = {
  makeId: '',
  modelId: null,
  version: null,
  year: new Date().getFullYear(),
  mileage: null,
  fuelType: null,
  transmission: null,
  bodyType: null,
  powerCv: null,
  engineDisplacementCc: null,
  color: null,
  registrationPlate: null,
  vin: null,
  purchaseDate: null,
  purchasePrice: null
};

const FUEL_TYPES: FuelType[] =
  ['Diesel', 'Essence', 'Hybride', 'HybrideRechargeable', 'Electrique', 'Autre'];
const TRANSMISSIONS: TransmissionType[] = ['Manuel', 'Automatique'];
const BODY_TYPES: BodyType[] =
  ['Citadine', 'Berline', 'Break', 'Suv', 'Coupe', 'Cabriolet', 'Monospace', 'PickUp', 'Utilitaire', 'Autre'];

/**
 * Ficha de un vehículo de Mon Garage: alta, consulta y corrección en una sola pantalla.
 *
 * Cubre tres entradas:
 * - `/mi-garaje/nuevo` — alta manual desde cero.
 * - `/mi-garaje/nuevo?contrat=<id>` — «Ajouter ce véhicule à Mon Garage» tras una venta
 *   verificada, con la ficha ya rellena.
 * - `/mi-garaje/:id` — el vehículo que ya está en el garaje.
 */
@Component({
  selector: 'lll-garage-vehicle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, FcfaPipe],
  templateUrl: './garage-vehicle.component.html'
})
export class GarageVehicleComponent implements OnInit, OnDestroy {
  private readonly service = inject(GarageService);
  private readonly vehicles = inject(VehicleService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly vehicle = signal<GarageVehicleDetail | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly notFound = signal(false);

  /** En alta el formulario está abierto de entrada; en consulta se abre al pulsar. */
  readonly editing = signal(false);
  readonly isNew = signal(false);

  readonly makes = signal<VehicleMake[]>([]);
  readonly models = signal<VehicleModelOption[]>([]);

  form: GarageVehicleForm = { ...EMPTY_FORM };

  /** Contrato de origen, cuando se llega desde una venta verificada. */
  private contractId: string | null = null;

  readonly fuelTypes = FUEL_TYPES;
  readonly transmissions = TRANSMISSIONS;
  readonly bodyTypes = BODY_TYPES;

  fuelLabel = (f: FuelType) => FUEL_LABELS[f];
  transmissionLabel = (t: TransmissionType) => TRANSMISSION_LABELS[t];
  bodyLabel = (b: BodyType) => BODY_LABELS[b];

  readonly images = computed<GarageVehicleImage[]>(() => this.vehicle()?.images ?? []);

  ngOnInit(): void {
    this.vehicles.getMakes().subscribe({
      next: m => this.makes.set(m),
      error: () => this.makes.set([])
    });

    const id = this.route.snapshot.paramMap.get('id');
    this.contractId = this.route.snapshot.queryParamMap.get('contrat');

    if (id) {
      this.load(id);
      return;
    }

    this.isNew.set(true);
    this.editing.set(true);

    if (this.contractId) {
      this.loadPrefill(this.contractId);
    } else {
      this.loading.set(false);
    }
  }

  ngOnDestroy(): void {
    // Las URLs de las fotos privadas viven en memoria: hay que liberarlas.
    this.revokePhotos();
  }

  private load(id: string): void {
    this.service.getVehicle(id).subscribe({
      next: v => {
        this.vehicle.set(v);
        this.form = this.toForm(v);
        this.loadModels(v.makeId);
        this.loadDocuments(v.id);
        this.loadMaintenance(v.id);
        this.loadReminders(v.id);
        this.loadValuation(v.id);
        this.loadCompleteness(v.id);
        if (v.listedVehicleId) this.loadTransparency(v.listedVehicleId);
        this.loading.set(false);
      },
      error: () => { this.notFound.set(true); this.loading.set(false); }
    });
  }

  /** Tras la compra, la ficha llega rellena del contrato y del anuncio. */
  private loadPrefill(contractId: string): void {
    this.service.getPrefillFromContract(contractId).subscribe({
      next: p => {
        // Si ya se incorporó, no tiene sentido volver a ofrecerlo: se abre el que existe.
        if (p.alreadyAdded && p.existingGarageVehicleId) {
          void this.router.navigate(['/mi-garaje', p.existingGarageVehicleId]);
          return;
        }
        this.form = { ...p.vehicle };
        this.loadModels(p.vehicle.makeId);
        this.loading.set(false);
      },
      error: () => { this.notFound.set(true); this.loading.set(false); }
    });
  }

  private toForm(v: GarageVehicleDetail): GarageVehicleForm {
    return {
      makeId: v.makeId,
      modelId: v.modelId,
      version: v.version,
      year: v.year,
      mileage: v.mileage,
      fuelType: v.fuelType,
      transmission: v.transmission,
      bodyType: v.bodyType,
      powerCv: v.powerCv,
      engineDisplacementCc: v.engineDisplacementCc,
      color: v.color,
      registrationPlate: v.registrationPlate,
      vin: v.vin,
      // El input date solo entiende yyyy-MM-dd.
      purchaseDate: v.purchaseDate ? v.purchaseDate.substring(0, 10) : null,
      purchasePrice: v.purchasePrice
    };
  }

  private loadModels(makeId: string): void {
    if (!makeId) { this.models.set([]); return; }
    this.vehicles.getModels(makeId).subscribe({
      next: m => this.models.set(m),
      error: () => this.models.set([])
    });
  }

  onMakeChange(): void {
    // El modelo pertenece a una marca: al cambiarla deja de ser válido.
    this.form.modelId = null;
    this.loadModels(this.form.makeId);
  }

  startEditing(): void {
    const v = this.vehicle();
    if (v) this.form = this.toForm(v);
    this.error.set(null);
    this.editing.set(true);
  }

  cancelEditing(): void {
    if (this.isNew()) {
      void this.router.navigate(['/mi-garaje']);
      return;
    }
    this.editing.set(false);
    this.error.set(null);
  }

  save(): void {
    if (this.saving()) return;

    if (!this.form.makeId) {
      this.error.set('La marque est obligatoire.');
      return;
    }
    if (!this.form.year) {
      this.error.set('L\'année est obligatoire.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const existing = this.vehicle();
    const failed = () => {
      this.saving.set(false);
      this.error.set('Enregistrement impossible. Vérifiez les données saisies.');
    };

    if (existing) {
      this.service.update(existing.id, this.form).subscribe({
        next: () => {
          this.saving.set(false);
          this.editing.set(false);
          this.load(existing.id);
        },
        error: failed
      });
      return;
    }

    this.service.create(this.form, this.contractId).subscribe({
      next: created => {
        this.saving.set(false);
        void this.router.navigate(['/mi-garaje', created.id]);
      },
      error: failed
    });
  }

  remove(): void {
    const v = this.vehicle();
    if (!v || this.saving()) return;

    this.saving.set(true);
    this.service.remove(v.id).subscribe({
      next: () => void this.router.navigate(['/mi-garaje']),
      error: () => { this.saving.set(false); this.error.set('Suppression impossible.'); }
    });
  }

  // ─── Photographies ───────────────────────────────────────────────────────
  readonly uploading = signal(false);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const v = this.vehicle();
    if (!file || !v || this.uploading()) return;

    this.uploading.set(true);
    this.service.uploadImage(v.id, file, false, this.images().length).subscribe({
      next: () => { this.uploading.set(false); input.value = ''; this.load(v.id); },
      error: () => {
        this.uploading.set(false);
        input.value = '';
        this.error.set('Envoi de la photo impossible.');
      }
    });
  }

  deleteImage(image: GarageVehicleImage): void {
    const v = this.vehicle();
    if (!v || this.uploading()) return;

    this.uploading.set(true);
    this.service.deleteImage(image.id).subscribe({
      next: () => { this.uploading.set(false); this.load(v.id); },
      error: () => { this.uploading.set(false); this.error.set('Suppression impossible.'); }
    });
  }

  // ─── Documents ───────────────────────────────────────────────────────────
  readonly documents = signal<GarageDocument[]>([]);
  readonly documentBusy = signal(false);
  readonly documentFormOpen = signal(false);
  readonly documentError = signal<string | null>(null);

  readonly documentTypes: GarageDocumentType[] = [
    'CarteGrise', 'Assurance', 'ControleTechnique', 'ContratDeVente', 'FactureAchat',
    'FactureEntretien', 'FactureReparation', 'Douane', 'Autre'
  ];

  documentTypeLabel = (t: GarageDocumentType) => GARAGE_DOCUMENT_LABELS[t];

  /** Documento en edición; `null` cuando se está subiendo uno nuevo. */
  editingDocument: GarageDocument | null = null;
  documentForm = {
    type: 'CarteGrise' as GarageDocumentType,
    name: '',
    documentDate: null as string | null,
    notes: null as string | null
  };
  private documentFile: File | null = null;

  private loadDocuments(vehicleId: string): void {
    this.service.getDocuments(vehicleId).subscribe({
      next: d => this.documents.set(d),
      error: () => this.documents.set([])
    });
  }

  openDocumentForm(document: GarageDocument | null = null): void {
    this.editingDocument = document;
    this.documentFile = null;
    this.documentError.set(null);

    this.documentForm = document
      ? {
          type: document.type,
          name: document.name,
          documentDate: document.documentDate ? document.documentDate.substring(0, 10) : null,
          notes: document.notes
        }
      : { type: 'CarteGrise', name: '', documentDate: null, notes: null };

    this.documentFormOpen.set(true);
  }

  closeDocumentForm(): void {
    this.documentFormOpen.set(false);
    this.editingDocument = null;
    this.documentFile = null;
    this.documentError.set(null);
  }

  onDocumentFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.documentFile = input.files?.[0] ?? null;

    // Sin nombre propio, el del archivo sirve de punto de partida.
    if (this.documentFile && !this.documentForm.name.trim())
      this.documentForm.name = this.documentFile.name;
  }

  saveDocument(): void {
    const v = this.vehicle();
    if (!v || this.documentBusy()) return;

    if (!this.documentForm.name.trim()) {
      this.documentError.set('Le nom du document est obligatoire.');
      return;
    }

    const editing = this.editingDocument;
    if (!editing && !this.documentFile) {
      this.documentError.set('Choisissez un fichier.');
      return;
    }

    this.documentBusy.set(true);
    this.documentError.set(null);

    const done = () => {
      this.documentBusy.set(false);
      this.closeDocumentForm();
      this.loadDocuments(v.id);
    };
    const failed = () => {
      this.documentBusy.set(false);
      this.documentError.set('Enregistrement impossible.');
    };

    if (editing) {
      // Reclasificar no sustituye el archivo: para eso se sube otro.
      this.service.updateDocument(editing.id, this.documentForm)
        .subscribe({ next: done, error: failed });
      return;
    }

    this.service.uploadDocument(v.id, this.documentFile!, this.documentForm)
      .subscribe({ next: done, error: failed });
  }

  deleteDocument(document: GarageDocument): void {
    const v = this.vehicle();
    if (!v || this.documentBusy()) return;

    this.documentBusy.set(true);
    this.service.deleteDocument(document.id).subscribe({
      next: () => { this.documentBusy.set(false); this.loadDocuments(v.id); },
      error: () => {
        this.documentBusy.set(false);
        this.documentError.set('Suppression impossible.');
      }
    });
  }

  downloadDocument(document: GarageDocument): void {
    if (this.documentBusy()) return;

    this.documentBusy.set(true);
    this.service.downloadDocument(document.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = window.document.createElement('a');
        link.href = url;
        link.download = document.fileName;
        link.click();
        URL.revokeObjectURL(url);
        this.documentBusy.set(false);
      },
      error: () => {
        this.documentBusy.set(false);
        this.documentError.set('Téléchargement impossible.');
      }
    });
  }

  // ─── Entretien ───────────────────────────────────────────────────────────
  readonly maintenance = signal<MaintenanceHistory | null>(null);
  readonly maintenanceBusy = signal(false);
  readonly maintenanceFormOpen = signal(false);
  readonly maintenanceError = signal<string | null>(null);

  readonly maintenanceTypes: MaintenanceType[] = [
    'Vidange', 'Filtres', 'Pneus', 'Freins', 'Batterie', 'Distribution',
    'Embrayage', 'Suspension', 'Climatisation', 'ReparationMoteur',
    'RevisionGenerale', 'Autre'
  ];

  maintenanceLabel = (t: MaintenanceType) => MAINTENANCE_LABELS[t];

  /** Intervención en edición; `null` cuando se registra una nueva. */
  editingRecord: MaintenanceRecord | null = null;
  maintenanceForm: MaintenanceForm = this.emptyMaintenanceForm();

  private emptyMaintenanceForm(): MaintenanceForm {
    return {
      type: 'Vidange',
      performedAt: new Date().toISOString().substring(0, 10),
      // El kilometraje actual del vehículo es el punto de partida más probable.
      mileage: this.vehicle()?.mileage ?? null,
      description: '',
      cost: null,
      workshop: null,
      notes: null,
      documentId: null
    };
  }

  private loadMaintenance(vehicleId: string): void {
    this.service.getMaintenance(vehicleId).subscribe({
      next: h => this.maintenance.set(h),
      error: () => this.maintenance.set(null)
    });
  }

  openMaintenanceForm(record: MaintenanceRecord | null = null): void {
    this.editingRecord = record;
    this.maintenanceError.set(null);

    this.maintenanceForm = record
      ? { ...record, performedAt: record.performedAt.substring(0, 10) }
      : this.emptyMaintenanceForm();

    if (record) this.loadPhotos(record); else this.revokePhotos();

    this.maintenanceFormOpen.set(true);
  }

  closeMaintenanceForm(): void {
    this.maintenanceFormOpen.set(false);
    this.editingRecord = null;
    this.maintenanceError.set(null);
    this.revokePhotos();
  }

  saveMaintenance(): void {
    const v = this.vehicle();
    if (!v || this.maintenanceBusy()) return;

    if (!this.maintenanceForm.description.trim()) {
      this.maintenanceError.set('La description est obligatoire.');
      return;
    }

    this.maintenanceBusy.set(true);
    this.maintenanceError.set(null);

    const editing = this.editingRecord;
    const done = () => {
      this.maintenanceBusy.set(false);
      this.closeMaintenanceForm();
      this.loadMaintenance(v.id);
      // Una intervención reciente pone al día el kilometraje del vehículo.
      this.load(v.id);
    };
    const failed = () => {
      this.maintenanceBusy.set(false);
      this.maintenanceError.set('Enregistrement impossible.');
    };

    if (editing) {
      this.service.updateMaintenance(editing.id, this.maintenanceForm)
        .subscribe({ next: done, error: failed });
      return;
    }

    this.service.addMaintenance(v.id, this.maintenanceForm)
      .subscribe({ next: done, error: failed });
  }

  deleteMaintenance(record: MaintenanceRecord): void {
    const v = this.vehicle();
    if (!v || this.maintenanceBusy()) return;

    this.maintenanceBusy.set(true);
    this.service.deleteMaintenance(record.id).subscribe({
      next: () => { this.maintenanceBusy.set(false); this.loadMaintenance(v.id); },
      error: () => {
        this.maintenanceBusy.set(false);
        this.maintenanceError.set('Suppression impossible.');
      }
    });
  }

  // ─── Vendre ce véhicule ──────────────────────────────────────────────────
  readonly selling = signal(false);
  readonly sellResult = signal<CreateListingResult | null>(null);
  readonly sellError = signal<string | null>(null);

  /** El coche ya tiene un anuncio vivo, salvo que se haya vendido o archivado. */
  readonly isListed = computed(() => {
    const v = this.vehicle();
    if (!v?.listedVehicleId) return false;
    return v.listedVehicleStatus !== 'Vendu' && v.listedVehicleStatus !== 'Archive';
  });

  sell(): void {
    const v = this.vehicle();
    if (!v || this.selling()) return;

    this.selling.set(true);
    this.sellError.set(null);

    this.service.sell(v.id).subscribe({
      next: result => {
        this.selling.set(false);
        this.sellResult.set(result);
        // La ficha ahora apunta al anuncio, y con él llega la transparencia.
        this.load(v.id);
      },
      error: () => {
        this.selling.set(false);
        this.sellError.set('Création du brouillon impossible.');
      }
    });
  }

  // ─── Transparence du véhicule ────────────────────────────────────────────
  readonly transparency = signal<TransparencySettings | null>(null);
  readonly transparencyBusy = signal(false);
  readonly transparencySaved = signal(false);

  private loadTransparency(listingId: string): void {
    this.service.getTransparency(listingId).subscribe({
      next: t => this.transparency.set(t),
      error: () => this.transparency.set(null)
    });
  }

  toggleTransparency(field: 'showMaintenanceHistory' | 'showMaintenanceDetails' | 'showMileageEvolution'): void {
    this.transparency.update(t => t && { ...t, [field]: !t[field] });
  }

  toggleRecord(record: TransparencyRecord): void {
    this.transparency.update(t => t && {
      ...t,
      records: t.records.map(r => r.maintenanceRecordId === record.maintenanceRecordId
        // Dejar de compartir la intervención se lleva por delante su factura.
        ? { ...r, shared: !r.shared, shareInvoice: r.shared ? false : r.shareInvoice }
        : r)
    });
  }

  toggleRecordInvoice(record: TransparencyRecord): void {
    this.transparency.update(t => t && {
      ...t,
      records: t.records.map(r => r.maintenanceRecordId === record.maintenanceRecordId
        ? { ...r, shareInvoice: !r.shareInvoice }
        : r)
    });
  }

  saveTransparency(): void {
    const t = this.transparency();
    if (!t || this.transparencyBusy()) return;

    this.transparencyBusy.set(true);
    this.service.saveTransparency(t.vehicleId, {
      showMaintenanceHistory: t.showMaintenanceHistory,
      showMaintenanceDetails: t.showMaintenanceDetails,
      showMileageEvolution: t.showMileageEvolution,
      records: t.records.map(r => ({
        maintenanceRecordId: r.maintenanceRecordId,
        shared: r.shared,
        shareInvoice: r.shareInvoice
      }))
    }).subscribe({
      next: () => {
        this.transparencyBusy.set(false);
        this.transparencySaved.set(true);
        setTimeout(() => this.transparencySaved.set(false), 3000);
      },
      error: () => this.transparencyBusy.set(false)
    });
  }

  // ─── Complétude du dossier ───────────────────────────────────────────────
  readonly completeness = signal<Completeness | null>(null);

  private loadCompleteness(vehicleId: string): void {
    this.service.getCompleteness(vehicleId).subscribe({
      next: c => this.completeness.set(c),
      error: () => this.completeness.set(null)
    });
  }

  completenessLevelLabel(c: Completeness): string {
    return COMPLETENESS_LEVEL_LABELS[c.level];
  }

  /** Texto de cada línea del desglose, con el dato concreto cuando lo hay. */
  completenessText(item: CompletenessItem): string {
    const n = item.detail ?? 0;

    const texts: Record<CompletenessCheck, string> = {
      MainInformation: item.status === 'Complete'
        ? 'Informations principales'
        : 'Informations principales à compléter',
      MileageUpToDate: item.status === 'Complete' ? 'Kilométrage à jour'
        : item.status === 'Partial' ? 'Kilométrage à mettre à jour'
        : 'Kilométrage non renseigné',
      Vin: item.status === 'Complete' ? 'VIN enregistré' : 'VIN à renseigner',
      Photos: item.status === 'Complete' ? `${n} photo${n > 1 ? 's' : ''}`
        : item.status === 'Partial' ? 'Photo principale ancienne'
        : 'Aucune photo',
      Documents: item.status === 'Complete' ? `${n} document${n > 1 ? 's' : ''}`
        : item.status === 'Partial' ? 'Carte grise ou assurance à ajouter'
        : 'Aucun document essentiel',
      // Como el resto: cuando falta algo se dice qué falta. Enunciar solo el recuento
      // junto a un ⚠ se leía como si tener una intervención fuera el problema.
      MaintenanceHistory: item.status === 'Complete'
        ? `${n} entretien${n > 1 ? 's' : ''} enregistré${n > 1 ? 's' : ''}`
        : n > 0
          ? `Historique d'entretien à étoffer (${n})`
          : 'Aucun entretien enregistré',
      Reminders: item.status === 'Complete'
        ? 'Rappels à jour'
        : `${n} rappel${n > 1 ? 's' : ''} en retard`,
      MaintenanceInvoices: item.status === 'Complete'
        ? 'Factures rattachées aux entretiens'
        : 'Entretiens sans facture rattachée'
    };

    return texts[item.check];
  }

  completenessIcon(item: CompletenessItem): string {
    return item.status === 'Complete' ? '✓' : '⚠';
  }

  // ─── Valeur estimée ──────────────────────────────────────────────────────
  readonly valuation = signal<VehicleValuation | null>(null);

  private loadValuation(vehicleId: string): void {
    this.service.getValuation(vehicleId).subscribe({
      next: v => this.valuation.set(v),
      error: () => this.valuation.set(null)
    });
  }

  /** En qué se ha basado la muestra, para poder explicarlo sin tecnicismos. */
  valuationBasis(criteria: number): string {
    const parts = ['modèle et année'];
    if (criteria & VALUATION_CRITERIA.Mileage) parts.push('kilométrage');
    if (criteria & VALUATION_CRITERIA.FuelAndTransmission) parts.push('carburant et boîte');
    if (criteria & VALUATION_CRITERIA.Region) parts.push('région');
    return parts.join(', ');
  }

  /**
   * Puntos de la curva de evolución, normalizados a una caja de 100 × 40.
   *
   * Es un gráfico deliberadamente sencillo, dibujado con un SVG en línea: no merece la
   * pena arrastrar una librería de gráficos para una serie de cuatro o cinco puntos.
   */
  readonly evolutionPath = computed(() => {
    const points = this.valuation()?.evolution?.points ?? [];
    if (points.length < 2) return null;

    const values = points.map(p => p.estimatedValue);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const span = max - min || 1;

    return points
      .map((p, i) => {
        const x = (i / (points.length - 1)) * 100;
        // El SVG crece hacia abajo: se invierte para que más valor quede más arriba.
        const y = 40 - ((p.estimatedValue - min) / span) * 40;
        return `${x.toFixed(1)},${y.toFixed(1)}`;
      })
      .join(' ');
  });

  // ─── Rappels ─────────────────────────────────────────────────────────────
  readonly reminders = signal<Reminder[]>([]);
  readonly reminderBusy = signal(false);
  readonly reminderFormOpen = signal(false);
  readonly reminderError = signal<string | null>(null);

  readonly reminderTypes: ReminderType[] = [
    'Vidange', 'Assurance', 'Inspection', 'Pneus', 'Distribution',
    'Freins', 'Revision', 'Autre'
  ];

  reminderLabel = (t: ReminderType) => REMINDER_LABELS[t];
  reminderStatusLabel = (s: ReminderStatus) => REMINDER_STATUS_LABELS[s];

  reminderStatusClass(status: ReminderStatus): string {
    const map: Record<ReminderStatus, string> = {
      AVenir:  'bg-navy/10 text-navy',
      AFaire:  'bg-amber-100 text-amber-800',
      Termine: 'bg-green-100 text-green-800',
      Annule:  'bg-navy/10 text-navy/50'
    };
    return map[status];
  }

  editingReminder: Reminder | null = null;
  reminderForm: ReminderForm = this.emptyReminderForm();

  private emptyReminderForm(): ReminderForm {
    return { type: 'Vidange', label: '', dueDate: null, dueMileage: null, notes: null };
  }

  private loadReminders(vehicleId: string): void {
    this.service.getReminders(vehicleId).subscribe({
      next: r => this.reminders.set(r),
      error: () => this.reminders.set([])
    });
  }

  openReminderForm(reminder: Reminder | null = null): void {
    this.editingReminder = reminder;
    this.reminderError.set(null);

    this.reminderForm = reminder
      ? {
          type: reminder.type,
          label: reminder.label,
          dueDate: reminder.dueDate ? reminder.dueDate.substring(0, 10) : null,
          dueMileage: reminder.dueMileage,
          notes: reminder.notes
        }
      : this.emptyReminderForm();

    this.reminderFormOpen.set(true);
  }

  closeReminderForm(): void {
    this.reminderFormOpen.set(false);
    this.editingReminder = null;
    this.reminderError.set(null);
  }

  saveReminder(): void {
    const v = this.vehicle();
    if (!v || this.reminderBusy()) return;

    if (!this.reminderForm.label.trim()) {
      this.reminderError.set('Le libellé est obligatoire.');
      return;
    }
    // Sin fecha ni kilometraje no hay nada que vigilar.
    if (!this.reminderForm.dueDate && !this.reminderForm.dueMileage) {
      this.reminderError.set('Indiquez une date, un kilométrage, ou les deux.');
      return;
    }

    this.reminderBusy.set(true);
    this.reminderError.set(null);

    const editing = this.editingReminder;
    const done = () => {
      this.reminderBusy.set(false);
      this.closeReminderForm();
      this.loadReminders(v.id);
    };
    const failed = () => {
      this.reminderBusy.set(false);
      this.reminderError.set('Enregistrement impossible.');
    };

    if (editing) {
      this.service.updateReminder(editing.id, this.reminderForm)
        .subscribe({ next: done, error: failed });
      return;
    }

    this.service.addReminder(v.id, this.reminderForm).subscribe({ next: done, error: failed });
  }

  setReminderStatus(reminder: Reminder, status: ReminderStatus): void {
    const v = this.vehicle();
    if (!v || this.reminderBusy()) return;

    this.reminderBusy.set(true);
    this.service.setReminderStatus(reminder.id, status).subscribe({
      next: () => { this.reminderBusy.set(false); this.loadReminders(v.id); },
      error: () => {
        this.reminderBusy.set(false);
        this.reminderError.set('Action impossible.');
      }
    });
  }

  deleteReminder(reminder: Reminder): void {
    const v = this.vehicle();
    if (!v || this.reminderBusy()) return;

    this.reminderBusy.set(true);
    this.service.deleteReminder(reminder.id).subscribe({
      next: () => { this.reminderBusy.set(false); this.loadReminders(v.id); },
      error: () => {
        this.reminderBusy.set(false);
        this.reminderError.set('Suppression impossible.');
      }
    });
  }

  /** «Dans 12 jours», «2.500 km restants», «Dépassé de 300 km»… */
  reminderCountdown(reminder: Reminder): string | null {
    const parts: string[] = [];

    if (reminder.daysRemaining !== null) {
      parts.push(reminder.daysRemaining >= 0
        ? `dans ${reminder.daysRemaining} j`
        : `en retard de ${-reminder.daysRemaining} j`);
    }

    if (reminder.mileageRemaining !== null) {
      const km = Math.abs(reminder.mileageRemaining).toLocaleString('de-DE');
      parts.push(reminder.mileageRemaining >= 0
        ? `${km} km restants`
        : `dépassé de ${km} km`);
    }

    return parts.length ? parts.join(' · ') : null;
  }

  // ─── Photos d'une intervention ───────────────────────────────────────────
  /**
   * URLs temporales de las fotos de la intervención que se está editando.
   *
   * Estas fotos no son públicas: llegan como blob por un endpoint autenticado, así que
   * hay que construir la URL en memoria y liberarla al cerrar el formulario.
   */
  readonly photoUrls = signal<Record<string, string>>({});

  private loadPhotos(record: MaintenanceRecord): void {
    this.revokePhotos();

    for (const image of record.images) {
      this.service.getMaintenanceImage(image.id).subscribe({
        next: blob => this.photoUrls.update(urls => ({
          ...urls, [image.id]: URL.createObjectURL(blob)
        })),
        error: () => { /* una foto que falla no debe romper el formulario */ }
      });
    }
  }

  private revokePhotos(): void {
    for (const url of Object.values(this.photoUrls())) URL.revokeObjectURL(url);
    this.photoUrls.set({});
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const record = this.editingRecord;
    const v = this.vehicle();
    if (!file || !record || !v || this.maintenanceBusy()) return;

    this.maintenanceBusy.set(true);
    this.service.uploadMaintenanceImage(record.id, file).subscribe({
      next: () => { input.value = ''; this.refreshEditingRecord(v.id, record.id); },
      error: () => {
        this.maintenanceBusy.set(false);
        input.value = '';
        this.maintenanceError.set('Envoi de la photo impossible.');
      }
    });
  }

  deletePhoto(imageId: string): void {
    const record = this.editingRecord;
    const v = this.vehicle();
    if (!record || !v || this.maintenanceBusy()) return;

    this.maintenanceBusy.set(true);
    this.service.deleteMaintenanceImage(imageId).subscribe({
      next: () => this.refreshEditingRecord(v.id, record.id),
      error: () => {
        this.maintenanceBusy.set(false);
        this.maintenanceError.set('Suppression impossible.');
      }
    });
  }

  /** Recarga el historial y vuelve a apuntar el formulario a la misma intervención. */
  private refreshEditingRecord(vehicleId: string, recordId: string): void {
    this.service.getMaintenance(vehicleId).subscribe({
      next: h => {
        this.maintenance.set(h);
        const updated = h.years
          .flatMap(y => y.records)
          .find(r => r.id === recordId) ?? null;

        this.editingRecord = updated;
        if (updated) this.loadPhotos(updated);
        this.maintenanceBusy.set(false);
      },
      error: () => this.maintenanceBusy.set(false)
    });
  }

  /** El nombre del documento enlazado, para mostrar «Facture disponible ✓». */
  invoiceName(record: MaintenanceRecord): string | null {
    if (!record.documentId) return null;
    return this.documents().find(d => d.id === record.documentId)?.name ?? 'Facture';
  }

  /** «145.320 km» */
  km(value: number | null): string | null {
    return value === null ? null : `${value.toLocaleString('de-DE')} km`;
  }

  /** «240 Ko» */
  fileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} o`;
    if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} Ko`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} Mo`;
  }
}
