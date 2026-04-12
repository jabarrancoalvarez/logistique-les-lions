import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { AdminService, DashboardKpis, StatusBucket } from '@core/services/admin.service';

/**
 * Widget de KPIs del panel admin. Renderiza barras horizontales y un sparkline
 * vertical con CSS puro — sin chart libraries para no inflar el bundle.
 */
@Component({
  selector: 'lll-dashboard-kpis',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DecimalPipe],
  template: `
    @if (loading()) {
      <div class="rounded-2xl border border-navy/10 bg-ivory p-8 text-center text-navy/60">
        Cargando KPIs…
      </div>
    }
    @if (!loading() && kpis(); as k) {
      <section class="space-y-6">
        <!-- Cards -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div class="rounded-2xl border border-navy/10 bg-ivory p-5">
            <p class="text-xs uppercase tracking-wide text-navy/60">Lead time medio</p>
            <p class="mt-1 text-3xl font-semibold text-navy">{{ k.averageLeadTimeDays | number:'1.0-1' }} <span class="text-base font-normal text-navy/60">días</span></p>
          </div>
          <div class="rounded-2xl border border-navy/10 bg-ivory p-5">
            <p class="text-xs uppercase tracking-wide text-navy/60">Incidencias abiertas</p>
            <p class="mt-1 text-3xl font-semibold text-rose-600">{{ k.openIncidents }}</p>
          </div>
          <div class="rounded-2xl border border-navy/10 bg-ivory p-5">
            <p class="text-xs uppercase tracking-wide text-navy/60">Completados este mes</p>
            <p class="mt-1 text-3xl font-semibold text-emerald-600">{{ k.completedThisMonth }}</p>
          </div>
        </div>

        <!-- Bar chart: procesos por estado -->
        <div class="rounded-2xl border border-navy/10 bg-ivory p-5">
          <h3 class="text-sm font-semibold text-navy mb-4">Procesos por estado</h3>
          @if (k.processesByStatus.length === 0) {
            <p class="text-sm text-navy/50">Sin datos.</p>
          } @else {
            <div class="space-y-2">
              @for (b of k.processesByStatus; track b.status) {
                <div>
                  <div class="flex justify-between text-xs text-navy/70 mb-1">
                    <span>{{ b.status }}</span>
                    <span class="font-medium">{{ b.count }}</span>
                  </div>
                  <div class="h-2 bg-navy/5 rounded-full overflow-hidden">
                    <div class="h-full bg-navy rounded-full transition-all"
                         [style.width.%]="percent(b, k.processesByStatus)"></div>
                  </div>
                </div>
              }
            </div>
          }
        </div>

        <!-- Sparkline vertical: timeseries -->
        <div class="rounded-2xl border border-navy/10 bg-ivory p-5">
          <h3 class="text-sm font-semibold text-navy mb-4">Procesos por mes (últimos 6)</h3>
          <div class="flex items-end gap-3 h-32">
            @for (m of k.processesPerMonth; track m.month) {
              <div class="flex flex-col items-center justify-end flex-1 h-full">
                <div class="w-full bg-navy rounded-t-md transition-all"
                     [style.height.%]="monthHeight(m, k.processesPerMonth)"
                     [title]="m.month + ': ' + m.count"></div>
                <span class="mt-2 text-[10px] text-navy/60">{{ m.month.slice(5) }}</span>
                <span class="text-[10px] font-semibold text-navy">{{ m.count }}</span>
              </div>
            }
          </div>
        </div>
      </section>
    }
  `
})
export class DashboardKpisComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly kpis    = signal<DashboardKpis | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.admin.getDashboardKpis().subscribe({
      next: k => { this.kpis.set(k); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  percent(bucket: StatusBucket, all: StatusBucket[]): number {
    const max = Math.max(...all.map(b => b.count), 1);
    return Math.round((bucket.count / max) * 100);
  }

  monthHeight(m: { count: number }, all: { count: number }[]): number {
    const max = Math.max(...all.map(x => x.count), 1);
    return Math.max(2, Math.round((m.count / max) * 100));
  }
}
