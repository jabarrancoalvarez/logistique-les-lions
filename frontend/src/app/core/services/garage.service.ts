import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { FuelType, TransmissionType, BodyType, VehicleStatus } from './vehicle.service';

/**
 * Ficha de un vehículo de Mon Garage.
 *
 * Casi todo es opcional: la especificación permite crear el vehículo con lo mínimo y
 * completar la ficha más tarde.
 */
export interface GarageVehicleForm {
  makeId: string;
  modelId: string | null;
  version: string | null;
  year: number;
  mileage: number | null;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  bodyType: BodyType | null;
  powerCv: number | null;
  engineDisplacementCc: number | null;
  color: string | null;
  registrationPlate: string | null;
  vin: string | null;
  purchaseDate: string | null;
  purchasePrice: number | null;
}

export interface GarageVehicleCard {
  id: string;
  title: string;
  year: number;
  mileage: number | null;
  color: string | null;
  registrationPlate: string | null;
  /** Identificador de la foto principal: la imagen se pide autenticada, no por URL. */
  primaryImageId: string | null;
  /** Comprado dentro de Yoon u Auto con contrato validado. */
  boughtOnYoonUAuto: boolean;
  purchaseDate: string | null;
  /** «Prochain rappel: Vidange · 2.500 km». */
  nextReminder: GarageCardReminder | null;
  /** `null` si no hay comparables suficientes: no se inventa ninguna cifra. */
  estimatedValue: number | null;
  /** «Dossier 82 %» — lo completo que está el historial, nunca el estado mecánico. */
  completenessScore: number;
}

export interface GarageCardReminder {
  id: string;
  type: ReminderType;
  label: string;
  status: ReminderStatus;
  dueDate: string | null;
  dueMileage: number | null;
  daysRemaining: number | null;
  mileageRemaining: number | null;
}

export interface Garage {
  vehicleCount: number;
  /** «1 rappel à venir» del resumen. */
  openReminderCount: number;
  /** «Valeur estimée totale». Solo suma los vehículos que sí tienen estimación. */
  totalEstimatedValue: number | null;
  vehicles: GarageVehicleCard[];
}

