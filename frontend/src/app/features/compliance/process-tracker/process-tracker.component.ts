import {
  Component, ChangeDetectionStrategy, signal, inject, OnInit
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { ComplianceService, ProcessStatus } from '@core/services/compliance.service';
import { DocumentChecklistComponent } from '../document-checklist/document-checklist.component';

@Component({
  selector: 'lll-process-tracker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, CurrencyPipe, DocumentChecklistComponent],
  templateUrl: './process-tracker.component.html'
})
export class ProcessTrackerComponent implements OnInit {
  private readonly complianceService = inject(ComplianceService);
  private readonly route = inject(ActivatedRoute);

  readonly process = signal<ProcessStatus | null>(null);
  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);

  readonly statusLabel: Record<string, string> = {
    Draft:     'Borrador',
    Active:    'En proceso',
    OnHold:    'En espera',
    Completed: 'Completado',
    Cancelled: 'Cancelado',
    Rejected:  'Rechazado',
  };

  readonly statusClass: Record<string, string> = {
    Draft:     'bg-navy/10 text-navy',
    Active:    'bg-blue-100 text-blue-800',
    OnHold:    'bg-amber-100 text-amber-800',
    Completed: 'bg-green-100 text-green-800',
    Cancelled: 'bg-red-100 text-red-800',
    Rejected:  'bg-red-100 text-red-800',
  };

  ngOnInit(): void {
    const processId = this.route.snapshot.paramMap.get('id');
    if (!processId) {
      this.error.set('ID de proceso no especificado.');
      this.isLoading.set(false);
      return;
    }

    this.complianceService.getProcessStatus(processId).subscribe({
      next: p => {
        this.process.set(p);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Proceso no encontrado.');
        this.isLoading.set(false);
      }
    });
  }

  formatEur(amount: number | null): string {
    if (!amount) return '—';
    return new Intl.NumberFormat('es-ES', {
      style: 'currency', currency: 'EUR', maximumFractionDigits: 0
    }).format(amount);
  }
}
