import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AdminService, AdminUserRow, AdminUserList, AdminUserDetail, AdminUserFilters,
  AccountStatus, AdminAccountType, AdminActionEntry,
  ACCOUNT_STATUS_LABELS, ADMIN_ACTION_LABELS,
  UserPoints, POINT_ORIGIN_LABELS, LoyaltyPointOrigin
} from '@core/services/admin.service';

/**
 * «Utilisateurs» del backoffice: buscar, consultar y gestionar cuentas.
 *
 * Restringir una cuenta exige siempre un motivo, que queda registrado: la especificación
 * no admite que un administrador toque información sensible sin dejar trazabilidad.
 */
@Component({
  selector: 'lll-admin-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html'
})
export class AdminUsersComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly data = signal<AdminUserList | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly statuses: AccountStatus[] = ['Active', 'Suspended', 'Blocked'];
  readonly accountTypes: AdminAccountType[] = ['Particulier', 'Professionnel'];

  filters: AdminUserFilters = { page: 1, pageSize: 20 };

  statusLabel = (s: AccountStatus) => ACCOUNT_STATUS_LABELS[s];
  actionLabel = (a: AdminActionEntry) => ADMIN_ACTION_LABELS[a.type];

  statusClass(status: AccountStatus): string {
    const map: Record<AccountStatus, string> = {
      Active:    'bg-green-100 text-green-800',
      Suspended: 'bg-amber-100 text-amber-800',
      Blocked:   'bg-red-100 text-red-800'
    };
    return map[status];
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.admin.getUsers(this.filters).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); this.busy.set(false); },
      error: () => {
        this.error.set('Impossible de charger les utilisateurs.');
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
    const d = this.data();
    if (!d) return;

    const next = (this.filters.page ?? 1) + delta;
    const lastPage = Math.max(1, Math.ceil(d.totalCount / d.pageSize));
    if (next < 1 || next > lastPage) return;

    this.filters.page = next;
    this.load();
  }

  get lastPage(): number {
    const d = this.data();
    return d ? Math.max(1, Math.ceil(d.totalCount / d.pageSize)) : 1;
  }

  // ─── Fiche ───────────────────────────────────────────────────────────────
  readonly detail = signal<AdminUserDetail | null>(null);
  readonly detailLoading = signal(false);

  openDetail(user: AdminUserRow): void {
    if (this.detail()?.profile.id === user.id) { this.closeDetail(); return; }

    this.detailLoading.set(true);
    this.detail.set(null);
    this.points.set(null);
    this.resetForms();

    this.admin.getUser(user.id).subscribe({
      next: d => { this.detail.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); this.error.set('Fiche indisponible.'); }
    });
  }

  closeDetail(): void {
    this.detail.set(null);
    this.points.set(null);
    this.resetForms();
  }

  // ─── Points de fidélité ──────────────────────────────────────────────────
  readonly points = signal<UserPoints | null>(null);
  pointsForm = { points: 0, reason: '' };

  originLabel = (o: LoyaltyPointOrigin) => POINT_ORIGIN_LABELS[o];

  loadPoints(userId: string): void {
    if (this.points()?.userId === userId) { this.points.set(null); return; }

    this.admin.getUserPoints(userId).subscribe({
      next: p => this.points.set(p),
      error: () => this.error.set('Solde de points indisponible.')
    });
  }

  adjustPoints(userId: string): void {
    if (this.busy()) return;

    // El motivo no es opcional: es lo que hace legible el movimiento dentro de un año.
    if (!this.pointsForm.reason.trim()) {
      this.error.set('Le motif est obligatoire pour ajuster des points.');
      return;
    }
    if (!this.pointsForm.points) {
      this.error.set('Indiquez combien de points ajouter ou retirer.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.admin.adjustUserPoints(userId, this.pointsForm.points, this.pointsForm.reason.trim())
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.pointsForm = { points: 0, reason: '' };
          this.admin.getUserPoints(userId).subscribe(p => this.points.set(p));
          this.refreshDetail();
        },
        error: () => {
          this.busy.set(false);
          this.error.set('Ajustement impossible.');
        }
      });
  }

  private refreshDetail(): void {
    const id = this.detail()?.profile.id;
    if (!id) return;

    this.admin.getUser(id).subscribe({
      next: d => { this.detail.set(d); this.busy.set(false); },
      error: () => this.busy.set(false)
    });
  }

  // ─── Actions sur le compte ───────────────────────────────────────────────
  statusForm = { status: 'Suspended' as AccountStatus, reason: '', suspendedUntil: '' };
  noteBody = '';

  private resetForms(): void {
    this.statusForm = { status: 'Suspended', reason: '', suspendedUntil: '' };
    this.noteBody = '';
  }

  applyStatus(): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    const restricting = this.statusForm.status !== 'Active';

    if (restricting && !this.statusForm.reason.trim()) {
      this.error.set('Indiquez le motif : il restera dans l\'historique.');
      return;
    }
    if (this.statusForm.status === 'Suspended' && !this.statusForm.suspendedUntil) {
      this.error.set('Indiquez la date de fin de la suspension.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.admin.changeAccountStatus(d.profile.id, {
      status: this.statusForm.status,
      reason: this.statusForm.reason.trim() || null,
      suspendedUntil: this.statusForm.status === 'Suspended'
        ? new Date(this.statusForm.suspendedUntil).toISOString()
        : null
    }).subscribe({
      next: () => {
        this.resetForms();
        this.refreshDetail();
        this.load();
      },
      error: () => { this.busy.set(false); this.error.set('Action impossible.'); }
    });
  }

  addNote(): void {
    const d = this.detail();
    if (!d || this.busy() || !this.noteBody.trim()) return;

    this.busy.set(true);
    this.admin.addNote('User', d.profile.id, this.noteBody.trim()).subscribe({
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
