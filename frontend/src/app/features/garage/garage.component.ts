import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GarageService, Garage, GarageCardReminder } from '@core/services/garage.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/**
 * Pantalla principal de Mon Garage: resumen arriba y tarjetas debajo.
 *
 * El resumen crecerá con la valeur estimée totale y los rappels à venir cuando lleguen
 * el valor estimado y los recordatorios.
 */
@Component({
  selector: 'lll-garage',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, FcfaPipe],
  templateUrl: './garage.component.html'
})
export class GarageComponent implements OnInit {
  private readonly service = inject(GarageService);

  readonly garage = signal<Garage | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);

  ngOnInit(): void {
    this.service.getMyGarage().subscribe({
      next: g => { this.garage.set(g); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }

  /** «147.500 km» */
  mileage(km: number | null): string | null {
    return km === null ? null : `${km.toLocaleString('de-DE')} km`;
  }

  /**
   * Cuándo toca el próximo rappel: «dans 2.500 km», «12/12/2026», «à faire».
   *
   * Se muestra lo que llegue antes, y si ya ha vencido se dice sin rodeos.
   */
  reminderWhen(reminder: GarageCardReminder): string | null {
    if (reminder.status === 'AFaire') return 'à faire';

    if (reminder.mileageRemaining !== null && reminder.mileageRemaining >= 0)
      return `dans ${reminder.mileageRemaining.toLocaleString('de-DE')} km`;

    if (reminder.daysRemaining !== null && reminder.daysRemaining >= 0)
      return `dans ${reminder.daysRemaining} j`;

    return null;
  }
}
