import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { VehicleStatus } from './vehicle.service';

/** Pestañas de Mes négociations. */
export type NegotiationStatus = 'EnCours' | 'EnAttente' | 'Terminee';

export const NEGOTIATION_STATUS_LABELS: Record<NegotiationStatus, string> = {
  EnCours: 'En cours',
  EnAttente: 'En attente',
  Terminee: 'Terminée'
};

/** Hitos de la cronología única de la operación. */
export type NegotiationEventType =
  | 'ConversationStarted' | 'OfferMade' | 'CounterOffer' | 'OfferAccepted'
  | 'OfferRejected' | 'ContractCreated' | 'ContractChangeRequested'
  | 'ContractValidated' | 'SaleVerified';

export const NEGOTIATION_EVENT_LABELS: Record<NegotiationEventType, string> = {
  ConversationStarted:     'Conversation commencée',
  OfferMade:               'Offre',
  CounterOffer:            'Contre-offre',
  OfferAccepted:           'Offre acceptée',
  OfferRejected:           'Offre refusée',
  ContractCreated:         'Contrat créé',
  ContractChangeRequested: 'Modification demandée',
  ContractValidated:       'Contrat validé',
  SaleVerified:            'Vente vérifiée'
};

export interface NegotiationSummary {
  id: string;
  status: NegotiationStatus;
  /** El usuario es el interesado; si no, es quien publica. */
  isBuyer: boolean;

  vehicleId: string;
  vehicleSlug: string;
  vehicleTitle: string;
  vehiclePublicReference: string;
  vehiclePrice: number;
  vehicleStatus: VehicleStatus;
  vehicleThumbnailUrl: string | null;

  otherUserId: string;
  otherUserName: string;
  otherUserAvatarUrl: string | null;

  lastMessage: string | null;
  lastActivityAt: string | null;
  unreadCount: number;
}

export interface NegotiationEvent {
  id: string;
  type: NegotiationEventType;
  amount: number | null;
  byMe: boolean;
  createdAt: string;
}

export type OfferStatus = 'EnAttente' | 'Acceptee' | 'Refusee' | 'ContreOfferte' | 'Retiree';

export const OFFER_STATUS_LABELS: Record<OfferStatus, string> = {
  EnAttente:     'En attente',
  Acceptee:      'Acceptée',
  Refusee:       'Refusée',
  ContreOfferte: 'Contre-offerte',
  Retiree:       'Retirée'
};

export interface Offer {
  id: string;
  amount: number;
  /** Precio publicado cuando se hizo la oferta. */
  listedPrice: number;
  message: string | null;
  status: OfferStatus;
  /** La hizo el usuario que consulta. */
  byMe: boolean;
  /** Puede aceptarla, rechazarla o contraofertar. */
  canRespond: boolean;
  createdAt: string;
  respondedAt: string | null;
}

export interface NegotiationDetail {
  id: string;
  status: NegotiationStatus;
  isBuyer: boolean;

  vehicleId: string;
  vehicleSlug: string;
  vehicleTitle: string;
  vehiclePublicReference: string;
  vehiclePrice: number;
  vehicleStatus: VehicleStatus;
  vehicleThumbnailUrl: string | null;
  /** Un anuncio vendido deja de admitir ofertas y nuevos contactos. */
  acceptsNegotiation: boolean;

  otherUserId: string;
  otherUserName: string;
  otherUserAvatarUrl: string | null;

  createdAt: string;
  lastActivityAt: string | null;
  timeline: NegotiationEvent[];
  offers: Offer[];
  /** Oferta viva, si la hay. Es la única que admite respuesta. */
  pendingOffer: Offer | null;
}

// ─── Checklist privée d'inspection ─────────────────────────────────────────
export type InspectionItemType =
  | 'Moteur' | 'Carrosserie' | 'Pneus' | 'Interieur' | 'Climatisation'
  | 'Feux' | 'Freins' | 'Direction' | 'Documents' | 'Vin' | 'EssaiRoutier';

export type InspectionResult = 'Bon' | 'Moyen' | 'Mauvais';

export const INSPECTION_ITEM_LABELS: Record<InspectionItemType, string> = {
  Moteur:        'Moteur',
  Carrosserie:   'Carrosserie',
  Pneus:         'Pneus',
  Interieur:     'Intérieur',
  Climatisation: 'Climatisation',
  Feux:          'Feux',
  Freins:        'Freins',
  Direction:     'Direction',
  Documents:     'Documents',
  Vin:           'VIN',
  EssaiRoutier:  'Essai routier'
};

export const INSPECTION_RESULT_LABELS: Record<InspectionResult, string> = {
  Bon:     'Bon',
  Moyen:   'Moyen',
  Mauvais: 'Mauvais'
};

export interface InspectionItem {
  type: InspectionItemType;
  result: InspectionResult | null;
  notes: string | null;
}

export interface Inspection {
  /** `null` mientras el usuario no haya guardado nada. */
  id: string | null;
  visitedAt: string | null;
  observedMileage: number | null;
  notes: string | null;
  items: InspectionItem[];
  updatedAt: string | null;
}

// ─── Contrat de vente ──────────────────────────────────────────────────────
export type ContractStatus =
  | 'Brouillon' | 'AValider' | 'ModificationDemandee' | 'Valide' | 'Annule';

export const CONTRACT_STATUS_LABELS: Record<ContractStatus, string> = {
  Brouillon:            'Brouillon',
  AValider:             'À valider',
  ModificationDemandee: 'Modification demandée',
  Valide:               'Validé',
  Annule:               'Annulé'
};

