import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AdminService, ReportRow, ReportList, ReportDetail, ReportStatus, ReportReason,
  ReportTargetType, AdminActionEntry,
  REPORT_REASON_LABELS, REPORT_STATUS_LABELS, REPORT_TARGET_LABELS, ADMIN_ACTION_LABELS
} from '@core/services/admin.service';

/**
 * «Modération» — la bandeja de signalements.
 *
 * Un mismo reporte puede señalar un anuncio, a una persona o una conversación: todos
 * llegan aquí. Cerrar uno exige explicar qué se ha decidido, y todo queda registrado.
 */
@Component({
  selector: 'lll-admin-moderation',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-moderation.component.html'
})
export class AdminModerationComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly data = signal<ReportList | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly statuses: ReportStatus[] = ['Nouveau', 'EnExamen', 'Resolu', 'Rejete'];
  readonly reasons: ReportReason[] = [
    'AnnonceSuspecte', 'InformationFausse', 'PrixTrompeur', 'PhotosIncorrectes',
    'VehiculeInexistant', 'TentativeDeFraude', 'ComportementInapproprie', 'Spam', 'Autre'
  ];
  readonly targetTypes: ReportTargetType[] = ['Listing', 'User', 'Negotiation'];

  filters: Record<string, unknown> = { page: 1, pageSize: 20 };

  statusLabel = (s: ReportStatus) => REPORT_STATUS_LABELS[s];
  reasonLabel = (r: ReportReason) => REPORT_REASON_LABELS[r];
  targetLabel = (t: ReportTargetType) => REPORT_TARGET_LABELS[t];
  actionLabel = (a: AdminActionEntry) => ADMIN_ACTION_LABELS[a.type];

  statusClass(status: ReportStatus): string {
    const map: Record<ReportStatus, string> = {
      Nouveau:  'bg-red-100 text-red-800',
      EnExamen: 'bg-amber-100 text-amber-800',
      Resolu:   'bg-green-100 text-green-800',
      Rejete:   'bg-navy/10 text-navy/50'
    };
    return map[status];
  }

  countFor(status: ReportStatus): number {
    return this.data()?.countByStatus?.[status] ?? 0;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.admin.getReports(this.filters).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); this.busy.set(false); },
      error: () => {
        this.error.set('Impossible de charger les signalements.');
        this.loading.set(false);
        this.busy.set(false);
      }
    });
  }

  filterByStatus(status: ReportStatus | null): void {
    this.filters['status'] = status ?? undefined;
    this.filters['page'] = 1;
    this.closeDetail();
    this.load();
  }

  search(): void {
    this.filters['page'] = 1;
    this.closeDetail();
    this.load();
  }

  resetFilters(): void {
    this.filters = { page: 1, pageSize: 20 };
    this.closeDetail();
    this.load();
  }

  // ─── Fiche ───────────────────────────────────────────────────────────────
  readonly detail = signal<ReportDetail | null>(null);
  readonly detailLoading = signal(false);

  openDetail(row: ReportRow): void {
    if (this.detail()?.report.id === row.id) { this.closeDetail(); return; }

    this.detailLoading.set(true);
    this.closeDetail();

    this.admin.getReport(row.id).subscribe({
      next: d => { this.detail.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); this.error.set('Fiche indisponible.'); }
    });
  }

  closeDetail(): void {
    this.detail.set(null);
    this.newStatus = 'EnExamen';
    this.resolution = '';
    this.warningMessage = '';
    this.infoMessage = '';
    this.noteBody = '';
  }

  private refreshDetail(): void {
    const id = this.detail()?.report.id;
    if (!id) { this.busy.set(false); return; }

    this.admin.getReport(id).subscribe({
      next: d => { this.detail.set(d); this.busy.set(false); },
      error: () => this.busy.set(false)
    });
  }

  // ─── Traitement ──────────────────────────────────────────────────────────
  newStatus: ReportStatus = 'EnExamen';
  resolution = '';
  warningMessage = '';
  infoMessage = '';
  noteBody = '';

  /** Cerrar un signalement exige explicar la decisión. */
  get needsResolution(): boolean {
    return this.newStatus === 'Resolu' || this.newStatus === 'Rejete';
  }

  applyStatus(): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    if (this.needsResolution && !this.resolution.trim()) {
      this.error.set('Expliquez ce qui a été décidé : c\'est ce qui restera dans l\'historique.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.admin.changeReportStatus(d.report.id, this.newStatus, this.resolution.trim() || null)
      .subscribe({
        next: () => { this.resolution = ''; this.refreshDetail(); this.load(); },
        error: () => { this.busy.set(false); this.error.set('Action impossible.'); }
      });
  }

  warn(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.warningMessage.trim()) return;

    this.busy.set(true);
    this.admin.warnReportedUser(d.report.id, this.warningMessage.trim()).subscribe({
      next: () => { this.warningMessage = ''; this.refreshDetail(); },
      error: () => { this.busy.set(false); this.error.set('Avertissement non envoyé.'); }
    });
  }

  requestInfo(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.infoMessage.trim()) return;

    this.busy.set(true);
    this.admin.requestReportInfo(d.report.id, this.infoMessage.trim()).subscribe({
      next: () => { this.infoMessage = ''; this.refreshDetail(); this.load(); },
      error: () => { this.busy.set(false); this.error.set('Demande non envoyée.'); }
    });
  }

  addNote(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.noteBody.trim()) return;

    this.busy.set(true);
    this.admin.addNote('Report', d.report.id, this.noteBody.trim()).subscribe({
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
