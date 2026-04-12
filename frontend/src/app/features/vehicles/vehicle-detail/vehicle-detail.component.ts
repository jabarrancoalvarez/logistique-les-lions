import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { VehicleService, VehicleDetail } from '@core/services/vehicle.service';
import { AuthService } from '@core/auth/auth.service';
import { MessagingService } from '@core/services/messaging.service';

@Component({
  selector: 'lll-vehicle-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, CurrencyPipe, DecimalPipe],
  templateUrl: './vehicle-detail.component.html'
})
export class VehicleDetailComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly route          = inject(ActivatedRoute);
  private readonly router         = inject(Router);
  readonly auth                   = inject(AuthService);
  private readonly messaging      = inject(MessagingService);

  readonly vehicle          = signal<VehicleDetail | null>(null);
  readonly isLoading        = signal(true);
  readonly error            = signal<string | null>(null);
  readonly activeImageIndex = signal(0);
  readonly isFavorited      = signal(false);
  readonly contacting       = signal(false);

  readonly activeImage = computed(() => {
    const v = this.vehicle();
    if (!v || v.images.length === 0) return null;
    return v.images[this.activeImageIndex()];
  });

  readonly conditionLabel = computed(() => {
    const map: Record<string, string> = {
      New: 'Nuevo', Used: 'Segunda mano', Km0: 'Km 0'
    };
    return map[this.vehicle()?.condition ?? ''] ?? '';
  });

  readonly fuelLabel = computed(() => {
    const map: Record<string, string> = {
      Gasoline: 'Gasolina', Diesel: 'Diésel', Electric: 'Eléctrico',
      Hybrid: 'Híbrido', PluginHybrid: 'Híbrido enchufable',
      Lpg: 'GLP', Cng: 'GNC', Hydrogen: 'Hidrógeno'
    };
    return map[this.vehicle()?.fuelType ?? ''] ?? '';
  });

  readonly transmissionLabel = computed(() => {
    const map: Record<string, string> = {
      Manual: 'Manual', Automatic: 'Automático',
      SemiAutomatic: 'Semiautomático', Cvt: 'CVT'
    };
    return map[this.vehicle()?.transmission ?? ''] ?? '';
  });

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.vehicleService.getVehicleBySlug(slug).subscribe({
      next: v => {
        this.vehicle.set(v);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Vehículo no encontrado.');
        this.isLoading.set(false);
      }
    });
  }

  selectImage(index: number): void {
    this.activeImageIndex.set(index);
  }

  formatMileage(km: number | null): string {
    if (!km) return 'N/D';
    return new Intl.NumberFormat('es-ES').format(km) + ' km';
  }

  formatPrice(price: number, currency: string): string {
    return new Intl.NumberFormat('es-ES', {
      style: 'currency', currency, maximumFractionDigits: 0
    }).format(price);
  }

  contactSeller(): void {
    const v = this.vehicle();
    if (!v) return;

    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.contacting.set(true);
    this.messaging.sendMessageRest(
      v.sellerId,
      v.id,
      `Hola, estoy interesado en tu vehículo: ${v.title}. ¿Podría obtener más información?`
    ).subscribe({
      next: () => {
        this.contacting.set(false);
        this.router.navigate(['/mensajes']);
      },
      error: () => {
        // Conversation may already exist, navigate anyway
        this.contacting.set(false);
        this.router.navigate(['/mensajes']);
      }
    });
  }
}
