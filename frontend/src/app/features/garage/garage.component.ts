import { Component, ChangeDetectionStrategy, OnInit, OnDestroy, signal, inject } from '@angular/core';
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
export class GarageComponent implements OnInit, OnDestroy {
  private readonly service = inject(GarageService);

  readonly garage = signal<Garage | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);

  /**
   * Miniaturas de las tarjetas, en memoria.
   *
   * Las fotos del garaje son privadas: se piden por endpoint autenticado y llegan como
   * blob. Una etiqueta &lt;img&gt; no envía el token, así que no puede apuntar a la API.
   */
  readonly thumbnails = signal<Record<string, string>>({});

  ngOnInit(): void {
    this.service.getMyGarage().subscribe({
      next: g => {
        this.garage.set(g);
        this.loading.set(false);
        for (const v of g.vehicles) {
          if (!v.primaryImageId) continue;
          this.service.getImageFile(v.primaryImageId).subscribe({
            next: blob => this.thumbnails.update(t => ({
              ...t, [v.id]: URL.createObjectURL(blob)
            })),
            error: () => { /* sin miniatura se ve el icono, no se rompe la tarjeta */ }
          });
        }
      },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }

  ngOnDestroy(): void {
    for (const url of Object.values(this.thumbnails())) URL.revokeObjectURL(url);
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
