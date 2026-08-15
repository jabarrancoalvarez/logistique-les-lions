import { Component, ChangeDetectionStrategy, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  VehicleRequestService, VehicleRequestDetail,
  REQUEST_STATUS_LABELS, REQUEST_STATUS_CLASSES, REQUEST_ORIGIN_LABELS
} from '@core/services/vehicle-request.service';
import { FUEL_LABELS, TRANSMISSION_LABELS, BODY_LABELS } from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

interface Criterion {
  label: string;
  value: string;
}

@Component({
  selector: 'lll-vehicle-request-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, FcfaPipe],
  templateUrl: './vehicle-request-detail.component.html'
})
export class VehicleRequestDetailComponent implements OnInit {
  private readonly service = inject(VehicleRequestService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fcfa = new FcfaPipe();

  readonly request = signal<VehicleRequestDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly sending = signal(false);
  messageBody = '';

  /** La cancelación pide confirmación antes de ejecutarse. */
  readonly confirmingCancel = signal(false);
  readonly cancelling = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Demande introuvable.');
      this.loading.set(false);
      return;
    }
    this.load(id);
  }

  private load(id: string): void {
    this.loading.set(true);
    this.service.getById(id).subscribe({
      next: r => {
        this.request.set(r);
        this.loading.set(false);
        // Al abrir la solicitud, sus propuestas dejan de figurar como nuevas.
        if (r.proposals.length > 0) {
          this.service.markProposalsSeen(id).subscribe({ error: () => {} });
        }
      },
      error: () => {
        this.error.set('Demande introuvable.');
        this.loading.set(false);
      }
    });
  }

  // ─── Etiquetas ─────────────────────────────────────────────────────────
  readonly statusLabel = computed(() => {
    const r = this.request();
    return r ? REQUEST_STATUS_LABELS[r.status] : '';
  });

  readonly statusClass = computed(() => {
    const r = this.request();
    return r ? REQUEST_STATUS_CLASSES[r.status] : '';
  });

  readonly vehicleName = computed(() => {
    const r = this.request();
    return r ? [r.makeName, r.modelName, r.version].filter(Boolean).join(' ') : '';
  });

  /** Criterios declarados. Los no rellenados simplemente no aparecen. */
  readonly criteria = computed<Criterion[]>(() => {
    const r = this.request();
    if (!r) return [];

    const list: Criterion[] = [];

    if (r.yearFrom && r.yearTo) list.push({ label: 'Années', value: `${r.yearFrom}–${r.yearTo}` });
    else if (r.yearFrom) list.push({ label: 'Année min.', value: `${r.yearFrom}` });
    else if (r.yearTo) list.push({ label: 'Année max.', value: `${r.yearTo}` });

    if (r.maxMileage !== null) {
      list.push({ label: 'Kilométrage max.', value: `${r.maxMileage.toLocaleString('fr-FR')} km` });
    }
    if (r.fuelType)     list.push({ label: 'Carburant', value: FUEL_LABELS[r.fuelType] });
    if (r.transmission) list.push({ label: 'Boîte', value: TRANSMISSION_LABELS[r.transmission] });
    if (r.bodyType)     list.push({ label: 'Carrosserie', value: BODY_LABELS[r.bodyType] });
    if (r.color)        list.push({ label: 'Couleur', value: r.color });
    if (r.importantEquipment) {
      list.push({ label: 'Équipements', value: r.importantEquipment });
    }
    if (r.maxBudget !== null) {
      list.push({ label: 'Budget max.', value: this.fcfa.transform(r.maxBudget) });
    }
    list.push({ label: 'Provenance', value: REQUEST_ORIGIN_LABELS[r.origin] });

    return list;
  });

  // ─── Hilo con Yoon u Auto ──────────────────────────────────────────────
  sendMessage(): void {
    const r = this.request();
    const body = this.messageBody.trim();
    if (!r || !body || this.sending()) return;

    this.sending.set(true);
    this.service.addMessage(r.id, body).subscribe({
      next: () => {
        this.sending.set(false);
        this.messageBody = '';
        this.load(r.id);
      },
      error: () => this.sending.set(false)
    });
  }

  // ─── Annuler ma demande ────────────────────────────────────────────────
  askCancel(): void {
    this.confirmingCancel.set(true);
  }

  dismissCancel(): void {
    this.confirmingCancel.set(false);
  }

  confirmCancel(): void {
    const r = this.request();
    if (!r || this.cancelling()) return;

    this.cancelling.set(true);
    this.service.cancel(r.id).subscribe({
      next: () => {
        this.cancelling.set(false);
        this.confirmingCancel.set(false);
        this.load(r.id);
      },
      error: () => {
        this.cancelling.set(false);
        this.confirmingCancel.set(false);
      }
    });
  }

  back(): void {
    this.router.navigate(['/mis-pedidos']);
  }
}
