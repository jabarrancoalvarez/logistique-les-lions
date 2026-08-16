import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@environments/environment';

export interface AdminStats {
  totalVehicles: number;
  activeListings: number;
  totalUsers: number;
  newUsersThisMonth: number;
  activeProcesses: number;
  completedProcesses: number;
  totalConversations: number;
  totalListingValue: number;
}

export interface VehicleAdminItem {
  id: string;
  title: string;
  slug: string;
  status: string;
  price: number;
  currency: string;
  sellerName: string;
  makeName: string;
  year: number;
  createdAt: string;
  expiresAt?: string;
}

export interface StatusBucket {
  status: string;
  count: number;
}

export interface MonthBucket {
  month: string;
  count: number;
}

export interface DashboardKpis {
  processesByStatus: StatusBucket[];
  vehiclesByStatus: StatusBucket[];
  processesPerMonth: MonthBucket[];
  averageLeadTimeDays: number;
  openIncidents: number;
  completedThisMonth: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─── Tableau de bord ───────────────────────────────────────────────────────
export interface AdminUserStats {
  total: number;
  newToday: number;
  newLast7Days: number;
  newLast30Days: number;
  particuliers: number;
  professionnels: number;
  phoneVerified: number;
}

export interface AdminMarketplaceStats {
  active: number;
  newLast7Days: number;
  newLast30Days: number;
  reserved: number;
  sold: number;
  drafts: number;
  paused: number;
  archived: number;
  /** Anuncios con signalements abiertos. */
  pendingModeration: number;
}

export interface AdminActivityStats {
  negotiationsStarted: number;
  negotiationsActive: number;
  messagesSent: number;
  offersMade: number;
  offersAccepted: number;
  contractsCreated: number;
  contractsValidated: number;
  verifiedSales: number;
}

export interface ModelDemand {
  label: string;
  count: number;
}

export interface AdminDemandStats {
  savedSearches: number;
  savedSearchesWithAlert: number;
  favoritesTotal: number;
  requestsPending: number;
  requestsSearching: number;
  topFavoritedModels: ModelDemand[];
  topRequestedModels: ModelDemand[];
}

export interface AdminGarageStats {
  vehiclesTotal: number;
  fromYoonUAuto: number;
  addedManually: number;
  convertedToListings: number;
}

export interface AdminDashboard {
  users: AdminUserStats;
  marketplace: AdminMarketplaceStats;
  activity: AdminActivityStats;
  demand: AdminDemandStats;
  garage: AdminGarageStats;
}

// ─── Gestion des utilisateurs ──────────────────────────────────────────────
export type AccountStatus = 'Active' | 'Suspended' | 'Blocked';
export type AdminAccountType = 'Particulier' | 'Professionnel';
export type AdminTargetType = 'User' | 'Listing' | 'Request' | 'Report';

export const ACCOUNT_STATUS_LABELS: Record<AccountStatus, string> = {
  Active:    'Active',
  Suspended: 'Suspendue',
  Blocked:   'Bloquée'
};

export interface AdminUserRow {
  id: string;
  displayName: string;
  phone: string;
  phoneVerified: boolean;
  email: string | null;
  city: string | null;
  accountType: AdminAccountType;
  status: AccountStatus;
  suspendedUntil: string | null;
  role: 'User' | 'Admin';
  listingsCount: number;
  verifiedSalesCount: number;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface AdminUserList {
  totalCount: number;
  page: number;
  pageSize: number;
  items: AdminUserRow[];
}

export interface AdminUserActivity {
  listingsPublished: number;
  listingsSold: number;
  negotiations: number;
  offersMade: number;
  contracts: number;
  verifiedSales: number;
  requests: number;
  /** Solo el número: el contenido de Mon Garage es privado. */
  garageVehicles: number;
  reportsReceived: number;
  reportsMade: number;
}

export type AdminActionType =
  | 'AccountActivated' | 'AccountSuspended' | 'AccountBlocked'
  | 'ListingHidden' | 'ListingReactivated' | 'ListingFlagged'
  | 'ListingArchived' | 'ListingDeleted' | 'ListingCorrectionRequested'
  | 'RequestAssigned' | 'RequestStatusChanged'
  | 'RequestProposalAdded' | 'RequestProposalRemoved'
  | 'NegotiationContentAccessed' | 'ContractInvalidated' | 'ContractDocumentAccessed'
  | 'ReportResolved' | 'UserWarned' | 'ReportInfoRequested' | 'ReportUnderReview'
  | 'PointsAdjusted' | 'SettingsChanged' | 'FeatureFlagToggled' | 'CatalogChanged';

export const ADMIN_ACTION_LABELS: Record<AdminActionType, string> = {
  AccountActivated:   'Compte réactivé',
  AccountSuspended:   'Compte suspendu',
  AccountBlocked:     'Compte bloqué',
  ListingHidden:      'Annonce masquée',
  ListingReactivated: 'Annonce réactivée',
  ListingFlagged:     'Annonce signalée pour révision',
  ListingArchived:    'Annonce archivée',
  ListingDeleted:     'Annonce supprimée',
  ListingCorrectionRequested: 'Correction demandée au vendeur',
  RequestAssigned:        'Prise en charge',
  RequestStatusChanged:   'Statut modifié',
  RequestProposalAdded:   'Véhicule proposé',
  RequestProposalRemoved: 'Proposition retirée',
  NegotiationContentAccessed: 'Conversation consultée',
  ContractInvalidated:        'Contrat invalidé',
  ContractDocumentAccessed:   'Document du contrat téléchargé',
  ReportResolved:             'Signalement clôturé',
  UserWarned:                 'Avertissement envoyé',
  ReportInfoRequested:        'Complément demandé',
  ReportUnderReview:          "Signalement mis à l'examen",
  PointsAdjusted:      'Points ajustés',
  SettingsChanged:     'Paramètres modifiés',
  FeatureFlagToggled:  'Fonctionnalité activée/désactivée',
  CatalogChanged:      'Catalogue modifié'
};

export interface AdminActionEntry {
  id: string;
  type: AdminActionType;
  reason: string | null;
  adminName: string;
  createdAt: string;
}

export interface AdminNote {
  id: string;
  body: string;
  adminName: string;
  createdAt: string;
}

export interface AdminUserDetail {
  profile: AdminUserRow;
  region: string | null;
  activity: AdminUserActivity;
  /** Append-only: nunca se modifica ni se borra. */
  actions: AdminActionEntry[];
  notes: AdminNote[];
}

export interface AdminUserFilters {
  search?: string;
  city?: string;
  accountType?: AdminAccountType;
  phoneVerified?: boolean;
  status?: AccountStatus;
  page?: number;
  pageSize?: number;
}

// ─── Gestion des annonces ──────────────────────────────────────────────────
export type AdminListingAction =
  'Hide' | 'Reactivate' | 'Flag' | 'Unflag' | 'Archive' | 'Delete';

export interface AdminListingRow {
  id: string;
  publicReference: string;
  slug: string;
  title: string;
  status: string;
  /** Ocultado por moderación: independiente del estado que eligió quien publica. */
  hiddenByAdmin: boolean;
  flaggedForReview: boolean;
  price: number;
  city: string | null;
  sellerId: string;
  sellerName: string;
  sellerAccountType: AdminAccountType;
  viewsCount: number;
  favoritesCount: number;
  qualityScore: number;
  /** Signalements abiertos sobre este anuncio. */
  openReports: number;
  createdAt: string;
  publishedAt: string | null;
}

export interface AdminListingList {
  totalCount: number;
  page: number;
  pageSize: number;
  items: AdminListingRow[];
}

export interface AdminPriceHistoryEntry {
  price: number;
  changedAt: string;
}

export interface AdminListingQualityItem {
  check: string;
  status: string;
  points: number;
  maxPoints: number;
  detail: number | null;
}

export interface AdminListingDetail {
  listing: AdminListingRow;
  updatedAt: string;
  sellerPhone: string;
  contactsCount: number;
  negotiationsCount: number;
  offersReceived: number;
  quality: { score: number; items: AdminListingQualityItem[] };
  priceHistory: AdminPriceHistoryEntry[];
  actions: AdminActionEntry[];
  notes: AdminNote[];
}

export interface AdminListingFilters {
  search?: string;
  sellerId?: string;
  city?: string;
  status?: string;
  sellerAccountType?: AdminAccountType;
  hidden?: boolean;
  flagged?: boolean;
  /** Con signalements abiertos. */
  reported?: boolean;
  page?: number;
  pageSize?: number;
}

// ─── Demandes de véhicules ─────────────────────────────────────────────────
export type RequestStatus =
  'NouvelleDemande' | 'EnRecherche' | 'VehiculePropose' | 'Terminee' | 'Annulee';

export const REQUEST_STATUS_LABELS: Record<RequestStatus, string> = {
  NouvelleDemande: 'Nouvelle demande',
  EnRecherche:     'En recherche',
  VehiculePropose: 'Véhicule proposé',
  Terminee:        'Terminée',
  Annulee:         'Annulée'
};

export interface AdminRequestRow {
  id: string;
  publicReference: string;
  userId: string;
  userName: string;
  userPhone: string;
  makeName: string;
  modelName: string | null;
  yearFrom: number | null;
  yearTo: number | null;
  maxMileage: number | null;
  maxBudget: number | null;
  origin: string;
  status: RequestStatus;
  assignedAdminId: string | null;
  assignedAdminName: string | null;
  proposalsCount: number;
  createdAt: string;
}

export interface AdminRequestList {
  totalCount: number;
  page: number;
  pageSize: number;
  items: AdminRequestRow[];
}

export interface AdminRequestProposal {
  id: string;
  isInternal: boolean;
  vehicleId: string | null;
  vehicleSlug: string | null;
  vehicleTitle: string | null;
  vehiclePrice: number | null;
  makeModel: string | null;
  version: string | null;
  year: number | null;
  mileage: number | null;
  fuelType: string | null;
  transmission: string | null;
  estimatedPrice: number | null;
  /** Transporte, aduana… Aparte del precio a propósito. */
  additionalCosts: number | null;
  countryOfOrigin: string | null;
  photoUrls: string[];
  externalUrl: string | null;
  comments: string | null;
  isSeenByUser: boolean;
  createdAt: string;
}

export interface AdminRequestDetail {
  request: AdminRequestRow;
  criteria: {
    version: string | null;
    fuelType: string | null;
    transmission: string | null;
    bodyType: string | null;
    color: string | null;
    importantEquipment: string | null;
    notes: string | null;
  };
  proposals: AdminRequestProposal[];
  messages: { id: string; body: string; fromAdmin: boolean; createdAt: string }[];
  actions: AdminActionEntry[];
  notes: AdminNote[];
}

export interface AdminRequestFilters {
  search?: string;
  status?: RequestStatus;
  unassigned?: boolean;
  page?: number;
  pageSize?: number;
}

/** Vehículo encontrado fuera de Yoon u Auto. */
export interface ExternalProposalPayload {
  makeModel: string;
  version: string | null;
  year: number | null;
  mileage: number | null;
  estimatedPrice: number | null;
  additionalCosts: number | null;
  countryOfOrigin: string | null;
  photoUrls: string[];
  externalUrl: string | null;
  comments: string | null;
}

// ─── Négociations ──────────────────────────────────────────────────────────
export type AdminNegotiationStatus = 'EnCours' | 'EnAttente' | 'Terminee';
export type AdminContractStatus =
  'Brouillon' | 'AValider' | 'ModificationDemandee' | 'Valide' | 'Annule';

/** Situaciones que justifican leer una conversación privada. */
export type ContentAccessReason =
  'Report' | 'Moderation' | 'Dispute' | 'FraudInvestigation' | 'SupportRequested';

export const CONTENT_ACCESS_LABELS: Record<ContentAccessReason, string> = {
  Report:             'Signalement',
  Moderation:         'Modération',
  Dispute:            'Litige entre les parties',
  FraudInvestigation: 'Enquête sur une fraude',
  SupportRequested:   'Support demandé par une partie'
};

export interface AdminNegotiationRow {
  id: string;
  vehicleId: string;
  vehicleReference: string;
  vehicleTitle: string;
  buyerId: string;
  buyerName: string;
  sellerId: string;
  sellerName: string;
  status: AdminNegotiationStatus;
  offersCount: number;
  /** Cuántos mensajes hay, no lo que dicen. */
  messagesCount: number;
  contractId: string | null;
  contractReference: string | null;
  contractStatus: AdminContractStatus | null;
  createdAt: string;
  lastActivityAt: string | null;
}

export interface AdminNegotiationList {
  totalCount: number;
  page: number;
  pageSize: number;
  items: AdminNegotiationRow[];
}

export interface AdminNegotiationDetail {
  negotiation: AdminNegotiationRow;
  offers: {
    id: string; amount: number; listedPrice: number; status: string;
    fromBuyer: boolean; createdAt: string;
  }[];
  timeline: { type: string; amount: number | null; createdAt: string }[];
  actions: AdminActionEntry[];
}

export interface AdminMessage {
  id: string;
  body: string;
  fromBuyer: boolean;
  createdAt: string;
}

// ─── Contrats & ventes ─────────────────────────────────────────────────────
export interface AdminContractRow {
  id: string;
  publicReference: string;
  negotiationId: string;
  vehicleId: string;
  vehicleReference: string;
  vehicleLabel: string;
  sellerId: string;
  sellerName: string;
  buyerId: string;
  buyerName: string;
  agreedPrice: number;
  status: AdminContractStatus;
  saleDate: string;
  createdAt: string;
  sentAt: string | null;
  validatedAt: string | null;
  cancelledAt: string | null;
  isVerifiedSale: boolean;
}

export interface AdminContractList {
  totalCount: number;
  page: number;
  pageSize: number;
  items: AdminContractRow[];
}

export interface AdminContractDetail {
  contract: AdminContractRow;
  vehicleModel: string | null;
  vehicleVersion: string | null;
  vehicleYear: number;
  vehicleMileage: number | null;
  vehicleVin: string | null;
  registrationPlate: string | null;
  sellerIdDocument: string | null;
  sellerAddress: string | null;
  buyerIdDocument: string | null;
  buyerAddress: string | null;
  verificationCode: string | null;
  changeRequestNotes: string | null;
  timeline: { type: string; amount: number | null; createdAt: string }[];
  actions: AdminActionEntry[];
  notes: AdminNote[];
}

// ─── Modération ────────────────────────────────────────────────────────────
export type ReportReason =
  | 'AnnonceSuspecte' | 'InformationFausse' | 'PrixTrompeur' | 'PhotosIncorrectes'
  | 'VehiculeInexistant' | 'TentativeDeFraude' | 'ComportementInapproprie'
  | 'Spam' | 'Autre';

export type ReportTargetType = 'Listing' | 'User' | 'Negotiation';
export type ReportStatus = 'Nouveau' | 'EnExamen' | 'Resolu' | 'Rejete';

export const REPORT_REASON_LABELS: Record<ReportReason, string> = {
  AnnonceSuspecte:        'Annonce suspecte',
  InformationFausse:      'Information fausse',
  PrixTrompeur:           'Prix trompeur',
  PhotosIncorrectes:      'Photographies incorrectes',
  VehiculeInexistant:     'Véhicule inexistant',
  TentativeDeFraude:      'Tentative de fraude',
  ComportementInapproprie: 'Comportement inapproprié',
  Spam:                   'Spam',
  Autre:                  'Autre motif'
};

export const REPORT_STATUS_LABELS: Record<ReportStatus, string> = {
  Nouveau:  'Nouveau',
  EnExamen: 'En examen',
  Resolu:   'Résolu',
  Rejete:   'Rejeté'
};

export const REPORT_TARGET_LABELS: Record<ReportTargetType, string> = {
  Listing:     'Annonce',
  User:        'Utilisateur',
  Negotiation: 'Négociation'
};

export interface ReportRow {
  id: string;
  publicReference: string;
  targetType: ReportTargetType;
  targetId: string;
  targetLabel: string;
  reporterId: string;
  reporterName: string;
  reportedUserId: string | null;
  reportedUserName: string | null;
  reason: ReportReason;
  description: string | null;
  status: ReportStatus;
  createdAt: string;
}

export interface ReportList {
  totalCount: number;
  page: number;
  pageSize: number;
  countByStatus: Partial<Record<ReportStatus, number>>;
  items: ReportRow[];
}

export interface ReportDetail {
  report: ReportRow;
  evidence: string[];
  resolution: string | null;
  resolvedAt: string | null;
  handledByAdminName: string | null;
  /** Otros signalements abiertos sobre lo mismo. */
  otherOpenReports: number;
  actions: AdminActionEntry[];
  notes: AdminNote[];
}

// ─── Notifications et communications ───────────────────────────────────────
export type CommunicationType =
  'AvisGeneral' | 'Maintenance' | 'InformationImportante' | 'Support';

export type CommunicationAudience =
  'Tous' | 'Particuliers' | 'Professionnels' | 'Individuel';

export const COMMUNICATION_TYPE_LABELS: Record<CommunicationType, string> = {
  AvisGeneral:           'Avis général',
  Maintenance:           'Maintenance programmée',
  InformationImportante: 'Information importante',
  Support:               'Support'
};

export const COMMUNICATION_AUDIENCE_LABELS: Record<CommunicationAudience, string> = {
  Tous:           'Tous les utilisateurs',
  Particuliers:   'Particuliers',
  Professionnels: 'Professionnels',
  Individuel:     'Une personne'
};

export interface CommunicationRow {
  id: string;
  type: CommunicationType;
  audience: CommunicationAudience;
  targetUserName: string | null;
  region: string | null;
  title: string;
  body: string;
  sentByEmail: boolean;
  recipientCount: number;
  /** Menor que el total: el correo es opcional en Yoon u Auto. */
  emailsSent: number;
  adminName: string;
  sentAt: string;
}

export interface CommunicationList {
  totalCount: number;
  page: number;
  pageSize: number;
  items: CommunicationRow[];
}

// ─── Statistiques ──────────────────────────────────────────────────────────

export interface LabelCount { label: string; count: number; }
export interface DayCount { day: string; count: number; }

export interface StatsUsers {
  total: number;
  newInPeriod: number;
  active: number;
  particuliers: number;
  professionnels: number;
  byRegion: LabelCount[];
  signupsPerDay: DayCount[];
}

export interface StatsSupply {
  publishedInPeriod: number;
  activeListings: number;
  averagePrice: number | null;
  medianPrice: number | null;
  medianMileage: number | null;
  medianYear: number | null;
  topMakes: LabelCount[];
  topModels: LabelCount[];
  byCity: LabelCount[];
  byFuel: LabelCount[];
  byCustomsStatus: LabelCount[];
}

/** Lo que se busca y no se encuentra: el dato con más valor del panel. */
export interface SupplyGap {
  label: string;
  searchingUsers: number;
  requests: number;
  availableListings: number;
}

export interface StatsDemand {
  savedSearches: number;
  favoritesTotal: number;
  requests: number;
  medianSearchBudget: number | null;
  topSearchedMakes: LabelCount[];
  topFavoritedModels: LabelCount[];
  topUsedFilters: LabelCount[];
  gaps: SupplyGap[];
}

export interface StatsFunnel {
  views: number;
  favorites: number;
  negotiations: number;
  offers: number;
  acceptedOffers: number;
  contracts: number;
  verifiedSales: number;
}

export interface Statistics {
  periodDays: number;
  users: StatsUsers;
  supply: StatsSupply;
  demand: StatsDemand;
  funnel: StatsFunnel;
}

// ─── Configuration ─────────────────────────────────────────────────────────

export interface PlatformSettings {
  comparatorMaxVehicles: number;
  pointsPerVerifiedSale: number;
  listingFreshnessDays: number;
  maxImagesPerListing: number;
  legalTermsVersion: string;
  legalTermsUpdatedAt: string | null;
}

export interface PriceIndicatorSettings {
  minComparables: number;
  maxListingAgeDays: number;
  yearBand: number;
  goodDealMargin: number;
  highPriceMargin: number;
}

export interface ValuationSettings {
  minComparables: number;
  maxListingAgeDays: number;
  yearBand: number;
  mileageBandKm: number;
  rangeSpread: number;
  snapshotIntervalDays: number;
}

export interface FeatureFlagRow {
  id: string;
  key: string;
  label: string;
  description: string | null;
  isEnabled: boolean;
}

export interface AdminSettings {
  platform: PlatformSettings;
  priceIndicator: PriceIndicatorSettings;
  valuation: ValuationSettings;
  flags: FeatureFlagRow[];
}

// ─── Catálogos ─────────────────────────────────────────────────────────────

export interface CatalogModel {
  id: string; name: string; category: string | null; listingsCount: number;
}

export interface CatalogMake {
  id: string;
  name: string;
  country: string | null;
  isPopular: boolean;
  modelsCount: number;
  listingsCount: number;
  models: CatalogModel[];
}

export interface CatalogEquipment {
  id: string; code: string; name: string;
  displayOrder: number; isActive: boolean; listingsCount: number;
}

export interface CatalogFeature {
  id: string; code: string; name: string; description: string | null;
  displayOrder: number; isActive: boolean; interestedCount: number;
}

export interface Catalogs {
  makes: CatalogMake[];
  equipments: CatalogEquipment[];
  upcomingFeatures: CatalogFeature[];
}

// ─── Points ────────────────────────────────────────────────────────────────

export type LoyaltyPointOrigin =
  'VenteVerifiee' | 'VenteInvalidee' | 'AjustementAdministrateur';

export const POINT_ORIGIN_LABELS: Record<LoyaltyPointOrigin, string> = {
  VenteVerifiee: 'Vente vérifiée',
  VenteInvalidee: 'Vente invalidée',
  AjustementAdministrateur: 'Ajustement administrateur'
};

export interface PointEntry {
  id: string;
  points: number;
  origin: LoyaltyPointOrigin;
  contractReference: string | null;
  adminName: string | null;
  note: string | null;
  at: string;
}

export interface UserPoints {
  userId: string;
  displayName: string;
  balance: number;
  verifiedSalesCount: number;
  entries: PointEntry[];
}

// ─── Journal d'activité ────────────────────────────────────────────────────

export interface ActivityLogRow {
  id: string;
  adminName: string;
  type: string;
  targetType: string;
  targetId: string;
  reason: string | null;
  oldValue: string | null;
  newValue: string | null;
  at: string;
}

export interface ActivityLog {
  totalCount: number;
  page: number;
  pageSize: number;
  items: ActivityLogRow[];
  admins: { id: string; name: string }[];
}

// ─── Intérêt pour les fonctionnalités à venir ──────────────────────────────

export interface FeatureInterestRow {
  id: string; code: string; name: string; isActive: boolean; interestedCount: number;
}

export interface FeatureSegmentation {
  featureId: string;
  featureName: string;
  total: number;
  particuliers: number;
  professionnels: number;
  byCity: { label: string; count: number }[];
  byActivity: { label: string; count: number }[];
}

export interface FeatureInterestReport {
  features: FeatureInterestRow[];
  segmentation: FeatureSegmentation | null;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly apiUrl = `${environment.apiUrl}/v1/admin`;

