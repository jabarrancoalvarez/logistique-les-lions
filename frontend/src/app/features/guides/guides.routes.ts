import { Routes } from '@angular/router';

export const GUIDES_ROUTES: Routes = [
  {
    path: 'importacion',
    loadComponent: () => import('./guide-page.component').then(m => m.GuidePageComponent),
    data: { slug: 'importacion' },
    title: 'Guía de Importación — Yoon U Auto'
  },
  {
    path: 'exportacion',
    loadComponent: () => import('./guide-page.component').then(m => m.GuidePageComponent),
    data: { slug: 'exportacion' },
    title: 'Guía de Exportación — Yoon U Auto'
  },
  {
    path: 'homologacion',
    loadComponent: () => import('./guide-page.component').then(m => m.GuidePageComponent),
    data: { slug: 'homologacion' },
    title: 'Homologaciones UE — Yoon U Auto'
  },
  {
    path: '',
    redirectTo: 'importacion',
    pathMatch: 'full'
  }
];
