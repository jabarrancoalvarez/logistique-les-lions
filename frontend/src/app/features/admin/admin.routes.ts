import { Routes } from '@angular/router';
import { adminGuard } from '@core/auth/auth.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent),
    title: 'Panel de administración — Logistique Les Lions'
  }
];
