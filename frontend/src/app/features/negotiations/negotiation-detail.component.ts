import {
  Component, ChangeDetectionStrategy, OnInit, OnDestroy, signal, computed, inject, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import {
  NegotiationService, NegotiationDetail, NegotiationEvent, Offer,
  Inspection, InspectionItem, InspectionResult,
  ContractTab, ContractForm, ContractStatus,
  NEGOTIATION_STATUS_LABELS, NEGOTIATION_EVENT_LABELS, OFFER_STATUS_LABELS,
  INSPECTION_ITEM_LABELS, INSPECTION_RESULT_LABELS, CONTRACT_STATUS_LABELS
} from '@core/services/negotiation.service';
import { MessagingService, MessageItem } from '@core/services/messaging.service';
import { AuthService } from '@core/auth/auth.service';
import { STATUS_LABELS } from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/** Pestañas de la negociación. */
type NegotiationTab = 'conversation' | 'inspection' | 'contract' | 'timeline';

/** Formulario vacío: el precargado llega del servidor. */
const EMPTY_CONTRACT_FORM: ContractForm = {
  agreedPrice: 0,
  registrationPlate: null,
  sellerLegalName: '',
  sellerIdDocument: null,
  sellerAddress: null,
  buyerLegalName: '',
  buyerIdDocument: null,
  buyerAddress: null
};

@Component({
  selector: 'lll-negotiation-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, FcfaPipe],
  templateUrl: './negotiation-detail.component.html'
})
export class NegotiationDetailComponent implements OnInit, OnDestroy {
  private readonly service = inject(NegotiationService);
  private readonly messaging = inject(MessagingService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly negotiation = signal<NegotiationDetail | null>(null);
  readonly messages = signal<MessageItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly activeTab = signal<NegotiationTab>('conversation');
  readonly sending = signal(false);
  messageBody = '';

  readonly myId = computed(() => this.auth.user()?.id ?? '');

  // ─── Tiempo real ─────────────────────────────────────────────────────────
  /** La otra parte está escribiendo ahora mismo. Se apaga sola. */
  readonly otherTyping = signal(false);

  /** Cuándo leyó la otra parte lo último que escribí. Null mientras no conste. */
  readonly lastReadAt = signal<string | null>(null);

  private typingTimer?: ReturnType<typeof setTimeout>;
  private lastTypingEmit = 0;

  constructor() {
    // Mensaje entrante: se recarga el hilo en vez de insertar el que llega, porque la
    // lista necesita el nombre del emisor y el estado de lectura, que el aviso no trae.
    effect(() => {
      const entrante = this.messaging.incomingMessage();
      const n = this.negotiation();
      if (!entrante || !n) return;
      if (entrante.senderId !== n.otherUserId || entrante.vehicleId !== n.vehicleId) return;

      this.loadMessages(n.id);
      // Se está mirando la conversación: avisar de que queda leído.
      void this.messaging.markAsRead(n.otherUserId, n.vehicleId);
    });

    effect(() => {
      const t = this.messaging.typingNotification();
      const n = this.negotiation();
      if (!t || !n) return;
      if (t.senderId !== n.otherUserId || t.vehicleId !== n.vehicleId) return;

      this.otherTyping.set(true);
      if (this.typingTimer) clearTimeout(this.typingTimer);
      // Solo llega el «empieza», nunca el «para»: se apaga por tiempo.
      this.typingTimer = setTimeout(() => this.otherTyping.set(false), 3000);
    });

    effect(() => {
      const r = this.messaging.readReceipt();
      const n = this.negotiation();
      if (!r || !n) return;
      if (r.readerId !== n.otherUserId || r.vehicleId !== n.vehicleId) return;
      this.lastReadAt.set(r.readAt);
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Négociation introuvable.');
      this.loading.set(false);
      return;
    }
    this.messaging.startConnection();
    this.load(id);
  }

  ngOnDestroy(): void {
    if (this.typingTimer) clearTimeout(this.typingTimer);
  }

  private load(id: string): void {
    this.loading.set(true);

    this.service.getById(id).subscribe({
      next: n => {
        this.negotiation.set(n);
        this.loading.set(false);
        this.loadMessages(n.id);
        // Abrir la negociación es haberla leído: se avisa a la otra parte.
        void this.messaging.markAsRead(n.otherUserId, n.vehicleId);
      },
      error: () => {
        this.error.set('Négociation introuvable.');
        this.loading.set(false);
      }
    });
  }

  private loadMessages(id: string): void {
    this.messaging.getMessages(id).subscribe({
      // La API los devuelve del más reciente al más antiguo.
      next: r => this.messages.set([...r.items].reverse()),
      error: () => this.messages.set([])
    });
  }

  selectTab(tab: NegotiationTab): void {
    this.activeTab.set(tab);
    if (tab === 'inspection') this.loadInspection();
    if (tab === 'contract') this.loadContract();
  }

  // ─── Conversation ──────────────────────────────────────────────────────
  sendMessage(): void {
    const n = this.negotiation();
    const body = this.messageBody.trim();
    if (!n || !body || this.sending() || !n.acceptsNegotiation) return;

    this.sending.set(true);
    this.messaging.sendMessageRest(n.otherUserId, n.vehicleId, body).subscribe({
      next: () => {
        this.sending.set(false);
        this.messageBody = '';
        // Lo nuevo aún no está leído: el acuse anterior ya no vale.
        this.lastReadAt.set(null);
        this.loadMessages(n.id);
      },
      error: () => this.sending.set(false)
    });
  }

  /**
   * Avisa de que se está escribiendo, como mucho una vez cada dos segundos: el evento se
   * emite en cada tecla y no hace falta repetirlo, porque al otro lado se apaga solo a
   * los tres segundos.
   */
  onTyping(): void {
    const n = this.negotiation();
    if (!n) return;

    const ahora = Date.now();
    if (ahora - this.lastTypingEmit < 2000) return;
    this.lastTypingEmit = ahora;

    void this.messaging.startTyping(n.otherUserId, n.vehicleId);
  }

  isMine(m: MessageItem): boolean {
    return m.senderId === this.myId();
  }

  // ─── Etiquetas ─────────────────────────────────────────────────────────
  readonly statusLabel = computed(() => {
    const n = this.negotiation();
    return n ? NEGOTIATION_STATUS_LABELS[n.status] : '';
  });

  readonly vehicleNotice = computed(() => {
    const n = this.negotiation();
    if (!n) return null;
    return n.vehicleStatus === 'Reserve' || n.vehicleStatus === 'Vendu'
      ? STATUS_LABELS[n.vehicleStatus]
      : null;
  });

  eventLabel(e: NegotiationEvent): string {
    return NEGOTIATION_EVENT_LABELS[e.type] ?? e.type;
  }

  /** «Offre acceptée» lo marca la otra parte o uno mismo; se distingue en el texto. */
  eventActor(e: NegotiationEvent): string {
    if (e.type === 'ConversationStarted') return '';
    return e.byMe ? 'Vous' : (this.negotiation()?.otherUserName ?? '');
  }

  // ─── Ofertas ───────────────────────────────────────────────────────────
  readonly responding = signal(false);
  readonly counterFormOpen = signal(false);
  readonly offerError = signal<string | null>(null);
  counterAmount = '';
  counterMessage = '';

  offerStatusLabel(o: Offer): string {
    return OFFER_STATUS_LABELS[o.status];
  }

  offerStatusClass(o: Offer): string {
    const map: Record<string, string> = {
      EnAttente:     'bg-amber-100 text-amber-800',
      Acceptee:      'bg-green-100 text-green-800',
      Refusee:       'bg-red-100 text-red-800',
      ContreOfferte: 'bg-navy/10 text-navy/60',
      Retiree:       'bg-navy/10 text-navy/60'
    };
    return map[o.status] ?? 'bg-navy/10 text-navy';
  }

  /** Diferencia respecto al precio publicado cuando se hizo la oferta. */
  offerGap(o: Offer): number | null {
    const gap = o.listedPrice - o.amount;
    return gap > 0 ? gap : null;
  }

  accept(o: Offer): void {
    if (this.responding()) return;
    this.responding.set(true);
    this.service.acceptOffer(o.id).subscribe({
      next: () => this.reload(),
      error: () => { this.responding.set(false); this.offerError.set('Action impossible.'); }
    });
  }

  reject(o: Offer): void {
    if (this.responding()) return;
    this.responding.set(true);
    this.service.rejectOffer(o.id).subscribe({
      next: () => this.reload(),
      error: () => { this.responding.set(false); this.offerError.set('Action impossible.'); }
    });
  }

  toggleCounterForm(): void {
    this.counterFormOpen.update(v => !v);
    this.offerError.set(null);
  }

  sendCounterOffer(): void {
    const n = this.negotiation();
    const amount = Number(this.counterAmount);
    if (!n || this.responding()) return;

    if (!Number.isFinite(amount) || amount <= 0) {
      this.offerError.set('Le montant doit être supérieur à 0.');
      return;
    }

    this.responding.set(true);
    this.service.counterOffer(n.id, amount, this.counterMessage.trim() || null).subscribe({
      next: () => {
        this.counterAmount = '';
        this.counterMessage = '';
        this.counterFormOpen.set(false);
        this.reload();
      },
      error: () => { this.responding.set(false); this.offerError.set('Action impossible.'); }
    });
  }

  // ─── Inspection privée ─────────────────────────────────────────────────
  readonly inspection = signal<Inspection | null>(null);
  readonly savingInspection = signal(false);
  readonly inspectionSaved = signal(false);

  readonly inspectionResults: InspectionResult[] = ['Bon', 'Moyen', 'Mauvais'];

  itemLabel(item: InspectionItem): string {
    return INSPECTION_ITEM_LABELS[item.type] ?? item.type;
  }

  resultLabel(result: InspectionResult): string {
    return INSPECTION_RESULT_LABELS[result];
  }

  /** Se carga la primera vez que se abre la pestaña. */
  private loadInspection(): void {
    const n = this.negotiation();
    if (!n || this.inspection()) return;

    this.service.getInspection(n.id).subscribe({
      next: i => this.inspection.set(i),
      error: () => this.inspection.set(null)
    });
  }

  setItemResult(item: InspectionItem, result: InspectionResult): void {
    // Volver a pulsar la misma valoración la retira.
    const next = item.result === result ? null : result;
    this.inspection.update(i => i && {
      ...i,
      items: i.items.map(x => (x.type === item.type ? { ...x, result: next } : x))
    });
  }

  setItemNotes(item: InspectionItem, notes: string): void {
    this.inspection.update(i => i && {
      ...i,
      items: i.items.map(x => (x.type === item.type ? { ...x, notes } : x))
    });
  }

  setInspectionNotes(notes: string): void {
    this.inspection.update(i => i && { ...i, notes });
  }

  setObservedMileage(value: string): void {
    const mileage = value ? Number(value) : null;
    this.inspection.update(i => i && {
      ...i,
      observedMileage: Number.isFinite(mileage as number) ? mileage : null
    });
  }

  saveInspection(): void {
    const n = this.negotiation();
    const i = this.inspection();
    if (!n || !i || this.savingInspection()) return;

    this.savingInspection.set(true);
    this.service.saveInspection(n.id, {
      visitedAt: i.visitedAt ?? new Date().toISOString(),
      observedMileage: i.observedMileage,
      notes: i.notes,
      items: i.items
    }).subscribe({
      next: () => {
        this.savingInspection.set(false);
        this.inspectionSaved.set(true);
        setTimeout(() => this.inspectionSaved.set(false), 3000);
      },
      error: () => this.savingInspection.set(false)
    });
  }

  // ─── Contrat de vente ──────────────────────────────────────────────────
  readonly contractTab = signal<ContractTab | null>(null);
  readonly contractLoading = signal(false);
  readonly contractBusy = signal(false);
  readonly contractError = signal<string | null>(null);
  /** El formulario está abierto: creando el contrato o corrigiéndolo. */
  readonly contractFormOpen = signal(false);
  readonly changeFormOpen = signal(false);

  contractForm: ContractForm = { ...EMPTY_CONTRACT_FORM };
  changeNotes = '';

  readonly contract = computed(() => this.contractTab()?.contract ?? null);

  contractStatusLabel(status: ContractStatus): string {
    return CONTRACT_STATUS_LABELS[status];
  }

  contractStatusClass(status: ContractStatus): string {
    const map: Record<ContractStatus, string> = {
      Brouillon:            'bg-navy/10 text-navy',
      AValider:             'bg-amber-100 text-amber-800',
      ModificationDemandee: 'bg-orange-100 text-orange-800',
      Valide:               'bg-green-100 text-green-800',
      Annule:               'bg-red-100 text-red-800'
    };
    return map[status];
  }

  private loadContract(force = false): void {
    const n = this.negotiation();
    if (!n || (this.contractTab() && !force)) return;

    this.contractLoading.set(true);
    this.service.getContract(n.id).subscribe({
      next: tab => {
        this.contractTab.set(tab);
        this.contractLoading.set(false);
        this.contractBusy.set(false);
      },
      error: () => {
        this.contractTab.set(null);
        this.contractLoading.set(false);
        this.contractBusy.set(false);
      }
    });
  }

  /** Abre el formulario: precargado del anuncio si es nuevo, del contrato si se corrige. */
  openContractForm(): void {
    const tab = this.contractTab();
    if (!tab) return;

    const c = tab.contract;
    this.contractForm = c
      ? {
          agreedPrice:       c.agreedPrice,
          registrationPlate: c.registrationPlate,
          sellerLegalName:   c.sellerLegalName,
          sellerIdDocument:  c.sellerIdDocument,
          sellerAddress:     c.sellerAddress,
          buyerLegalName:    c.buyerLegalName,
          buyerIdDocument:   c.buyerIdDocument,
          buyerAddress:      c.buyerAddress
        }
      : {
          ...EMPTY_CONTRACT_FORM,
          agreedPrice:     tab.prefill.suggestedPrice,
          sellerLegalName: tab.prefill.sellerLegalName,
          buyerLegalName:  tab.prefill.buyerLegalName
        };

    this.contractError.set(null);
    this.contractFormOpen.set(true);
  }

  closeContractForm(): void {
    this.contractFormOpen.set(false);
    this.contractError.set(null);
  }

  saveContract(): void {
    const n = this.negotiation();
    const existing = this.contract();
    if (!n || this.contractBusy()) return;

    const form = this.contractForm;
    if (!(form.agreedPrice > 0)) {
      this.contractError.set('Le prix convenu doit être supérieur à 0.');
      return;
    }
    if (!form.sellerLegalName.trim() || !form.buyerLegalName.trim()) {
      this.contractError.set('Les noms du vendeur et de l\'acheteur sont obligatoires.');
      return;
    }

    this.contractBusy.set(true);
    const request: Observable<unknown> = existing
      ? this.service.updateContract(existing.id, form)
      : this.service.createContract(n.id, form);

    request.subscribe({
      next: () => {
        this.contractFormOpen.set(false);
        this.loadContract(true);
      },
      error: () => {
        this.contractBusy.set(false);
        this.contractError.set('Action impossible.');
      }
    });
  }

  sendContract(): void {
    this.runContractAction(id => this.service.sendContract(id));
  }

  validateContract(): void {
    // Validar cierra la venta: también cambia el estado del anuncio y la negociación.
    this.runContractAction(id => this.service.validateContract(id), true);
  }

  cancelContract(): void {
    this.runContractAction(id => this.service.cancelContract(id));
  }

  toggleChangeForm(): void {
    this.changeFormOpen.update(v => !v);
    this.contractError.set(null);
  }

  requestChanges(): void {
    const notes = this.changeNotes.trim();
    if (!notes) {
      this.contractError.set('Indiquez ce qui doit être corrigé.');
      return;
    }
    this.runContractAction(id => this.service.requestContractChanges(id, notes));
    this.changeNotes = '';
    this.changeFormOpen.set(false);
  }

  readonly downloadingPdf = signal(false);

  /**
   * El PDF viaja con el token, así que llega como blob y se entrega al navegador
   * con un enlace temporal.
   */
  downloadPdf(): void {
    const c = this.contract();
    if (!c || this.downloadingPdf()) return;

    this.downloadingPdf.set(true);
    this.service.downloadContractPdf(c.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `contrat-${c.publicReference}.pdf`;
        link.click();
        URL.revokeObjectURL(url);
        this.downloadingPdf.set(false);
      },
      error: () => {
        this.downloadingPdf.set(false);
        this.contractError.set('Téléchargement impossible.');
      }
    });
  }

  private runContractAction(
    action: (contractId: string) => Observable<void>,
    reloadNegotiation = false
  ): void {
    const c = this.contract();
    if (!c || this.contractBusy()) return;

    this.contractBusy.set(true);
    this.contractError.set(null);

    action(c.id).subscribe({
      next: () => {
        this.loadContract(true);
        if (reloadNegotiation) this.reload();
      },
      error: () => {
        this.contractBusy.set(false);
        this.contractError.set('Action impossible.');
      }
    });
  }

  private reload(): void {
    const id = this.negotiation()?.id;
    if (!id) return;
    this.service.getById(id).subscribe({
      next: n => { this.negotiation.set(n); this.responding.set(false); },
      error: () => this.responding.set(false)
    });
  }
}
