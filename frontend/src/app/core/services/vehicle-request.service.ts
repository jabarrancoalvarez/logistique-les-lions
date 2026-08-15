import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { FuelType, TransmissionType, BodyType } from './vehicle.service';

export type VehicleRequestStatus =
  | 'NouvelleDemande' | 'EnRecherche' | 'VehiculePropose' | 'Terminee' | 'Annulee';

export type VehicleRequestOrigin = 'Importation' | 'Senegal' | 'Indifferent';

export const REQUEST_STATUS_LABELS: Record<VehicleRequestStatus, string> = {
  NouvelleDemande: 'Nouvelle demande',
  EnRecherche: 'En recherche',
  VehiculePropose: 'Véhicule proposé',
  Terminee: 'Terminée',
  Annulee: 'Annulée'
};

export const REQUEST_STATUS_CLASSES: Record<VehicleRequestStatus, string> = {
  NouvelleDemande: 'bg-navy/10 text-navy',
  EnRecherche: 'bg-blue-100 text-blue-800',
  VehiculePropose: 'bg-green-100 text-green-800',
  Terminee: 'bg-navy/10 text-navy/60',
  Annulee: 'bg-navy/5 text-navy/40'
};

export const REQUEST_ORIGIN_LABELS: Record<VehicleRequestOrigin, string> = {
  Importation: 'Importation',
  Senegal: 'Sénégal',
  Indifferent: 'Indifférent'
};

export interface VehicleRequestSummary {
  id: string;
  publicReference: string;
  makeName: string;
  modelName: string | null;
  yearFrom: number | null;
  yearTo: number | null;
  maxMileage: number | null;
  maxBudget: number | null;
  origin: VehicleRequestOrigin;
  status: VehicleRequestStatus;
  /** Propuestas que el usuario aún no ha abierto. */
  unseenProposals: number;
  proposalsCount: number;
  createdAt: string;
}

export interface VehicleRequestMessage {
  id: string;
  isFromAdmin: boolean;
  body: string;
  createdAt: string;
}

export interface VehicleRequestProposal {
  id: string;
  /** La propuesta es un anuncio publicado en Yoon u Auto. */
  isInternal: boolean;
  vehicleSlug: string | null;
  vehicleTitle: string | null;
  vehiclePrice: number | null;
  makeModel: string | null;
  year: number | null;
  mileage: number | null;
  estimatedPrice: number | null;
  countryOfOrigin: string | null;
  photoUrls: string[];
  externalUrl: string | null;
  comments: string | null;
  createdAt: string;
}

export interface VehicleRequestDetail {
  id: string;
  publicReference: string;
  status: VehicleRequestStatus;
  canBeCancelled: boolean;

  makeName: string;
  modelName: string | null;
  version: string | null;
  yearFrom: number | null;
  yearTo: number | null;
  maxMileage: number | null;
  fuelType: FuelType | null;
  transmission: TransmissionType | null;
  bodyType: BodyType | null;
  color: string | null;
  importantEquipment: string | null;
  maxBudget: number | null;
  origin: VehicleRequestOrigin;
  notes: string | null;

  createdAt: string;
  messages: VehicleRequestMessage[];
  proposals: VehicleRequestProposal[];
}

export interface CreateVehicleRequestPayload {
  makeId?: string;
  makeName: string;
  modelName?: string;
  version?: string;
  yearFrom?: number;
  yearTo?: number;
  maxMileage?: number;
  fuelType?: FuelType;
  transmission?: TransmissionType;
  bodyType?: BodyType;
  color?: string;
  importantEquipment?: string;
  maxBudget?: number;
  origin: VehicleRequestOrigin;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class VehicleRequestService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/vehicle-requests`;

  getAll(): Observable<VehicleRequestSummary[]> {
    return this.http.get<VehicleRequestSummary[]>(this.baseUrl);
  }

  getById(id: string): Observable<VehicleRequestDetail> {
    return this.http.get<VehicleRequestDetail>(`${this.baseUrl}/${id}`);
  }

  create(payload: CreateVehicleRequestPayload): Observable<{ id: string; publicReference: string }> {
    return this.http.post<{ id: string; publicReference: string }>(this.baseUrl, payload);
  }

  addMessage(id: string, body: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/messages`, { body });
  }

  markProposalsSeen(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/proposals/seen`, null);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, null);
  }
}
