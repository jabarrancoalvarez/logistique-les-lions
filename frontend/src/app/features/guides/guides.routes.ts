import { Routes } from '@angular/router';

export const GUIDES_ROUTES: Routes = [
  {
    path: 'importacion',
    loadComponent: () => import('./guide-page.component').then(m => m.GuidePageComponent),
    data: { slug: 'importacion' },
    title: 'Guía de Importación — Logistique Les Lions'
  },
  {
    path: 'exportacion',
    loadComponent: () => import('./guide-page.component').then(m => m.GuidePageComponent),
    data: { slug: 'exportacion' },
    title: 'Guía de Exportación — Logistique Les Lions'
  },
  {
    path: 'homologacion',
    loadComponent: () => import('./guide-page.component').then(m => m.GuidePageComponent),
    data: { slug: 'homologacion' },
    title: 'Homologaciones UE — Logistique Les Lions'
  },
  {
    path: '',
    redirectTo: 'importacion',
    pathMatch: 'full'
  }
];