/** Campos del formulario, comunes a crear y corregir. */
export interface ContractForm {
  agreedPrice: number;
  registrationPlate: string | null;
  sellerLegalName: string;
  sellerIdDocument: string | null;
  sellerAddress: string | null;
  buyerLegalName: string;
  buyerIdDocument: string | null;
  buyerAddress: string | null;
}

export interface Contract extends ContractForm {
  id: string;
  publicReference: string;
  status: ContractStatus;
  negotiationId: string;

  vehicleMake: string;
  vehicleModel: string | null;
  vehicleVersion: string | null;
  vehicleYear: number;
  vehicleMileage: number | null;
  vehicleVin: string | null;
  vehicleReference: string;

  saleDate: string;

  /** Lo redactó el usuario; la otra parte es quien valida. */
  createdByMe: boolean;
  canEdit: boolean;
  canSend: boolean;
  canValidate: boolean;
  canRequestChanges: boolean;
  canCancel: boolean;
  /** El contrato definitivo ya se puede descargar en PDF. */
  canDownloadPdf: boolean;

  /** Código del QR. Solo existe una vez validado. */
  verificationCode: string | null;
  changeRequestNotes: string | null;
  createdAt: string;
  sentAt: string | null;
  validatedAt: string | null;
  cancelledAt: string | null;
}

/** Datos que el formulario trae ya rellenos del anuncio y de los perfiles. */
export interface ContractPrefill {
  vehicleMake: string;
  vehicleModel: string | null;
  vehicleVersion: string | null;
  vehicleYear: number;
  vehicleMileage: number | null;
  vehicleVin: string | null;
  vehicleReference: string;
  /** Importe de la oferta aceptada; si no hay, el precio del anuncio. */
  suggestedPrice: number;
  sellerLegalName: string;
  buyerLegalName: string;
}

/**
 * Lo que devuelve la comprobación pública del QR: solo lo que ya figura en el papel
 * que quien escanea tiene delante. Sin documentos de identidad ni direcciones.
 */
export interface ContractVerification {
  publicReference: string;
  vehicleMake: string;
  vehicleModel: string | null;
  vehicleVersion: string | null;
  vehicleYear: number;
  vehicleVin: string | null;
  registrationPlate: string | null;
  vehicleReference: string;
  agreedPrice: number;
  saleDate: string;
  sellerLegalName: string;
  buyerLegalName: string;
  validatedAt: string;
}

export interface ContractTab {
  contract: Contract | null;
  prefill: ContractPrefill;
  canCreate: boolean;
  isSeller: boolean;
}

@Injectable({ providedIn: 'root' })
export class NegotiationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/negotiations`;

  getAll(status?: NegotiationStatus): Observable<NegotiationSummary[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<NegotiationSummary[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<NegotiationDetail> {
    return this.http.get<NegotiationDetail>(`${this.baseUrl}/${id}`);
  }

  /**
   * «Faire une offre» desde la ficha. El importe es opcional: sin él solo se inicia
   * la conversación.
   */
  makeOffer(vehicleId: string, amount: number | null, message: string | null):
      Observable<{ negotiationId: string; offerId: string | null }> {
    return this.http.post<{ negotiationId: string; offerId: string | null }>(
      `${this.baseUrl}/offers`, { vehicleId, amount, message });
  }

  counterOffer(negotiationId: string, amount: number, message: string | null): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.baseUrl}/${negotiationId}/offers`, { amount, message });
  }

  acceptOffer(offerId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/offers/${offerId}/accept`, null);
  }

  rejectOffer(offerId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/offers/${offerId}/reject`, null);
  }

  /** Checklist privada: la API solo devuelve la del propio usuario. */
  getInspection(negotiationId: string): Observable<Inspection> {
    return this.http.get<Inspection>(`${this.baseUrl}/${negotiationId}/inspection`);
  }

  saveInspection(negotiationId: string, inspection: {
    visitedAt: string | null;
    observedMileage: number | null;
    notes: string | null;
    items: InspectionItem[];
  }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${negotiationId}/inspection`, inspection);
  }

  // ─── Contrat de vente ────────────────────────────────────────────────────
  /** Devuelve el contrato si existe y, si no, los datos precargados del formulario. */
  getContract(negotiationId: string): Observable<ContractTab> {
    return this.http.get<ContractTab>(`${this.baseUrl}/${negotiationId}/contract`);
  }

  createContract(negotiationId: string, form: ContractForm): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/${negotiationId}/contract`, form);
  }

  updateContract(contractId: string, form: ContractForm): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/contracts/${contractId}`, form);
  }

  sendContract(contractId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/contracts/${contractId}/send`, null);
  }

  validateContract(contractId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/contracts/${contractId}/validate`, null);
  }

  requestContractChanges(contractId: string, notes: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/contracts/${contractId}/request-changes`, { notes });
  }

  cancelContract(contractId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/contracts/${contractId}/cancel`, null);
  }

  /** El PDF exige el token, así que se descarga como blob y no con un enlace directo. */
  downloadContractPdf(contractId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/contracts/${contractId}.pdf`, { responseType: 'blob' });
  }

  /** Comprobación pública del QR: no requiere cuenta. */
  verifyContract(code: string): Observable<ContractVerification> {
    return this.http.get<ContractVerification>(
      `${environment.apiUrl}/v1/public/contracts/${encodeURIComponent(code)}`);
  }
}
