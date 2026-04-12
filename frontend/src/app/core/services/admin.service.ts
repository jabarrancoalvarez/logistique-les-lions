import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@environments/environment';

export interface AdminStats {
  totalVehicles: number;
  activeListings: number;
  totalUsers: number;
  newUsersThisMonth: number;
  activeProcesses: number;
  completedProcesses: number;
  totalConversations: number;
  totalListingValue: number;
}

export interface VehicleAdminItem {
  id: string;
  title: string;
  slug: string;
  status: string;
  price: number;
  currency: string;
  sellerEmail: string;
  makeName: string;
  year: number;
  createdAt: string;
  expiresAt?: string;
}

export interface StatusBucket {
  status: string;
  count: number;
}

export interface MonthBucket {
  month: string;
  count: number;
}

export interface DashboardKpis {
  processesByStatus: StatusBucket[];
  vehiclesByStatus: StatusBucket[];
  processesPerMonth: MonthBucket[];
  averageLeadTimeDays: number;
  openIncidents: number;
  completedThisMonth: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly apiUrl = `${environment.apiUrl}/v1/admin`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<AdminStats> {
    return this.http.get<{ isSuccess: boolean; value: AdminStats }>(`${this.apiUrl}/stats`).pipe(
      map(r => r.value)
    );
  }

  getVehicles(status?: string, page = 1, pageSize = 20): Observable<PagedResult<VehicleAdminItem>> {
    let url = `${this.apiUrl}/vehicles?page=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<{ isSuccess: boolean; value: PagedResult<VehicleAdminItem> }>(url).pipe(
      map(r => r.value)
    );
  }

  getDashboardKpis(): Observable<DashboardKpis> {
    return this.http.get<{ isSuccess: boolean; value: DashboardKpis }>(`${this.apiUrl}/dashboard/kpis`).pipe(
      map(r => r.value)
    );
  }

  approveVehicle(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/vehicles/${id}/approve`, {});
  }
}
