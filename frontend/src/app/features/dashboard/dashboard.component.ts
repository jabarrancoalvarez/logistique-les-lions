import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { VehicleService } from '@core/services/vehicle.service';

@Component({
  selector: 'lll-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly vehicleService = inject(VehicleService);

  readonly user = computed(() => this.auth.user());
  readonly displayName = computed(() => {
    const u = this.user();
    if (!u) return '';
    const fullName = `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim();
    return fullName || u.email;
  });

  readonly myVehiclesCount = signal<number | null>(null);
  readonly favoritesCount  = signal<number | null>(null);

  ngOnInit(): void {
    const userId = this.user()?.id;
    if (!userId) return;

    this.vehicleService.getVehicles({ sellerId: userId, pageSize: 1 }).subscribe({
      next: (r) => this.myVehiclesCount.set(r.totalCount),
      error: () => this.myVehiclesCount.set(0)
    });

    this.vehicleService.getMyFavorites(userId).subscribe({
      next: (items) => this.favoritesCount.set(items.length),
      error: () => this.favoritesCount.set(0)
    });
  }
}
