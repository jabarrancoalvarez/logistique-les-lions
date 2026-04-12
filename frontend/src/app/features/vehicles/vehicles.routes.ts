import { Routes } from '@angular/router';

export const VEHICLES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./vehicle-list/vehicle-list.component').then(m => m.VehicleListComponent),
    title: 'Vehículos de importación — Logistique Les Lions'
  },
  {
    path: 'nuevo',
    loadComponent: () =>
      import('./vehicle-form/vehicle-form.component').then(m => m.VehicleFormComponent),
    title: 'Publicar vehículo — Logistique Les Lions'
  },
  {
    path: 'publicar',
    redirectTo: 'nuevo',
    pathMatch: 'full'
  },
  {
    path: ':slug',
    loadComponent: () =>
      import('./vehicle-detail/vehicle-detail.component').then(m => m.VehicleDetailComponent),
    title: 'Detalle de vehículo — Logistique Les Lions'
  }
];

// Note: /mis-vehiculos is a top-level route in app.routes.ts
