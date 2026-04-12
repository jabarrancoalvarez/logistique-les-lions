import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TrackingService, PublicTracking } from '@core/services/tracking.service';

/**
 * Página pública sin login. El destinatario introduce su código de tracking
 * y obtiene el estado del proceso de tramitación. No revela datos personales.
 */
@Component({
  selector: 'lll-public-tracking',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <div class="min-h-screen bg-ivory">
      <div class="max-w-2xl mx-auto px-6 py-16">
        <h1 class="font-heading text-3xl md:text-4xl font-bold text-navy text-center">
          Seguimiento de tu trámite
        </h1>
        <p class="mt-3 text-center text-navy/60">
          Introduce el código de 12 caracteres que recibiste por email para ver el estado actualizado.
        </p>

        <form (ngSubmit)="search()" class="mt-8 flex gap-3">
          <input
            type="text"
            [(ngModel)]="code"
            name="code"
            maxlength="16"
            placeholder="ej. ABCD23EFGH45"
            class="flex-1 px-4 py-3 rounded-xl border border-navy/20 bg-white text-navy uppercase tracking-wider focus:outline-none focus:border-navy"
            autocomplete="off" />
          <button
            type="submit"
            [disabled]="loading() || !code.trim()"
            class="px-6 py-3 rounded-xl bg-navy text-ivory font-medium disabled:opacity-40">
            {{ loading() ? 'Buscando…' : 'Buscar' }}
          </button>
        </form>

        @if (error()) {
          <div class="mt-6 p-4 rounded-xl border border-rose-300 bg-rose-50 text-rose-700 text-sm text-center">
            No hemos encontrado ningún trámite con ese código. Verifica que esté escrito correctamente.
          </div>
        }

        @if (result(); as r) {
          <article class="mt-8 rounded-2xl border border-navy/10 bg-white p-6 shadow-sm">
            <header class="flex items-start justify-between gap-4 border-b border-navy/10 pb-4">
              <div>
                <p class="text-xs uppercase tracking-wide text-navy/50">Código</p>
                <p class="font-mono text-lg text-navy">{{ r.trackingCode }}</p>
              </div>
              <span class="px-3 py-1 rounded-full text-xs font-semibold"
                    [class.bg-emerald-100]="r.status === 'Completed'"
                    [class.text-emerald-700]="r.status === 'Completed'"
                    [class.bg-amber-100]="r.status === 'InProgress' || r.status === 'PendingDocuments'"
                    [class.text-amber-700]="r.status === 'InProgress' || r.status === 'PendingDocuments'"
                    [class.bg-rose-100]="r.status === 'Cancelled' || r.status === 'Rejected'"
                    [class.text-rose-700]="r.status === 'Cancelled' || r.status === 'Rejected'"
                    [class.bg-navy]="r.status === 'Draft'"
                    [class.text-ivory]="r.status === 'Draft'">
                {{ r.status }}
              </span>
            </header>

            <dl class="mt-4 grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              <div>
                <dt class="text-navy/50 text-xs uppercase">Vehículo</dt>
                <dd class="text-navy font-medium mt-1">{{ r.vehicleTitle }}</dd>
              </div>
              <div>
                <dt class="text-navy/50 text-xs uppercase">Tipo</dt>
                <dd class="text-navy font-medium mt-1">{{ r.processType }}</dd>
              </div>
              <div>
                <dt class="text-navy/50 text-xs uppercase">Origen</dt>
                <dd class="text-navy font-medium mt-1">{{ r.originCountry }}</dd>
              </div>
              <div>
                <dt class="text-navy/50 text-xs uppercase">Destino</dt>
                <dd class="text-navy font-medium mt-1">{{ r.destinationCountry }}</dd>
              </div>
              <div>
                <dt class="text-navy/50 text-xs uppercase">Iniciado</dt>
                <dd class="text-navy font-medium mt-1">{{ r.startedAt ? (r.startedAt | date:'dd/MM/yyyy') : '—' }}</dd>
              </div>
              <div>
                <dt class="text-navy/50 text-xs uppercase">Última actualización</dt>
                <dd class="text-navy font-medium mt-1">{{ r.lastUpdatedAt | date:'dd/MM/yyyy HH:mm' }}</dd>
              </div>
            </dl>

            <div class="mt-6">
              <div class="flex justify-between text-xs text-navy/60 mb-1">
                <span>Progreso</span>
                <span class="font-semibold">{{ r.completionPercent }}%</span>
              </div>
              <div class="h-3 bg-navy/5 rounded-full overflow-hidden">
                <div class="h-full bg-navy rounded-full transition-all"
                     [style.width.%]="r.completionPercent"></div>
              </div>
            </div>
          </article>
        }
      </div>
    </div>
  `
})
export class PublicTrackingComponent {
  private readonly tracking = inject(TrackingService);

  code = '';
  readonly result  = signal<PublicTracking | null>(null);
  readonly loading = signal(false);
  readonly error   = signal(false);

  search(): void {
    if (!this.code.trim()) return;
    this.loading.set(true);
    this.error.set(false);
    this.result.set(null);

    this.tracking.getByCode(this.code).subscribe({
      next: r => { this.result.set(r); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }
}
