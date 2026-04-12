import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VehicleService, VehicleListItem } from '@core/services/vehicle.service';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'lll-my-vehicles',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-vehicles.component.html'
})
export class MyVehiclesComponent implements OnInit {
  vehicles = signal<VehicleListItem[]>([]);
  loading  = signal(true);
  total    = signal(0);

  constructor(
    private vehicleService: VehicleService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const userId = this.auth.user()?.id;
    if (!userId) { this.loading.set(false); return; }

    this.vehicleService.getVehicles({ sellerId: userId, pageSize: 100 }).subscribe({
      next: (r) => {
        this.vehicles.set(r.items);
        this.total.set(r.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Active: 'Activo', Reviewing: 'En revisión',
      Sold: 'Vendido', Expired: 'Expirado', Inactive: 'Inactivo'
    };
    return map[status] ?? status;
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Active:    'bg-green-100 text-green-800',
      Reviewing: 'bg-yellow-100 text-yellow-800',
      Sold:      'bg-blue-100 text-blue-800',
      Expired:   'bg-red-100 text-red-800',
      Inactive:  'bg-gray-100 text-gray-600'
    };
    return map[status] ?? 'bg-gray-100 text-gray-600';
  }
}
