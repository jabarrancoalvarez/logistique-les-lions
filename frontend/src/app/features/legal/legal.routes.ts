import { Routes } from '@angular/router';

export const LEGAL_ROUTES: Routes = [
  {
    path: 'aviso-legal',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'aviso-legal' },
    title: 'Mentions légales — Yoon u Auto'
  },
  {
    path: 'privacidad',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'privacidad' },
    title: 'Politique de confidentialité — Yoon u Auto'
  },
  {
    path: 'cookies',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'cookies' },
    title: 'Politique de cookies — Yoon u Auto'
  },
  {
    path: 'terminos',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'terminos' },
    title: 'Conditions générales — Yoon u Auto'
  },
  {
    path: 'rgpd',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'rgpd' },
    title: 'Protection des données (RGPD) — Yoon u Auto'
  },
  {
    path: '',
    redirectTo: 'aviso-legal',
    pathMatch: 'full'
  }
];
