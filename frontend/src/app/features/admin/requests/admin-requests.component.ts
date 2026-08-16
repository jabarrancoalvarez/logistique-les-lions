import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AdminService, AdminRequestRow, AdminRequestList, AdminRequestDetail,
  AdminRequestFilters, AdminRequestProposal, RequestStatus, AdminActionEntry,
  ExternalProposalPayload, REQUEST_STATUS_LABELS, ADMIN_ACTION_LABELS
} from '@core/services/admin.service';
import { VehicleService, VehicleListItem } from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/**
 * «Demandes de véhicules» del backoffice.
 *
 * Aquí el administrador presta un servicio: se hace cargo de la solicitud, busca coches
 * —dentro o fuera de Yoon u Auto— y responde a quien la creó.
 */
@Component({
  selector: 'lll-admin-requests',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, FcfaPipe],
  templateUrl: './admin-requests.component.html'
})
export class AdminRequestsComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly vehicles = inject(VehicleService);

  readonly data = signal<AdminRequestList | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly statuses: RequestStatus[] =
    ['NouvelleDemande', 'EnRecherche', 'VehiculePropose', 'Terminee', 'Annulee'];

  filters: AdminRequestFilters = { page: 1, pageSize: 20 };

  statusLabel = (s: RequestStatus) => REQUEST_STATUS_LABELS[s];
  actionLabel = (a: AdminActionEntry) => ADMIN_ACTION_LABELS[a.type];

  statusClass(status: RequestStatus): string {
    const map: Record<RequestStatus, string> = {
      NouvelleDemande: 'bg-azure/15 text-azure-dark',
      EnRecherche:     'bg-amber-100 text-amber-800',
      VehiculePropose: 'bg-green-100 text-green-800',
      Terminee:        'bg-navy/10 text-navy/60',
      Annulee:         'bg-red-100 text-red-800'
    };
    return map[status];
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.admin.getRequests(this.filters).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); this.busy.set(false); },
      error: () => {
        this.error.set('Impossible de charger les demandes.');
        this.loading.set(false);
        this.busy.set(false);
      }
    });
  }

  search(): void {
    this.filters.page = 1;
    this.closeDetail();
    this.load();
  }

  resetFilters(): void {
    this.filters = { page: 1, pageSize: 20 };
    this.closeDetail();
    this.load();
  }

  changePage(delta: number): void {
    const next = (this.filters.page ?? 1) + delta;
    if (next < 1 || next > this.lastPage) return;
    this.filters.page = next;
    this.load();
  }

  get lastPage(): number {
    const d = this.data();
    return d ? Math.max(1, Math.ceil(d.totalCount / d.pageSize)) : 1;
  }

  // ─── Fiche ───────────────────────────────────────────────────────────────
  readonly detail = signal<AdminRequestDetail | null>(null);
  readonly detailLoading = signal(false);

  openDetail(row: AdminRequestRow): void {
    if (this.detail()?.request.id === row.id) { this.closeDetail(); return; }

    this.detailLoading.set(true);
    this.detail.set(null);
    this.resetForms();

    this.admin.getRequest(row.id).subscribe({
      next: d => { this.detail.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); this.error.set('Fiche indisponible.'); }
    });
  }

  closeDetail(): void {
    this.detail.set(null);
    this.resetForms();
    this.searchResults.set([]);
  }

  private refreshDetail(): void {
    const id = this.detail()?.request.id;
    if (!id) { this.busy.set(false); return; }

    this.admin.getRequest(id).subscribe({
      next: d => { this.detail.set(d); this.busy.set(false); },
      error: () => { this.closeDetail(); this.busy.set(false); }
    });
  }

  // ─── Prise en charge et statut ───────────────────────────────────────────
  newStatus: RequestStatus = 'EnRecherche';
  statusReason = '';
  replyBody = '';
  noteBody = '';

  private resetForms(): void {
    this.newStatus = 'EnRecherche';
    this.statusReason = '';
    this.replyBody = '';
    this.noteBody = '';
    this.vehicleSearch = '';
    this.proposalComments = '';
    this.externalForm = this.emptyExternal();
    this.externalOpen.set(false);
  }

  toggleAssign(): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    this.busy.set(true);
    this.admin.assignRequest(d.request.id, d.request.assignedAdminId === null).subscribe({
      next: () => { this.refreshDetail(); this.load(); },
      error: () => { this.busy.set(false); this.error.set('Action impossible.'); }
    });
  }

  applyStatus(): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    if (this.newStatus === 'Annulee' && !this.statusReason.trim()) {
      this.error.set('Indiquez le motif de l\'annulation.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.admin.changeRequestStatus(d.request.id, this.newStatus, this.statusReason.trim() || null)
      .subscribe({
        next: () => { this.statusReason = ''; this.refreshDetail(); this.load(); },
        error: () => { this.busy.set(false); this.error.set('Changement de statut impossible.'); }
      });
  }

  // ─── Propositions ────────────────────────────────────────────────────────
  readonly searchResults = signal<VehicleListItem[]>([]);
  vehicleSearch = '';
  proposalComments = '';

  /** Busca anuncios publicados para anexarlos a la solicitud. */
  searchVehicles(): void {
    const term = this.vehicleSearch.trim();
    if (!term) { this.searchResults.set([]); return; }

    // Solo anuncios a la venta: como quien busca es administrador, el listado le
    // devolvía también los vendidos, los pausados y los ocultados, y se llegaban a
    // proponer coches que ya no estaban disponibles.
    this.vehicles.getVehicles({ search: term, status: 'Actif', pageSize: 8 }).subscribe({
      next: r => this.searchResults.set(r.items),
      error: () => this.searchResults.set([])
    });
  }

  attachVehicle(vehicle: VehicleListItem): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    this.busy.set(true);
    this.admin.addInternalProposal(d.request.id, vehicle.id, this.proposalComments.trim() || null)
      .subscribe({
        next: () => {
          this.vehicleSearch = '';
          this.proposalComments = '';
          this.searchResults.set([]);
          this.refreshDetail();
          this.load();
        },
        error: () => { this.busy.set(false); this.error.set('Proposition impossible.'); }
      });
  }

  readonly externalOpen = signal(false);
  externalForm = this.emptyExternal();
  photoUrlsText = '';

  private emptyExternal(): ExternalProposalPayload {
    this.photoUrlsText = '';
    return {
      makeModel: '', version: null, year: null, mileage: null,
      estimatedPrice: null, additionalCosts: null, countryOfOrigin: null,
      photoUrls: [], externalUrl: null, comments: null
    };
  }

  toggleExternal(): void {
    this.externalOpen.update(v => !v);
    if (!this.externalOpen()) this.externalForm = this.emptyExternal();
  }

  submitExternal(): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    if (!this.externalForm.makeModel.trim()) {
      this.error.set('Indiquez au moins la marque et le modèle.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    const payload: ExternalProposalPayload = {
      ...this.externalForm,
      makeModel: this.externalForm.makeModel.trim(),
      photoUrls: this.photoUrlsText
        .split('\n')
        .map(u => u.trim())
        .filter(u => u.length > 0)
    };

    this.admin.addExternalProposal(d.request.id, payload).subscribe({
      next: () => {
        this.externalForm = this.emptyExternal();
        this.externalOpen.set(false);
        this.refreshDetail();
        this.load();
      },
      error: () => { this.busy.set(false); this.error.set('Proposition impossible.'); }
    });
  }

  removeProposal(proposal: AdminRequestProposal): void {
    if (this.busy()) return;

    this.busy.set(true);
    this.admin.removeProposal(proposal.id).subscribe({
      next: () => this.refreshDetail(),
      error: () => { this.busy.set(false); this.error.set('Suppression impossible.'); }
    });
  }

  // ─── Communication et notes ──────────────────────────────────────────────
  reply(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.replyBody.trim()) return;

    this.busy.set(true);
    this.admin.replyToRequest(d.request.id, this.replyBody.trim()).subscribe({
      next: () => { this.replyBody = ''; this.refreshDetail(); },
      error: () => { this.busy.set(false); this.error.set('Envoi impossible.'); }
    });
  }

  addNote(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.noteBody.trim()) return;

    this.busy.set(true);
    this.admin.addNote('Request', d.request.id, this.noteBody.trim()).subscribe({
      next: () => { this.noteBody = ''; this.refreshDetail(); },
      error: () => { this.busy.set(false); this.error.set('Note non enregistrée.'); }
    });
  }

  deleteNote(noteId: string): void {
    if (this.busy()) return;

    this.busy.set(true);
    this.admin.deleteNote(noteId).subscribe({
      next: () => this.refreshDetail(),
      error: () => { this.busy.set(false); this.error.set('Suppression impossible.'); }
    });
  }
}
