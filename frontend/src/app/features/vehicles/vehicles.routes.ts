import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';

export const VEHICLES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./vehicle-list/vehicle-list.component').then(m => m.VehicleListComponent),
    title: 'Voitures d’occasion au Sénégal — Yoon u Auto'
  },
  {
    path: 'nuevo',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./vehicle-form/vehicle-form.component').then(m => m.VehicleFormComponent),
    title: 'Publier une annonce — Yoon u Auto'
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
    title: 'Annonce — Yoon u Auto'
  }
];

// Note: /mis-vehiculos is a top-level route in app.routes.ts
