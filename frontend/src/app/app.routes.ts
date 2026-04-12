import { Routes } from '@angular/router';

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
    canActivate: [() => import('./core/auth/auth.guard').then(m => m.authGuard)],
    loadComponent: () =>
      import('./features/vehicles/my-vehicles/my-vehicles.component').then(m => m.MyVehiclesComponent),
    title: 'Mis vehículos — Logistique Les Lions'
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
    title: 'Concesionarios — Logistique Les Lions'
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
    title: 'Precios — Logistique Les Lions'
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
    title: 'Inspectores Certificados — Logistique Les Lions'
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
    title: 'Pagos — Logistique Les Lions'
  },
  {
    path: 'valoraciones',
    loadComponent: () =>
      import('./shared/components/coming-soon/coming-soon.component').then(m => m.ComingSoonComponent),
    title: 'Valoraciones — Logistique Les Lions'
  },
  {
    path: 'transporte',
    loadComponent: () =>
      import('./features/transport/transport-page.component').then(m => m.TransportPageComponent),
    title: 'Transporte Internacional — Logistique Les Lions'
  },
  {
    path: 'financiacion',
    loadComponent: () =>
      import('./features/financing/financing-page.component').then(m => m.FinancingPageComponent),
    title: 'Financiación — Logistique Les Lions'
  },
  {
    path: 'tracking',
    loadComponent: () =>
      import('./features/tracking/public-tracking.component').then(m => m.PublicTrackingComponent),
    title: 'Seguimiento de trámite — Logistique Les Lions'
  },
  {
    path: '**',
    loadComponent: () =>
      import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];
