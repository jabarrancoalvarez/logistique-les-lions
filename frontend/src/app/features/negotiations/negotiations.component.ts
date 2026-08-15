import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  NegotiationService, NegotiationSummary, NegotiationStatus, NEGOTIATION_STATUS_LABELS
} from '@core/services/negotiation.service';
import { STATUS_LABELS } from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

@Component({
  selector: 'lll-negotiations',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, FcfaPipe],
  templateUrl: './negotiations.component.html'
})
export class NegotiationsComponent implements OnInit {
  private readonly service = inject(NegotiationService);

  readonly negotiations = signal<NegotiationSummary[]>([]);
  readonly loading = signal(true);
  readonly hasError = signal(false);

  /** Las tres pestañas de la especificación. */
  readonly tabs: { value: NegotiationStatus | null; label: string }[] = [
    { value: null,        label: 'Toutes' },
    { value: 'EnCours',   label: NEGOTIATION_STATUS_LABELS.EnCours },
    { value: 'EnAttente', label: NEGOTIATION_STATUS_LABELS.EnAttente },
    { value: 'Terminee',  label: NEGOTIATION_STATUS_LABELS.Terminee }
  ];

  readonly activeTab = signal<NegotiationStatus | null>(null);

  ngOnInit(): void {
    this.load();
  }

  selectTab(status: NegotiationStatus | null): void {
    this.activeTab.set(status);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.service.getAll(this.activeTab() ?? undefined).subscribe({
      next: items => {
        this.negotiations.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.hasError.set(true);
        this.loading.set(false);
      }
    });
  }

  statusLabel(n: NegotiationSummary): string {
    return NEGOTIATION_STATUS_LABELS[n.status];
  }

  statusClass(n: NegotiationSummary): string {
    const map: Record<NegotiationStatus, string> = {
      EnCours:   'bg-green-100 text-green-800',
      EnAttente: 'bg-amber-100 text-amber-800',
      Terminee:  'bg-navy/10 text-navy/60'
    };
    return map[n.status];
  }

  /** El anuncio puede haberse reservado o vendido durante la negociación. */
  vehicleNotice(n: NegotiationSummary): string | null {
    return n.vehicleStatus === 'Reserve' || n.vehicleStatus === 'Vendu'
      ? STATUS_LABELS[n.vehicleStatus]
      : null;
  }

  /** «Vous ↔ Auto Dakar» */
  parties(n: NegotiationSummary): string {
    return n.isBuyer ? `Vous ↔ ${n.otherUserName}` : `${n.otherUserName} ↔ Vous`;
  }

  timeAgo(iso: string | null): string {
    if (!iso) return '';
    const minutes = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
    if (minutes < 60) return `Il y a ${Math.max(minutes, 1)} min`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `Il y a ${hours} h`;
    const days = Math.floor(hours / 24);
    if (days === 1) return 'Hier';
    if (days < 30) return `Il y a ${days} jours`;
    return new Date(iso).toLocaleDateString('fr-FR');
  }
}
