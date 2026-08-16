import { Routes } from '@angular/router';

export const LANDING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./landing-page/landing-page.component').then(m => m.LandingPageComponent),
    title: 'Yoon u Auto — Achat et vente de véhicules au Sénégal'
  }
];