  constructor(private http: HttpClient) {}

  /** «Tableau de bord»: qué está ocurriendo hoy en la plataforma. */
  getDashboard(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${this.apiUrl}/dashboard`);
  }

  // ─── Gestion des utilisateurs ────────────────────────────────────────────
  getUsers(filters: AdminUserFilters = {}): Observable<AdminUserList> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<AdminUserList>(`${this.apiUrl}/users`, { params });
  }

  getUser(id: string): Observable<AdminUserDetail> {
    return this.http.get<AdminUserDetail>(`${this.apiUrl}/users/${id}`);
  }

  /** Restringir una cuenta exige motivo; suspender, además, fecha de final. */
  changeAccountStatus(id: string, body: {
    status: AccountStatus;
    reason: string | null;
    suspendedUntil: string | null;
  }): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/users/${id}/status`, body);
  }

  addNote(targetType: AdminTargetType, targetId: string, body: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.apiUrl}/notes`, { targetType, targetId, body });
  }

  deleteNote(noteId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/notes/${noteId}`);
  }

  // ─── Gestion des annonces ────────────────────────────────────────────────
  getListings(filters: AdminListingFilters = {}): Observable<AdminListingList> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<AdminListingList>(`${this.apiUrl}/listings`, { params });
  }

  getListing(id: string): Observable<AdminListingDetail> {
    return this.http.get<AdminListingDetail>(`${this.apiUrl}/listings/${id}`);
  }

  /** Moderar el anuncio. ⚠️ No modifica la información comercial. */
  applyListingAction(id: string, action: AdminListingAction, reason: string | null): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/listings/${id}/action`, { action, reason });
  }

  /** Pedir a quien publica que corrija su anuncio. */
  requestCorrection(id: string, message: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/listings/${id}/correction`, { message });
  }

  // ─── Demandes de véhicules ───────────────────────────────────────────────
  getRequests(filters: AdminRequestFilters = {}): Observable<AdminRequestList> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<AdminRequestList>(`${this.apiUrl}/requests`, { params });
  }

  getRequest(id: string): Observable<AdminRequestDetail> {
    return this.http.get<AdminRequestDetail>(`${this.apiUrl}/requests/${id}`);
  }

  assignRequest(id: string, assign: boolean): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/requests/${id}/assign`, { assign });
  }

  changeRequestStatus(id: string, status: RequestStatus, reason: string | null): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/requests/${id}/status`, { status, reason });
  }

  /** Anexar un anuncio de Yoon u Auto a la solicitud. */
  addInternalProposal(id: string, vehicleId: string, comments: string | null): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.apiUrl}/requests/${id}/proposals/internal`, { vehicleId, comments });
  }

  addExternalProposal(id: string, proposal: ExternalProposalPayload): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.apiUrl}/requests/${id}/proposals/external`, proposal);
  }

  removeProposal(proposalId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/requests/proposals/${proposalId}`);
  }

  replyToRequest(id: string, body: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/requests/${id}/reply`, { body });
  }

  // ─── Négociations ────────────────────────────────────────────────────────
  getNegotiations(filters: Record<string, unknown> = {}): Observable<AdminNegotiationList> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<AdminNegotiationList>(`${this.apiUrl}/negotiations`, { params });
  }

  getNegotiation(id: string): Observable<AdminNegotiationDetail> {
    return this.http.get<AdminNegotiationDetail>(`${this.apiUrl}/negotiations/${id}`);
  }

  /**
   * Única vía de leer una conversación privada. Exige motivo y **queda registrada**:
   * obtener los mensajes y dejar constancia ocurren en la misma operación.
   */
  accessNegotiationContent(
    id: string, reason: ContentAccessReason, details: string): Observable<AdminMessage[]> {
    return this.http.post<AdminMessage[]>(
      `${this.apiUrl}/negotiations/${id}/content`, { reason, details });
  }

  // ─── Contrats & ventes ───────────────────────────────────────────────────
  getContracts(filters: Record<string, unknown> = {}): Observable<AdminContractList> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<AdminContractList>(`${this.apiUrl}/contracts`, { params });
  }

  getContract(id: string): Observable<AdminContractDetail> {
    return this.http.get<AdminContractDetail>(`${this.apiUrl}/contracts/${id}`);
  }

  /** Lo único que se puede hacer a un contrato: la validación pertenece a las partes. */
  invalidateContract(id: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/contracts/${id}/invalidate`, { reason });
  }

  /**
   * Descarga el PDF del contrato. Va por POST porque **exige motivo**: el documento lleva
   * las pièces d'identité, las direcciones y los teléfonos de las dos partes, y la
   * descarga deja fila en `admin_actions` en la misma operación que la entrega.
   */
  downloadContractDocument(id: string, reason: string): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/contracts/${id}/document`, { reason },
      { responseType: 'blob' });
  }

  // ─── Modération ──────────────────────────────────────────────────────────
  getReports(filters: Record<string, unknown> = {}): Observable<ReportList> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<ReportList>(`${this.apiUrl}/reports`, { params });
  }

  getReport(id: string): Observable<ReportDetail> {
    return this.http.get<ReportDetail>(`${this.apiUrl}/reports/${id}`);
  }

  /** Cerrar un signalement exige explicar qué se ha decidido. */
  changeReportStatus(id: string, status: ReportStatus, resolution: string | null): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reports/${id}/status`, { status, resolution });
  }

  warnReportedUser(id: string, message: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reports/${id}/warn`, { message });
  }

  requestReportInfo(id: string, message: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reports/${id}/request-info`, { message });
  }

  // ─── Communications ──────────────────────────────────────────────────────
  getCommunications(page = 1, pageSize = 20): Observable<CommunicationList> {
    return this.http.get<CommunicationList>(`${this.apiUrl}/communications`,
      { params: { page, pageSize } });
  }

  sendCommunication(payload: {
    type: CommunicationType;
    audience: CommunicationAudience;
    targetUserId: string | null;
    region: string | null;
    title: string;
    body: string;
    sendByEmail: boolean;
  }): Observable<{ id: string; recipientCount: number; emailsSent: number }> {
    return this.http.post<{ id: string; recipientCount: number; emailsSent: number }>(
      `${this.apiUrl}/communications`, payload);
  }

  // ─── Points de fidélité ──────────────────────────────────────────────────
  getUserPoints(userId: string): Observable<UserPoints> {
    return this.http.get<UserPoints>(`${this.apiUrl}/users/${userId}/points`);
  }

  /** Un ajuste manual exige siempre motivo escrito. */
  adjustUserPoints(userId: string, points: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/users/${userId}/points`, { points, reason });
  }

  // ─── Configuration ───────────────────────────────────────────────────────
  getSettings(): Observable<AdminSettings> {
    return this.http.get<AdminSettings>(`${this.apiUrl}/settings`);
  }

  updateSettings(body: {
    platform: PlatformSettings;
    priceIndicator: PriceIndicatorSettings;
    valuation: ValuationSettings;
  }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/settings`, body);
  }

  toggleFeatureFlag(id: string, isEnabled: boolean): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/settings/flags/${id}`, { isEnabled });
  }

  // ─── Catálogos ───────────────────────────────────────────────────────────
  getCatalogs(): Observable<Catalogs> {
    return this.http.get<Catalogs>(`${this.apiUrl}/catalogs`);
  }

  /** `id` nulo crea; con valor, edita. */
  saveMake(body: {
    id: string | null; name: string; country: string | null; isPopular: boolean;
  }): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/catalogs/makes`, body);
  }

  saveModel(body: {
    id: string | null; makeId: string; name: string; category: string | null;
  }): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/catalogs/models`, body);
  }

  saveEquipment(body: {
    id: string | null; code: string; name: string;
    displayOrder: number; isActive: boolean;
  }): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/catalogs/equipments`, body);
  }

  saveUpcomingFeature(body: {
    id: string | null; code: string; name: string; description: string | null;
    displayOrder: number; isActive: boolean;
  }): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/catalogs/features`, body);
  }

  // ─── Intérêt pour les fonctionnalités à venir ────────────────────────────
  getFeatureInterest(featureId?: string | null): Observable<FeatureInterestReport> {
    let params = new HttpParams();
    if (featureId) params = params.set('featureId', featureId);

    return this.http.get<FeatureInterestReport>(`${this.apiUrl}/feature-interest`, { params });
  }

  // ─── Journal d'activité ──────────────────────────────────────────────────
  getActivityLog(filters: {
    adminId?: string | null;
    targetType?: string | null;
    type?: string | null;
    from?: string | null;
    to?: string | null;
    page?: number;
    pageSize?: number;
  } = {}): Observable<ActivityLog> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<ActivityLog>(`${this.apiUrl}/activity`, { params });
  }

  // ─── Statistiques ────────────────────────────────────────────────────────
  getStatistics(days = 30): Observable<Statistics> {
    return this.http.get<Statistics>(`${this.apiUrl}/statistics`, { params: { days } });
  }

  getStats(): Observable<AdminStats> {
    return this.http.get<{ isSuccess: boolean; value: AdminStats }>(`${this.apiUrl}/stats`).pipe(
      map(r => r.value)
    );
  }

  getVehicles(status?: string, page = 1, pageSize = 20): Observable<PagedResult<VehicleAdminItem>> {
    let url = `${this.apiUrl}/vehicles?page=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<{ isSuccess: boolean; value: PagedResult<VehicleAdminItem> }>(url).pipe(
      map(r => r.value)
    );
  }

  getDashboardKpis(): Observable<DashboardKpis> {
    return this.http.get<{ isSuccess: boolean; value: DashboardKpis }>(`${this.apiUrl}/dashboard/kpis`).pipe(
      map(r => r.value)
    );
  }

  approveVehicle(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/vehicles/${id}/approve`, {});
  }
}
