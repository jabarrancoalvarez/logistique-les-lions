import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

// ─── DTOs ─────────────────────────────────────────────────────────────────────
export interface CountryRequirement {
  originCountry: string;
  destinationCountry: string;
  requiredDocuments: string[];
  homologationRequired: boolean;
  customsRatePercent: number;
  vatRatePercent: number;
  technicalInspectionRequired: boolean;
  estimatedProcessingCostEur: number;
  estimatedDays: number;
  notesEs: string | null;
  notesEn: string | null;
  lastUpdatedAt: string;
}

export interface CostLineItem {
  label: string;
  amountEur: number;
  description: string | null;
}

export interface CostEstimate {
  vehicleId: string;
  originCountry: string;
  destinationCountry: string;
  vehiclePriceEur: number;
  customsDutyEur: number;
  vatEur: number;
  processingFeesEur: number;
  homologationCostEur: number;
  totalEstimatedEur: number;
  customsRatePercent: number;
  vatRatePercent: number;
  homologationRequired: boolean;
  estimatedDays: number;
  lineItems: CostLineItem[];
}

export interface ProcessDocumentItem {
  id: string;
  documentType: string;
  status: string;
  responsibleParty: string;
  requiredByDate: string | null;
  fileUrl: string | null;
  templateUrl: string | null;
  officialUrl: string | null;
  instructionsEs: string | null;
  estimatedCostEur: number | null;
}

export interface ProcessStatus {
  id: string;
  vehicleId: string;
  vehicleTitle: string;
  originCountry: string;
  destinationCountry: string;
  processType: string;
  status: string;
  completionPercent: number;
  estimatedCostEur: number | null;
  actualCostEur: number | null;
  startedAt: string | null;
  completedAt: string | null;
  createdAt: string;
  documents: ProcessDocumentItem[];
}

export interface DocumentTemplate {
  id: string;
  country: string;
  documentType: string;
  templateUrl: string | null;
  instructionsEs: string | null;
  instructionsEn: string | null;
  officialUrl: string | null;
  issuingAuthority: string | null;
  estimatedCostEur: number | null;
  estimatedDays: number | null;
}

// ─── Service ──────────────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class ComplianceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/compliance`;

  getRequirements(origin: string, destination: string): Observable<CountryRequirement> {
    return this.http.get<CountryRequirement>(`${this.baseUrl}/requirements`, {
      params: { origin, destination }
    });
  }

  estimateCost(vehicleId: string, origin: string, destination: string): Observable<CostEstimate> {
    return this.http.get<CostEstimate>(`${this.baseUrl}/estimate`, {
      params: { vehicleId, origin, destination }
    });
  }

  getDocumentTemplates(country: string, documentType?: string): Observable<DocumentTemplate[]> {
    let params = new HttpParams().set('country', country);
    if (documentType) params = params.set('documentType', documentType);
    return this.http.get<DocumentTemplate[]>(`${this.baseUrl}/templates`, { params });
  }

  getProcessStatus(processId: string): Observable<ProcessStatus> {
    return this.http.get<ProcessStatus>(`${this.baseUrl}/processes/${processId}`);
  }

  initiateProcess(data: {
    vehicleId: string;
    buyerId: string;
    sellerId: string;
    originCountry: string;
    destinationCountry: string;
    processType: string;
  }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/processes`, data);
  }

  uploadDocument(processId: string, documentId: string, fileUrl: string): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/processes/${processId}/documents/${documentId}`,
      { processId, documentId, fileUrl }
    );
  }
}
