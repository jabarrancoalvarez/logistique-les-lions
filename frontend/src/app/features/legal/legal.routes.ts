import { Routes } from '@angular/router';

export const LEGAL_ROUTES: Routes = [
  {
    path: 'aviso-legal',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'aviso-legal' },
    title: 'Aviso Legal — Logistique Les Lions'
  },
  {
    path: 'privacidad',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'privacidad' },
    title: 'Política de Privacidad — Logistique Les Lions'
  },
  {
    path: 'cookies',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'cookies' },
    title: 'Política de Cookies — Logistique Les Lions'
  },
  {
    path: 'terminos',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'terminos' },
    title: 'Términos y Condiciones — Logistique Les Lions'
  },
  {
    path: 'rgpd',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'rgpd' },
    title: 'Protección de Datos (RGPD) — Logistique Les Lions'
  },
  {
    path: '',
    redirectTo: 'aviso-legal',
    pathMatch: 'full'
  }
];
