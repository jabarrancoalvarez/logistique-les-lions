import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AdminService, CommunicationList, CommunicationType, CommunicationAudience,
  AdminUserRow, COMMUNICATION_TYPE_LABELS, COMMUNICATION_AUDIENCE_LABELS
} from '@core/services/admin.service';
import { SENEGAL_REGIONS } from '@shared/data/senegal-geo';

/**
 * «Communications» del backoffice.
 *
 * Deliberadamente corto, como pide la especificación: avisos, mantenimiento,
 * información importante y soporte individual. No es una herramienta de marketing.
 */
@Component({
  selector: 'lll-admin-communications',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-communications.component.html'
})
export class AdminCommunicationsComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly history = signal<CommunicationList | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly sent = signal<{ recipientCount: number; emailsSent: number } | null>(null);

  readonly types: CommunicationType[] =
    ['AvisGeneral', 'Maintenance', 'InformationImportante', 'Support'];
  readonly audiences: CommunicationAudience[] =
    ['Tous', 'Particuliers', 'Professionnels', 'Individuel'];
  readonly regions = SENEGAL_REGIONS;

  typeLabel = (t: CommunicationType) => COMMUNICATION_TYPE_LABELS[t];
  audienceLabel = (a: CommunicationAudience) => COMMUNICATION_AUDIENCE_LABELS[a];

  form = {
    type: 'AvisGeneral' as CommunicationType,
    audience: 'Tous' as CommunicationAudience,
    targetUserId: null as string | null,
    region: null as string | null,
    title: '',
    body: '',
    sendByEmail: false
  };

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.admin.getCommunications().subscribe({
      next: d => { this.history.set(d); this.loading.set(false); this.busy.set(false); },
      error: () => {
        this.error.set('Impossible de charger l\'historique.');
        this.loading.set(false);
        this.busy.set(false);
      }
    });
  }

  // ─── Destinataire individuel ─────────────────────────────────────────────
  readonly userResults = signal<AdminUserRow[]>([]);
  readonly selectedUser = signal<AdminUserRow | null>(null);
  userSearch = '';

  searchUser(): void {
    const term = this.userSearch.trim();
    if (!term) { this.userResults.set([]); return; }

    this.admin.getUsers({ search: term, pageSize: 8 }).subscribe({
      next: d => this.userResults.set(d.items),
      error: () => this.userResults.set([])
    });
  }

  selectUser(user: AdminUserRow): void {
    this.selectedUser.set(user);
    this.form.targetUserId = user.id;
    this.userResults.set([]);
    this.userSearch = '';
  }

  clearUser(): void {
    this.selectedUser.set(null);
    this.form.targetUserId = null;
  }

  // ─── Envoi ───────────────────────────────────────────────────────────────
  send(): void {
    if (this.busy()) return;

    if (!this.form.title.trim() || !this.form.body.trim()) {
      this.error.set('Le titre et le message sont obligatoires.');
      return;
    }
    if (this.form.audience === 'Individuel' && !this.form.targetUserId) {
      this.error.set('Choisissez la personne à qui écrire.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.sent.set(null);

    this.admin.sendCommunication({
      ...this.form,
      title: this.form.title.trim(),
      body: this.form.body.trim(),
      // La región solo acota los envíos colectivos.
      region: this.form.audience === 'Individuel' ? null : this.form.region
    }).subscribe({
      next: result => {
        this.sent.set(result);
        this.form.title = '';
        this.form.body = '';
        this.clearUser();
        this.load();
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Envoi impossible. Vérifiez qu\'il y a des destinataires.');
      }
    });
  }
}
