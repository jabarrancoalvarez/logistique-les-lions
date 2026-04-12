import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';

export const MESSAGING_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./inbox/inbox.component').then(m => m.InboxComponent),
    title: 'Mensajes — Logistique Les Lions'
  },
  {
    path: ':id',
    canActivate: [authGuard],
    loadComponent: () => import('./conversation/conversation.component').then(m => m.ConversationComponent),
    title: 'Conversación — Logistique Les Lions'
  }
];