/** La fotografía no trae URL: es privada y se descarga por endpoint autenticado. */
export interface GarageVehicleImage {
  id: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface GarageVehicleDetail extends GarageVehicleForm {
  id: string;
  title: string;
  makeName: string;
  modelName: string | null;
  boughtOnYoonUAuto: boolean;
  /** Anuncio del que salió, si se compró en Yoon u Auto. */
  sourceVehicleId: string | null;
  /** Anuncio creado desde «Vendre ce véhicule», si está a la venta. */
  listedVehicleId: string | null;
  listedVehicleSlug: string | null;
  listedVehicleStatus: VehicleStatus | null;
  images: GarageVehicleImage[];
  createdAt: string;
  updatedAt: string;
}

/** «Ajouter ce véhicule à Mon Garage» tras una venta verificada. */
export interface GaragePrefill {
  contractId: string;
  /** Ya está en el garaje: no debe ofrecerse añadirlo otra vez. */
  alreadyAdded: boolean;
  existingGarageVehicleId: string | null;
  vehicle: GarageVehicleForm;
  makeName: string;
  modelName: string | null;
}

// ─── Documents ─────────────────────────────────────────────────────────────
export type GarageDocumentType =
  | 'ContratDeVente' | 'CarteGrise' | 'Douane' | 'Assurance' | 'ControleTechnique'
  | 'FactureEntretien' | 'FactureReparation' | 'FactureAchat' | 'Autre';

export const GARAGE_DOCUMENT_LABELS: Record<GarageDocumentType, string> = {
  ContratDeVente:    'Contrat de vente',
  CarteGrise:        'Carte grise',
  Douane:            'Documents de douane',
  Assurance:         'Assurance',
  ControleTechnique: 'Contrôle technique',
  FactureEntretien:  'Facture d\'entretien',
  FactureReparation: 'Facture de réparation',
  FactureAchat:      'Facture d\'achat',
  Autre:             'Autre document'
};

/**
 * Documento del historial documental.
 *
 * No incluye la ruta del archivo a propósito: se descarga por un endpoint que comprueba
 * de quién es. La documentación de Mon Garage es privada.
 */
export interface GarageDocument {
  id: string;
  type: GarageDocumentType;
  name: string;
  documentDate: string | null;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  notes: string | null;
  uploadedAt: string;
}

// ─── Entretien ─────────────────────────────────────────────────────────────
export type MaintenanceType =
  | 'Vidange' | 'Filtres' | 'Pneus' | 'Freins' | 'Batterie' | 'Distribution'
  | 'Embrayage' | 'Suspension' | 'Climatisation' | 'ReparationMoteur'
  | 'RevisionGenerale' | 'Autre';

export const MAINTENANCE_LABELS: Record<MaintenanceType, string> = {
  Vidange:          'Vidange',
  Filtres:          'Filtres',
  Pneus:            'Pneus',
  Freins:           'Freins',
  Batterie:         'Batterie',
  Distribution:     'Distribution',
  Embrayage:        'Embrayage',
  Suspension:       'Suspension',
  Climatisation:    'Climatisation',
  ReparationMoteur: 'Réparation moteur',
  RevisionGenerale: 'Révision générale',
  Autre:            'Autre'
};

/** Campos del formulario de una intervención. */
export interface MaintenanceForm {
  type: MaintenanceType;
  performedAt: string;
  mileage: number | null;
  description: string;
  cost: number | null;
  workshop: string | null;
  notes: string | null;
  /** Factura ya subida a Documents, si la hay. */
  documentId: string | null;
}

/** Sin la ruta del archivo: la foto se sirve por un endpoint autenticado. */
export interface MaintenanceImage {
  id: string;
  fileName: string;
  sizeBytes: number;
}

export interface MaintenanceRecord extends MaintenanceForm {
  id: string;
  /** «Facture disponible ✓». */
  hasInvoice: boolean;
  images: MaintenanceImage[];
  createdAt: string;
  /** Última corrección. Igual a `createdAt` si nunca se ha tocado. */
  updatedAt: string;
}

export interface MaintenanceYear {
  year: number;
  records: MaintenanceRecord[];
}

export interface MaintenanceHistory {
  recordCount: number;
  totalCost: number;
  /** Kilometraje de la última intervención registrada. */
  lastMileage: number | null;
  years: MaintenanceYear[];
}

// ─── Rappels ───────────────────────────────────────────────────────────────
export type ReminderType =
  | 'Vidange' | 'Assurance' | 'Inspection' | 'Pneus' | 'Distribution'
  | 'Freins' | 'Revision' | 'Autre';

export type ReminderStatus = 'AVenir' | 'AFaire' | 'Termine' | 'Annule';

export const REMINDER_LABELS: Record<ReminderType, string> = {
  Vidange:      'Vidange',
  Assurance:    'Assurance',
  Inspection:   'Contrôle technique',
  Pneus:        'Pneus',
  Distribution: 'Distribution',
  Freins:       'Freins',
  Revision:     'Révision',
  Autre:        'Autre'
};

export const REMINDER_STATUS_LABELS: Record<ReminderStatus, string> = {
  AVenir:  'À venir',
  AFaire:  'À faire',
  Termine: 'Terminé',
  Annule:  'Annulé'
};

/** Al menos una de las dos condiciones es obligatoria. */
export interface ReminderForm {
  type: ReminderType;
  label: string;
  dueDate: string | null;
  dueMileage: number | null;
  notes: string | null;
}

export interface Reminder extends ReminderForm {
  id: string;
  garageVehicleId: string;
  status: ReminderStatus;
  daysRemaining: number | null;
  /** Según la última lectura declarada. Negativo si ya se ha pasado. */
  mileageRemaining: number | null;
  completedAt: string | null;
}

export interface UpcomingReminder {
  id: string;
  garageVehicleId: string;
  vehicleTitle: string;
  type: ReminderType;
  label: string;
  dueDate: string | null;
  dueMileage: number | null;
  status: ReminderStatus;
  daysRemaining: number | null;
  mileageRemaining: number | null;
}

// ─── Valeur estimée ────────────────────────────────────────────────────────
/**
 * Criterios con los que se ha construido la muestra. Coincide con el `[Flags]` del
 * backend: 1 marca/modelo/año · 2 kilometraje · 4 carburant/boîte · 8 región.
 */
export const VALUATION_CRITERIA = {
  MakeModelYear: 1,
  Mileage: 2,
  FuelAndTransmission: 4,
  Region: 8
} as const;

export interface ValuationPoint {
  capturedAt: string;
  estimatedValue: number;
  mileage: number | null;
}

export interface ValuationEvolution {
  points: ValuationPoint[];
  monthsCovered: number;
  changeAmount: number | null;
  changePercent: number | null;
}

/**
 * Estimación de valor.
 *
 * Con `hasEstimate: false` no llega ninguna cifra: la especificación prohíbe inventarse
 * una valoración cuando no hay comparables suficientes.
 */
export interface VehicleValuation {
  hasEstimate: boolean;
  estimatedValue: number | null;
  lowValue: number | null;
  highValue: number | null;
  comparableCount: number;
  criteria: number;
  evolution: ValuationEvolution | null;
}

// ─── Complétude du dossier ─────────────────────────────────────────────────
/**
 * ⚠️ Indicador de lo completo y actualizado que está el **historial digital** del
 * vehículo. Nunca un diagnóstico mecánico ni una certificación de su estado: Yoon u Auto
 * no dispone de información para afirmar nada sobre la mecánica.
 */
export type CompletenessCheck =
  | 'MainInformation' | 'MileageUpToDate' | 'Vin' | 'Photos'
  | 'Documents' | 'MaintenanceHistory' | 'Reminders' | 'MaintenanceInvoices';

export type CompletenessStatus = 'Missing' | 'Partial' | 'Complete';
export type CompletenessLevel = 'AComplete' | 'Correct' | 'TresBien' | 'Excellent';

export const COMPLETENESS_LEVEL_LABELS: Record<CompletenessLevel, string> = {
  AComplete: 'À compléter',
  Correct:   'Correct',
  TresBien:  'Très bien',
  Excellent: 'Excellent'
};

export interface CompletenessItem {
  check: CompletenessCheck;
  status: CompletenessStatus;
  points: number;
  maxPoints: number;
  /** Dato suelto que acompaña al texto: «4 entretiens enregistrés». */
  detail: number | null;
}

export interface Completeness {
  score: number;
  level: CompletenessLevel;
  items: CompletenessItem[];
}

// ─── Vendre ce véhicule ────────────────────────────────────────────────────
export interface CreateListingResult {
  vehicleId: string;
  slug: string;
  publicReference: string;
  /** Sugerencia para la pantalla: el anuncio se crea **sin** precio. */
  suggestedPrice: number | null;
  copiedImages: number;
}

/**
 * «Transparence du véhicule»: qué parte del historial se enseña en el anuncio.
 *
 * Todo empieza apagado. Nada del historial privado se publica sin marcarlo.
 */
export interface TransparencyRecord {
  maintenanceRecordId: string;
  type: MaintenanceType;
  performedAt: string;
  mileage: number | null;
  description: string;
  /** La intervención tiene factura que podría compartirse. */
  hasInvoice: boolean;
  shared: boolean;
  shareInvoice: boolean;
}

export interface TransparencySettings {
  vehicleId: string;
  showMaintenanceHistory: boolean;
  showMaintenanceDetails: boolean;
  showMileageEvolution: boolean;
  records: TransparencyRecord[];
}

/** Lo que ve quien mira el anuncio. */
export interface PublicMaintenance {
  type: MaintenanceType;
  description: string;
  performedAt: string | null;
  mileage: number | null;
  invoiceDocumentId: string | null;
}

export interface PublicTransparency {
  /** «7 entretiens enregistrés sur Yoon u Auto». */
  maintenanceCount: number;
  showDetails: boolean;
  records: PublicMaintenance[];
  mileageEvolution: { date: string; mileage: number }[];
}

@Injectable({ providedIn: 'root' })
export class GarageService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/garage`;

  getMyGarage(): Observable<Garage> {
    return this.http.get<Garage>(this.baseUrl);
  }

  getVehicle(id: string): Observable<GarageVehicleDetail> {
    return this.http.get<GarageVehicleDetail>(`${this.baseUrl}/${id}`);
  }

  create(form: GarageVehicleForm, sourceContractId: string | null = null): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, { ...form, sourceContractId });
  }

  update(id: string, form: GarageVehicleForm): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, { ...form, sourceContractId: null });
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Ficha ya rellena a partir de una compra realizada en Yoon u Auto. */
  getPrefillFromContract(contractId: string): Observable<GaragePrefill> {
    return this.http.get<GaragePrefill>(`${this.baseUrl}/from-contract/${contractId}`);
  }

  uploadImage(id: string, file: File, isPrimary = false, sortOrder = 0):
      Observable<{ id: string }> {
    const data = new FormData();
    data.append('file', file);
    data.append('isPrimary', String(isPrimary));
    data.append('sortOrder', String(sortOrder));
    return this.http.post<{ id: string }>(`${this.baseUrl}/${id}/images`, data);
  }

  /** Estas fotos nunca son públicas: llegan como blob por endpoint autenticado. */
  getImageFile(imageId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/images/${imageId}`, { responseType: 'blob' });
  }

  deleteImage(imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/images/${imageId}`);
  }

  // ─── Documents ───────────────────────────────────────────────────────────
  getDocuments(vehicleId: string): Observable<GarageDocument[]> {
    return this.http.get<GarageDocument[]>(`${this.baseUrl}/${vehicleId}/documents`);
  }

  uploadDocument(vehicleId: string, file: File, document: {
    type: GarageDocumentType;
    name: string;
    documentDate: string | null;
    notes: string | null;
  }): Observable<{ id: string }> {
    const data = new FormData();
    data.append('file', file);
    data.append('type', document.type);
    data.append('name', document.name);
    if (document.documentDate) data.append('documentDate', document.documentDate);
    if (document.notes) data.append('notes', document.notes);

    return this.http.post<{ id: string }>(`${this.baseUrl}/${vehicleId}/documents`, data);
  }

  updateDocument(documentId: string, document: {
    type: GarageDocumentType;
    name: string;
    documentDate: string | null;
    notes: string | null;
  }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/documents/${documentId}`, document);
  }

  deleteDocument(documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/documents/${documentId}`);
  }

  /** El archivo exige el token: llega como blob, nunca por una URL pública. */
  downloadDocument(documentId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/documents/${documentId}/file`, { responseType: 'blob' });
  }

  // ─── Entretien ───────────────────────────────────────────────────────────
  getMaintenance(vehicleId: string): Observable<MaintenanceHistory> {
    return this.http.get<MaintenanceHistory>(`${this.baseUrl}/${vehicleId}/maintenance`);
  }

  addMaintenance(vehicleId: string, record: MaintenanceForm): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/${vehicleId}/maintenance`, record);
  }

  updateMaintenance(recordId: string, record: MaintenanceForm): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/maintenance/${recordId}`, record);
  }

  deleteMaintenance(recordId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/maintenance/${recordId}`);
  }

  uploadMaintenanceImage(recordId: string, file: File): Observable<{ id: string }> {
    const data = new FormData();
    data.append('file', file);
    return this.http.post<{ id: string }>(`${this.baseUrl}/maintenance/${recordId}/images`, data);
  }

  /** Estas fotos nunca son públicas: llegan como blob por endpoint autenticado. */
  getMaintenanceImage(imageId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/maintenance/images/${imageId}`, { responseType: 'blob' });
  }

  deleteMaintenanceImage(imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/maintenance/images/${imageId}`);
  }

  // ─── Rappels ─────────────────────────────────────────────────────────────
  getReminders(vehicleId: string): Observable<Reminder[]> {
    return this.http.get<Reminder[]>(`${this.baseUrl}/${vehicleId}/reminders`);
  }

  /** Los pendientes de todos los vehículos, del más urgente al menos. */
  getUpcomingReminders(limit = 5): Observable<UpcomingReminder[]> {
    return this.http.get<UpcomingReminder[]>(`${this.baseUrl}/reminders`, { params: { limit } });
  }

  addReminder(vehicleId: string, reminder: ReminderForm): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/${vehicleId}/reminders`, reminder);
  }

  updateReminder(reminderId: string, reminder: ReminderForm): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/reminders/${reminderId}`, reminder);
  }

  /** «À faire» lo decide el sistema al vencer la condición, no el usuario. */
  setReminderStatus(reminderId: string, status: ReminderStatus): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/reminders/${reminderId}/status`, { status });
  }

  deleteReminder(reminderId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/reminders/${reminderId}`);
  }

  // ─── Valeur estimée ──────────────────────────────────────────────────────
  getValuation(vehicleId: string): Observable<VehicleValuation> {
    return this.http.get<VehicleValuation>(`${this.baseUrl}/${vehicleId}/valuation`);
  }

  // ─── Complétude du dossier ───────────────────────────────────────────────
  getCompleteness(vehicleId: string): Observable<Completeness> {
    return this.http.get<Completeness>(`${this.baseUrl}/${vehicleId}/completeness`);
  }

  // ─── Vendre ce véhicule ──────────────────────────────────────────────────
  /** Crea un **borrador** de anuncio. No publica nada. */
  sell(garageVehicleId: string): Observable<CreateListingResult> {
    return this.http.post<CreateListingResult>(`${this.baseUrl}/${garageVehicleId}/sell`, null);
  }

  getTransparency(listingId: string): Observable<TransparencySettings> {
    return this.http.get<TransparencySettings>(
      `${this.baseUrl}/listings/${listingId}/transparency`);
  }

  saveTransparency(listingId: string, settings: {
    showMaintenanceHistory: boolean;
    showMaintenanceDetails: boolean;
    showMileageEvolution: boolean;
    records: { maintenanceRecordId: string; shared: boolean; shareInvoice: boolean }[];
  }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/listings/${listingId}/transparency`, settings);
  }
}
