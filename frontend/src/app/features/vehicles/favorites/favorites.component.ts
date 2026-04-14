import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VehicleService, VehicleListItem } from '@core/services/vehicle.service';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'lll-favorites',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './favorites.component.html'
})
export class FavoritesComponent implements OnInit {
  vehicles = signal<VehicleListItem[]>([]);
  loading  = signal(true);
  hasError = signal(false);

  constructor(
    private vehicleService: VehicleService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const userId = this.auth.user()?.id;
    if (!userId) { this.loading.set(false); return; }

    this.vehicleService.getMyFavorites(userId).subscribe({
      next: (items) => {
        this.vehicles.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.hasError.set(true);
        this.loading.set(false);
      }
    });
  }

  remove(vehicleId: string): void {
    const userId = this.auth.user()?.id;
    if (!userId) return;
    this.vehicleService.toggleFavorite(vehicleId, userId).subscribe({
      next: () => this.vehicles.update(list => list.filter(v => v.id !== vehicleId))
    });
  }
}
