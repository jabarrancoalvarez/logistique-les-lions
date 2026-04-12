import {
  Component, OnInit, ChangeDetectionStrategy, inject, signal, ElementRef, AfterViewInit
} from '@angular/core';
import { VehicleService, VehicleStats } from '../../../core/services/vehicle.service';
import { CommonModule } from '@angular/common';

interface StatItem {
  label: string;
  value: number;
  displayValue: number;
  suffix: string;
  icon: string;
  description: string;
}

@Component({
  selector: 'lll-stats-counters',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './stats-counters.component.html'
})
export class StatsCountersComponent implements OnInit, AfterViewInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly el = inject(ElementRef);

  readonly stats = signal<StatItem[]>([
    { label: 'Vehículos activos', value: 0, displayValue: 0, suffix: '+', icon: '🚗', description: 'Anuncios verificados disponibles ahora' },
    { label: 'Países operativos', value: 0, displayValue: 0, suffix: '', icon: '🌍', description: 'Con documentación de importación/exportación' },
    { label: 'Transacciones completadas', value: 0, displayValue: 0, suffix: '+', icon: '✅', description: 'Operaciones cerradas con éxito' },
    { label: 'Marcas disponibles', value: 0, displayValue: 0, suffix: '+', icon: '🏷️', description: 'De los mejores fabricantes del mundo' }
  ]);

  private hasAnimated = false;

  ngOnInit(): void {
    this.vehicleService.getStats().subscribe((data: VehicleStats) => {
      this.stats.update(items => items.map((item, i) => ({
        ...item,
        value: [
          data.activeVehicles || 1240,
          data.supportedCountries || 12,
          data.completedTransactions || 1287,
          data.totalMakes || 85
        ][i]
      })));
    });
  }

  ngAfterViewInit(): void {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && !this.hasAnimated) {
          this.hasAnimated = true;
          this.animateCounters();
        }
      },
      { threshold: 0.3 }
    );
    observer.observe(this.el.nativeElement);
  }

  private animateCounters(): void {
    const duration = 1800;
    const steps = 60;
    const interval = duration / steps;

    let step = 0;
    const timer = setInterval(() => {
      step++;
      const progress = this.easeOutCubic(step / steps);
      this.stats.update(items =>
        items.map(item => ({
          ...item,
          displayValue: Math.round(item.value * progress)
        }))
      );
      if (step >= steps) clearInterval(timer);
    }, interval);
  }

  private easeOutCubic(t: number): number {
    return 1 - Math.pow(1 - t, 3);
  }
}
