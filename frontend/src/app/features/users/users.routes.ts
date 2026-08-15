import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';

export const USERS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent),
    title: 'Mon profil — Yoon u Auto'
  }
];
