import { Routes } from '@angular/router';

export const LEGAL_ROUTES: Routes = [
  {
    path: 'aviso-legal',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'aviso-legal' },
    title: 'Aviso Legal — Yoon U Auto'
  },
  {
    path: 'privacidad',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'privacidad' },
    title: 'Política de Privacidad — Yoon U Auto'
  },
  {
    path: 'cookies',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'cookies' },
    title: 'Política de Cookies — Yoon U Auto'
  },
  {
    path: 'terminos',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'terminos' },
    title: 'Términos y Condiciones — Yoon U Auto'
  },
  {
    path: 'rgpd',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { slug: 'rgpd' },
    title: 'Protección de Datos (RGPD) — Yoon U Auto'
  },
  {
    path: '',
    redirectTo: 'aviso-legal',
    pathMatch: 'full'
  }
];
