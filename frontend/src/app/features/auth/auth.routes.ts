import { Routes } from '@angular/router';
import { guestGuard } from '@core/auth/auth.guard';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent),
    title: 'Iniciar sesión — Logistique Les Lions'
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./register/register.component').then(m => m.RegisterComponent),
    title: 'Crear cuenta — Logistique Les Lions'
  }
];
