import { Routes } from '@angular/router';

export const COMPLIANCE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./compliance-home/compliance-home.component').then(m => m.ComplianceHomeComponent),
    title: 'Tramitación y aduanas — Logistique Les Lions'
  },
  {
    path: 'calculadora',
    loadComponent: () =>
      import('./cost-estimator/cost-estimator.component').then(m => m.CostEstimatorComponent),
    title: 'Calculadora de importación — Logistique Les Lions'
  },
  {
    path: 'procesos/:id',
    loadComponent: () =>
      import('./process-tracker/process-tracker.component').then(m => m.ProcessTrackerComponent),
    title: 'Estado del proceso — Logistique Les Lions'
  }
];
