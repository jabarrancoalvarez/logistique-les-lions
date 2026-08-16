import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () =>
      import('./features/landing/landing.routes').then(m => m.LANDING_ROUTES)
  },
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'vehiculos',
    loadChildren: () =>
      import('./features/vehicles/vehicles.routes').then(m => m.VEHICLES_ROUTES)
  },
  {
    path: 'tramitacion',
    loadChildren: () =>
      import('./features/compliance/compliance.routes').then(m => m.COMPLIANCE_ROUTES)
  },
  {
    path: 'mensajes',
    loadChildren: () =>
      import('./features/messaging/messaging.routes').then(m => m.MESSAGING_ROUTES)
  },
  {
    path: 'mis-vehiculos',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/vehicles/my-vehicles/my-vehicles.component').then(m => m.MyVehiclesComponent),
    title: 'Mes annonces — Yoon u Auto'
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    title: 'Mon tableau de bord — Yoon u Auto'
  },
  {
    path: 'favoritos',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/vehicles/favorites/favorites.component').then(m => m.FavoritesComponent),
    title: 'Favoris — Yoon u Auto'
  },
  {
    path: 'mis-negociaciones',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/negotiations/negotiations.component').then(m => m.NegotiationsComponent),
    title: 'Mes négociations — Yoon u Auto'
  },
  {
    path: 'mis-negociaciones/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/negotiations/negotiation-detail.component')
        .then(m => m.NegotiationDetailComponent),
    title: 'Négociation — Yoon u Auto'
  },
  // Mes recherches — centro de todo lo relacionado con encontrar un vehículo.
  {
    path: 'mis-busquedas',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/searches/my-searches.component').then(m => m.MySearchesComponent),
    title: 'Mes recherches — Yoon u Auto'
  },
  {
    path: 'ajustes',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/users/settings/settings.component').then(m => m.SettingsComponent),
    title: 'Paramètres — Yoon u Auto'
  },
  // «Prochainement» cuelga del perfil y no del menú principal: el documento pide no
  // llenar la navegación con algo que todavía no existe.
  {
    path: 'prochainement',
    loadComponent: () =>
      import('./features/upcoming/upcoming.component').then(m => m.UpcomingComponent),
    title: 'Prochainement — Yoon u Auto'
  },
  // Mon Garage — espacio privado de los vehículos que el usuario posee.
  {
    path: 'mi-garaje',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/garage/garage.component').then(m => m.GarageComponent),
    title: 'Mon Garage — Yoon u Auto'
  },
  {
    path: 'mi-garaje/nuevo',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/garage/garage-vehicle.component').then(m => m.GarageVehicleComponent),
    title: 'Ajouter un véhicule — Yoon u Auto'
  },
  {
    path: 'mi-garaje/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/garage/garage-vehicle.component').then(m => m.GarageVehicleComponent),
    title: 'Mon véhicule — Yoon u Auto'
  },
  {
    path: 'mis-pedidos',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/vehicles/vehicle-requests/vehicle-requests.component')
        .then(m => m.VehicleRequestsComponent),
    title: 'Mes demandes — Yoon u Auto'
  },
  {
    path: 'mis-pedidos/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/vehicles/vehicle-requests/vehicle-request-detail.component')
        .then(m => m.VehicleRequestDetailComponent),
    title: 'Ma demande — Yoon u Auto'
  },
  {
    path: 'comparateur',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/vehicles/comparator/comparator.component')
        .then(m => m.ComparatorComponent),
    title: 'Comparateur — Yoon u Auto'
  },
  {
    path: 'busquedas-guardadas',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/vehicles/saved-searches/saved-searches.component')
        .then(m => m.SavedSearchesComponent),
    title: 'Recherches enregistrées — Yoon u Auto'
  },
  {
    path: 'perfil',
    loadChildren: () =>
      import('./features/users/users.routes').then(m => m.USERS_ROUTES)
  },
  {
    path: 'concesionarios',
    loadComponent: () =>
      import('./features/dealers/dealers.component').then(m => m.DealersComponent),
    title: 'Concesionarios — Yoon U Auto'
  },
  {
    path: 'admin',
    loadChildren: () =>
      import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  // ─── Footer: Plataforma ────────────────────────────────────────────────────
  {
    path: 'precios',
    loadComponent: () =>
      import('./features/pricing/pricing-page.component').then(m => m.PricingPageComponent),
    title: 'Precios — Yoon U Auto'
  },
  {
    path: 'calculadora',
    redirectTo: '/tramitacion/calculadora',
    pathMatch: 'full'
  },
  {
    path: 'inspectores',
    loadComponent: () =>
      import('./features/inspectors/inspectors-page.component').then(m => m.InspectorsPageComponent),
    title: 'Inspectores Certificados — Yoon U Auto'
  },
  // ─── Footer: Servicios ────────────────────────────────────────────────────
  {
    path: 'guias',
    loadChildren: () =>
      import('./features/guides/guides.routes').then(m => m.GUIDES_ROUTES)
  },
  {
    path: 'logistica',
    redirectTo: '/transporte',
    pathMatch: 'full'
  },
  // ─── Footer: Legal ────────────────────────────────────────────────────────
  {
    path: 'legal',
    loadChildren: () =>
      import('./features/legal/legal.routes').then(m => m.LEGAL_ROUTES)
  },
  // ─── Coming soon ─────────────────────────────────────────────────────────
  {
    path: 'pagos',
    loadComponent: () =>
      import('./shared/components/coming-soon/coming-soon.component').then(m => m.ComingSoonComponent),
    title: 'Pagos — Yoon U Auto'
  },
  {
    path: 'valoraciones',
    loadComponent: () =>
      import('./shared/components/coming-soon/coming-soon.component').then(m => m.ComingSoonComponent),
    title: 'Valoraciones — Yoon U Auto'
  },
  {
    path: 'transporte',
    loadComponent: () =>
      import('./features/transport/transport-page.component').then(m => m.TransportPageComponent),
    title: 'Transporte Internacional — Yoon U Auto'
  },
  {
    path: 'financiacion',
    loadComponent: () =>
      import('./features/financing/financing-page.component').then(m => m.FinancingPageComponent),
    title: 'Financiación — Yoon U Auto'
  },
  {
    path: 'tracking',
    loadComponent: () =>
      import('./features/tracking/public-tracking.component').then(m => m.PublicTrackingComponent),
    title: 'Seguimiento de trámite — Yoon U Auto'
  },
  // Página pública del QR de un contrato — sin cuenta.
  {
    path: 'verification',
    loadComponent: () =>
      import('./features/negotiations/contract-verification.component')
        .then(m => m.ContractVerificationComponent),
    title: 'Vérification d\'une vente — Yoon u Auto'
  },
  {
    path: 'verification/:code',
    loadComponent: () =>
      import('./features/negotiations/contract-verification.component')
        .then(m => m.ContractVerificationComponent),
    title: 'Vérification d\'une vente — Yoon u Auto'
  },
  {
    path: '**',
    loadComponent: () =>
      import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];
