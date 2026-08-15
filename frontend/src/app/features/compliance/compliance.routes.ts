import { Routes } from '@angular/router';

export const COMPLIANCE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./compliance-home/compliance-home.component').then(m => m.ComplianceHomeComponent),
    title: 'Tramitación y aduanas — Yoon U Auto'
  },
  {
    path: 'calculadora',
    loadComponent: () =>
      import('./cost-estimator/cost-estimator.component').then(m => m.CostEstimatorComponent),
    title: 'Calculadora de importación — Yoon U Auto'
  },
  {
    path: 'procesos/:id',
    loadComponent: () =>
      import('./process-tracker/process-tracker.component').then(m => m.ProcessTrackerComponent),
    title: 'Estado del proceso — Yoon U Auto'
  }
];
