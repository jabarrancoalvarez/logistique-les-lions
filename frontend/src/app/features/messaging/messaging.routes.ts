import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';

export const MESSAGING_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./inbox/inbox.component').then(m => m.InboxComponent),
    title: 'Messages — Yoon u Auto'
  },
  {
    path: ':id',
    canActivate: [authGuard],
    loadComponent: () => import('./conversation/conversation.component').then(m => m.ConversationComponent),
    title: 'Conversation — Yoon u Auto'
  }
];
