import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  AdminService, AdminNegotiationRow, AdminNegotiationList, AdminNegotiationDetail,
  AdminMessage, AdminContractRow, AdminContractList, AdminContractDetail,
  AdminNegotiationStatus, AdminContractStatus, ContentAccessReason, AdminActionEntry,
  CONTENT_ACCESS_LABELS, ADMIN_ACTION_LABELS
} from '@core/services/admin.service';
import { NEGOTIATION_STATUS_LABELS, CONTRACT_STATUS_LABELS } from '@core/services/negotiation.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/** Motivos que justifican leer una conversación privada. */
const ACCESS_REASONS: readonly ContentAccessReason[] =
  ['Report', 'Moderation', 'Dispute', 'FraudInvestigation', 'SupportRequested'];

/**
 * «Négociations» y «Contrats & ventes» del backoffice.
 *
 * Dos reglas gobiernan esta pantalla: el administrador ve la **estructura** de las
 * negociaciones pero no su contenido —salvo con motivo justificado, que queda
 * registrado— y **no puede validar** un contrato en nombre de nadie.
 */
@Component({
  selector: 'lll-admin-negotiations',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, FcfaPipe],
  templateUrl: './admin-negotiations.component.html'
})
export class AdminNegotiationsComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly route = inject(ActivatedRoute);

  /** Qué pestaña se muestra: la ruta lo decide. */
  readonly tab = signal<'negotiations' | 'contracts'>('negotiations');

  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly negotiations = signal<AdminNegotiationList | null>(null);
  readonly contracts = signal<AdminContractList | null>(null);

  readonly negotiationStatuses: AdminNegotiationStatus[] = ['EnCours', 'EnAttente', 'Terminee'];
  readonly contractStatuses: AdminContractStatus[] =
    ['Brouillon', 'AValider', 'ModificationDemandee', 'Valide', 'Annule'];
  readonly accessReasons = ACCESS_REASONS;

  filters: Record<string, unknown> = { page: 1, pageSize: 20 };

  negotiationStatusLabel = (s: AdminNegotiationStatus) => NEGOTIATION_STATUS_LABELS[s];
  contractStatusLabel = (s: AdminContractStatus) => CONTRACT_STATUS_LABELS[s];
  accessReasonLabel = (r: ContentAccessReason) => CONTENT_ACCESS_LABELS[r];
  actionLabel = (a: AdminActionEntry) => ADMIN_ACTION_LABELS[a.type];

  ngOnInit(): void {
    this.tab.set(this.route.snapshot.data['tab'] === 'contracts' ? 'contracts' : 'negotiations');
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    if (this.tab() === 'contracts') {
      this.admin.getContracts(this.filters).subscribe({
        next: d => { this.contracts.set(d); this.loading.set(false); this.busy.set(false); },
        error: () => this.failLoad()
      });
      return;
    }

    this.admin.getNegotiations(this.filters).subscribe({
      next: d => { this.negotiations.set(d); this.loading.set(false); this.busy.set(false); },
      error: () => this.failLoad()
    });
  }

  private failLoad(): void {
    this.error.set('Chargement impossible.');
    this.loading.set(false);
    this.busy.set(false);
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

  // ─── Fiche négociation ───────────────────────────────────────────────────
  readonly detail = signal<AdminNegotiationDetail | null>(null);
  readonly contractDetail = signal<AdminContractDetail | null>(null);
  readonly detailLoading = signal(false);

  openNegotiation(row: AdminNegotiationRow): void {
    if (this.detail()?.negotiation.id === row.id) { this.closeDetail(); return; }

    this.detailLoading.set(true);
    this.closeDetail();

    this.admin.getNegotiation(row.id).subscribe({
      next: d => { this.detail.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); this.error.set('Fiche indisponible.'); }
    });
  }

  openContract(row: AdminContractRow): void {
    if (this.contractDetail()?.contract.id === row.id) { this.closeDetail(); return; }

    this.detailLoading.set(true);
    this.closeDetail();

    this.admin.getContract(row.id).subscribe({
      next: d => { this.contractDetail.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); this.error.set('Fiche indisponible.'); }
    });
  }

  closeDetail(): void {
    this.detail.set(null);
    this.contractDetail.set(null);
    this.messages.set(null);
    this.accessDetails = '';
    this.invalidateReason = '';
  }

  // ─── Accès au contenu ────────────────────────────────────────────────────
  readonly messages = signal<AdminMessage[] | null>(null);
  accessReason: ContentAccessReason = 'Dispute';
  accessDetails = '';

  readContent(): void {
    const d = this.detail();
    if (!d || this.busy()) return;

    if (!this.accessDetails.trim()) {
      this.error.set('Expliquez pourquoi cette conversation doit être lue : cela restera enregistré.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.admin.accessNegotiationContent(d.negotiation.id, this.accessReason, this.accessDetails.trim())
      .subscribe({
        next: messages => {
          this.messages.set(messages);
          this.accessDetails = '';
          this.busy.set(false);
          // El acceso acaba de registrarse: la ficha debe reflejarlo.
          this.admin.getNegotiation(d.negotiation.id).subscribe({
            next: refreshed => this.detail.set(refreshed),
            error: () => { /* la lectura ya se ha hecho */ }
          });
        },
        error: () => { this.busy.set(false); this.error.set('Accès impossible.'); }
      });
  }

  // ─── Invalidation ────────────────────────────────────────────────────────
  invalidateReason = '';

  invalidate(): void {
    const d = this.contractDetail();
    if (!d || this.busy()) return;

    if (!this.invalidateReason.trim()) {
      this.error.set('Indiquez le motif de l\'invalidation.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.admin.invalidateContract(d.contract.id, this.invalidateReason.trim()).subscribe({
      next: () => {
        this.invalidateReason = '';
        this.admin.getContract(d.contract.id).subscribe({
          next: refreshed => { this.contractDetail.set(refreshed); this.busy.set(false); },
          error: () => this.busy.set(false)
        });
        this.load();
      },
      error: () => { this.busy.set(false); this.error.set('Invalidation impossible.'); }
    });
  }
}
