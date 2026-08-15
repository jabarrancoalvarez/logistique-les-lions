import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AdminService, AdminListingRow, AdminListingList, AdminListingDetail,
  AdminListingFilters, AdminListingAction, AdminAccountType, AdminActionEntry,
  ADMIN_ACTION_LABELS
} from '@core/services/admin.service';
import { VehicleStatus, STATUS_LABELS } from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/** Medidas disponibles, con el texto que ve el administrador. */
const ACTIONS: readonly { action: AdminListingAction; label: string; needsReason: boolean }[] = [
  { action: 'Hide',       label: 'Masquer',            needsReason: true },
  { action: 'Reactivate', label: 'Réafficher',         needsReason: false },
  { action: 'Flag',       label: 'Marquer à réviser',  needsReason: false },
  { action: 'Unflag',     label: 'Retirer la marque',  needsReason: false },
  { action: 'Archive',    label: 'Archiver',           needsReason: true },
  { action: 'Delete',     label: 'Supprimer',          needsReason: true }
];

/**
 * «Annonces» del backoffice.
 *
 * El administrador modera —masquer, réviser, archiver, supprimer— pero **no reescribe**
 * la información comercial: para eso pide la corrección a quien publica.
 */
@Component({
  selector: 'lll-admin-listings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, FcfaPipe],
  templateUrl: './admin-listings.component.html'
})
export class AdminListingsComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly data = signal<AdminListingList | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly statuses: VehicleStatus[] =
    ['Brouillon', 'Actif', 'EnPause', 'Reserve', 'Vendu', 'Archive'];
  readonly accountTypes: AdminAccountType[] = ['Particulier', 'Professionnel'];
  readonly actions = ACTIONS;

  filters: AdminListingFilters = { page: 1, pageSize: 20 };

  statusLabel = (s: string) => STATUS_LABELS[s as VehicleStatus] ?? s;
  actionLabel = (a: AdminActionEntry) => ADMIN_ACTION_LABELS[a.type];

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Brouillon: 'bg-navy/10 text-navy/70',
      Actif:     'bg-green-100 text-green-800',
      EnPause:   'bg-amber-100 text-amber-800',
      Reserve:   'bg-orange-100 text-orange-800',
      Vendu:     'bg-blue-100 text-blue-800',
      Archive:   'bg-navy/10 text-navy/50'
    };
    return map[status] ?? 'bg-navy/10 text-navy';
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.admin.getListings(this.filters).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); this.busy.set(false); },
      error: () => {
        this.error.set('Impossible de charger les annonces.');
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
  readonly detail = signal<AdminListingDetail | null>(null);
  readonly detailLoading = signal(false);

  openDetail(listing: AdminListingRow): void {
    if (this.detail()?.listing.id === listing.id) { this.closeDetail(); return; }

    this.detailLoading.set(true);
    this.detail.set(null);
    this.resetForms();

    this.admin.getListing(listing.id).subscribe({
      next: d => { this.detail.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); this.error.set('Fiche indisponible.'); }
    });
  }

  closeDetail(): void {
    this.detail.set(null);
    this.resetForms();
  }

  private refreshDetail(): void {
    const id = this.detail()?.listing.id;
    if (!id) { this.busy.set(false); return; }

    this.admin.getListing(id).subscribe({
      next: d => { this.detail.set(d); this.busy.set(false); },
      error: () => { this.closeDetail(); this.busy.set(false); }
    });
  }

  // ─── Modération ──────────────────────────────────────────────────────────
  selectedAction: AdminListingAction = 'Hide';
  actionReason = '';
  correctionMessage = '';
  noteBody = '';

  private resetForms(): void {
    this.selectedAction = 'Hide';
    this.actionReason = '';
    this.correctionMessage = '';
    this.noteBody = '';
  }

  get actionNeedsReason(): boolean {
    return ACTIONS.find(a => a.action === this.selectedAction)?.needsReason ?? false;
  }

  applyAction(): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    if (this.actionNeedsReason && !this.actionReason.trim()) {
      this.error.set('Indiquez le motif : il restera dans l\'historique.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.admin.applyListingAction(
      d.listing.id, this.selectedAction, this.actionReason.trim() || null
    ).subscribe({
      next: () => { this.actionReason = ''; this.refreshDetail(); this.load(); },
      error: () => { this.busy.set(false); this.error.set('Action impossible.'); }
    });
  }

  requestCorrection(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.correctionMessage.trim()) return;

    this.busy.set(true);
    this.admin.requestCorrection(d.listing.id, this.correctionMessage.trim()).subscribe({
      next: () => { this.correctionMessage = ''; this.refreshDetail(); },
      error: () => { this.busy.set(false); this.error.set('Envoi impossible.'); }
    });
  }

  addNote(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.noteBody.trim()) return;

    this.busy.set(true);
    this.admin.addNote('Listing', d.listing.id, this.noteBody.trim()).subscribe({
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
