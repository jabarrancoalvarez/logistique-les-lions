import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ComplianceService, CostEstimate } from '@core/services/compliance.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'lll-cost-estimator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cost-estimator.component.html'
})
export class CostEstimatorComponent implements OnInit {
  private readonly complianceService = inject(ComplianceService);
  private readonly route = inject(ActivatedRoute);

  readonly estimate = signal<CostEstimate | null>(null);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  vehicleId = '';
  origin = 'ES';
  destination = 'DE';

  readonly countries = [
    { code: 'ES', name: 'España 🇪🇸' },
    { code: 'DE', name: 'Alemania 🇩🇪' },
    { code: 'FR', name: 'Francia 🇫🇷' },
    { code: 'IT', name: 'Italia 🇮🇹' },
    { code: 'JP', name: 'Japón 🇯🇵' },
    { code: 'US', name: 'EE.UU. 🇺🇸' },
    { code: 'GB', name: 'Reino Unido 🇬🇧' },
    { code: 'MA', name: 'Marruecos 🇲🇦' },
  ];

  ngOnInit(): void {
    this.vehicleId = this.route.snapshot.queryParams['vehicleId'] ?? '';
    this.origin = this.route.snapshot.queryParams['origin'] ?? 'ES';
    this.destination = this.route.snapshot.queryParams['destination'] ?? 'DE';
    if (this.vehicleId) this.calculate();
  }

  calculate(): void {
    if (!this.vehicleId || !this.origin || !this.destination) return;
    this.isLoading.set(true);
    this.error.set(null);

    this.complianceService.estimateCost(this.vehicleId, this.origin, this.destination).subscribe({
      next: result => {
        this.estimate.set(result);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('No se pudo calcular el coste. Verifica los datos.');
        this.isLoading.set(false);
      }
    });
  }

  formatEur(amount: number): string {
    return new Intl.NumberFormat('es-ES', {
      style: 'currency', currency: 'EUR', maximumFractionDigits: 0
    }).format(amount);
  }

  get isIntraEu(): boolean {
    const eu = ['ES','DE','FR','IT','PT','NL','BE'];
    return eu.includes(this.origin) && eu.includes(this.destination);
  }
}
