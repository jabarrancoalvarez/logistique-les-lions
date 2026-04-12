import { Routes } from '@angular/router';
import { adminGuard } from '@core/auth/auth.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./admin-layout.component').then(m => m.AdminLayoutComponent),
    title: 'Panel de administración — Logistique Les Lions',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent),
        title: 'Dashboard — Admin'
      },
      {
        path: 'vehiculos',
        loadComponent: () =>
          import('./sections/admin-section.components').then(m => m.AdminVehiclesComponent),
        title: 'Vehículos — Admin'
      },
      {
        path: 'procesos',
        loadComponent: () =>
          import('./sections/admin-section.components').then(m => m.AdminProcessesComponent),
        title: 'Procesos — Admin'
      },
      {
        path: 'incidencias',
        loadComponent: () =>
          import('./sections/admin-section.components').then(m => m.AdminIncidentsComponent),
        title: 'Incidencias — Admin'
      },
      {
        path: 'partners',
        loadComponent: () =>
          import('./sections/admin-section.components').then(m => m.AdminPartnersComponent),
        title: 'Marketplace — Admin'
      },
      {
        path: 'usuarios',
        loadComponent: () =>
          import('./sections/admin-section.components').then(m => m.AdminUsersComponent),
        title: 'Usuarios — Admin'
      },
      {
        path: 'notificaciones',
        loadComponent: () =>
          import('./sections/admin-section.components').then(m => m.AdminNotificationsComponent),
        title: 'Notificaciones — Admin'
      }
    ]
  }
];
