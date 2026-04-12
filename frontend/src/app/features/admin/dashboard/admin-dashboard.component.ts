import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService, AdminStats, VehicleAdminItem } from '@core/services/admin.service';
import { DashboardKpisComponent } from './dashboard-kpis.component';

@Component({
  selector: 'lll-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, DashboardKpisComponent],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  stats    = signal<AdminStats | null>(null);
  vehicles = signal<VehicleAdminItem[]>([]);
  loading  = signal(true);
  page     = signal(1);
  totalPages = signal(1);
  statusFilter = signal<string>('');

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.admin.getStats().subscribe(s => this.stats.set(s));
    this.loadVehicles();
  }

  loadVehicles(): void {
    this.loading.set(true);
    this.admin.getVehicles(this.statusFilter() || undefined, this.page()).subscribe({
      next: (r) => {
        this.vehicles.set(r.items);
        this.totalPages.set(r.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  filterByStatus(s: string): void {
    this.statusFilter.set(s);
    this.page.set(1);
    this.loadVehicles();
  }

  approve(id: string): void {
    this.admin.approveVehicle(id).subscribe(() => this.loadVehicles());
  }

  prevPage(): void { if (this.page() > 1) { this.page.update(p => p - 1); this.loadVehicles(); } }
  nextPage(): void { if (this.page() < this.totalPages()) { this.page.update(p => p + 1); this.loadVehicles(); } }
}
