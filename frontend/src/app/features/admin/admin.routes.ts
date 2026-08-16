import { Routes } from '@angular/router';
import { adminGuard } from '@core/auth/auth.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./admin-layout.component').then(m => m.AdminLayoutComponent),
    title: 'Administration — Yoon u Auto',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent),
        title: 'Tableau de bord — Administration'
      },
      {
        path: 'vehiculos',
        loadComponent: () =>
          import('./listings/admin-listings.component').then(m => m.AdminListingsComponent),
        title: 'Annonces — Administration'
      },
      {
        path: 'demandes',
        loadComponent: () =>
          import('./requests/admin-requests.component').then(m => m.AdminRequestsComponent),
        title: 'Demandes de véhicules — Administration'
      },
      {
        path: 'negociations',
        loadComponent: () =>
          import('./negotiations/admin-negotiations.component')
            .then(m => m.AdminNegotiationsComponent),
        data: { tab: 'negotiations' },
        title: 'Négociations — Administration'
      },
      {
        path: 'contrats',
        loadComponent: () =>
          import('./negotiations/admin-negotiations.component')
            .then(m => m.AdminNegotiationsComponent),
        data: { tab: 'contracts' },
        title: 'Contrats & ventes — Administration'
      },
      {
        path: 'configuration',
        loadComponent: () =>
          import('./configuration/admin-configuration.component')
            .then(m => m.AdminConfigurationComponent),
        title: 'Configuration — Administration'
      },
      {
        path: 'statistiques',
        loadComponent: () =>
          import('./statistics/admin-statistics.component')
            .then(m => m.AdminStatisticsComponent),
        title: 'Statistiques — Administration'
      },
      {
        path: 'moderation',
        loadComponent: () =>
          import('./moderation/admin-moderation.component')
            .then(m => m.AdminModerationComponent),
        title: 'Modération — Administration'
      },
      {
        path: 'usuarios',
        loadComponent: () =>
          import('./users/admin-users.component').then(m => m.AdminUsersComponent),
        title: 'Utilisateurs — Administration'
      },
      {
        path: 'notificaciones',
        loadComponent: () =>
          import('./communications/admin-communications.component')
            .then(m => m.AdminCommunicationsComponent),
        title: 'Communications — Administration'
      }
    ]
  }
];
