import { Component, Input, ChangeDetectionStrategy, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FeaturedVehicle, VehicleListItem } from '../../../core/services/vehicle.service';

export type VehicleCardData = FeaturedVehicle | VehicleListItem;

@Component({
  selector: 'lll-vehicle-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule, DecimalPipe],
  templateUrl: './vehicle-card.component.html'
})
export class VehicleCardComponent {
  @Input({ required: true }) vehicle!: VehicleCardData;

  /** Normaliza las diferencias entre FeaturedVehicle (countryFlagEmoji) y VehicleListItem (flagEmoji) */
  get flagEmoji(): string | null {
    return (this.vehicle as FeaturedVehicle).countryFlagEmoji
      ?? (this.vehicle as VehicleListItem).flagEmoji
      ?? null;
  }

  readonly isFavorited = signal(false);
  readonly imageError = signal(false);

  onImageError(): void {
    this.imageError.set(true);
  }

  get conditionLabel(): string {
    const map: Record<string, string> = { New: 'Nuevo', Used: 'Ocasión', Km0: 'Km 0' };
    return map[this.vehicle.condition] ?? this.vehicle.condition;
  }

  get conditionClass(): string {
    const map: Record<string, string> = {
      New: 'badge-success',
      Used: 'badge-navy',
      Km0: 'badge-gold'
    };
    return `badge ${map[this.vehicle.condition] ?? 'badge-navy'}`;
  }

  get fuelLabel(): string {
    const map: Record<string, string> = {
      Gasoline: 'Gasolina', Diesel: 'Diésel', Electric: 'Eléctrico',
      Hybrid: 'Híbrido', PluginHybrid: 'Híbrido enchufable',
      LPG: 'GLP', CNG: 'GNC'
    };
    return this.vehicle.fuelType ? (map[this.vehicle.fuelType] ?? this.vehicle.fuelType) : '';
  }

  get formattedMileage(): string {
    if (!this.vehicle.mileage) return 'N/D';
    return `${(this.vehicle.mileage).toLocaleString('es-ES')} km`;
  }

  get daysAgo(): string {
    const diff = Date.now() - new Date(this.vehicle.createdAt).getTime();
    const days = Math.floor(diff / 86400000);
    if (days === 0) return 'Hoy';
    if (days === 1) return 'Ayer';
    return `Hace ${days} días`;
  }

  toggleFavorite(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.isFavorited.update(v => !v);
  }
}
