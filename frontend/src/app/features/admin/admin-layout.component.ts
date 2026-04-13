import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

interface AdminNavItem {
  readonly path: string;
  readonly label: string;
  readonly icon: string;
}

const ADMIN_NAV: AdminNavItem[] = [
  { path: '',              label: 'Dashboard',     icon: '📊' },
  { path: 'vehiculos',     label: 'Vehículos',     icon: '🚗' },
  { path: 'procesos',      label: 'Procesos',      icon: '📦' },
  { path: 'incidencias',   label: 'Incidencias',   icon: '⚠️' },
  { path: 'partners',      label: 'Marketplace',   icon: '🤝' },
  { path: 'usuarios',      label: 'Usuarios',      icon: '👥' },
  { path: 'notificaciones',label: 'Notificaciones',icon: '🔔' },
];

@Component({
  selector: 'lll-admin-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="flex min-h-screen bg-slate-50">
      <aside class="w-64 bg-slate-900 text-slate-100 flex flex-col">
        <div class="px-6 py-5 border-b border-slate-800">
          <h1 class="text-lg font-semibold">Logistique Les Lions</h1>
          <p class="text-xs text-slate-400 mt-1">Panel admin</p>
        </div>
        <nav class="flex-1 px-3 py-4 space-y-1">
          @for (item of nav; track item.path) {
            <a
              [routerLink]="item.path ? ['/admin', item.path] : ['/admin']"
              [routerLinkActiveOptions]="{ exact: item.path === '' }"
              routerLinkActive="bg-amber-500 text-slate-900 font-semibold"
              class="flex items-center gap-3 px-3 py-2 rounded-md text-sm hover:bg-slate-800 transition-colors">
              <span class="text-base">{{ item.icon }}</span>
              <span>{{ item.label }}</span>
            </a>
          }
        </nav>
        <div class="px-4 py-3 border-t border-slate-800 text-xs text-slate-500">
          v1.0 — admin
        </div>
      </aside>

      <main class="flex-1 overflow-auto">
        <router-outlet />
      </main>
    </div>
  `
})
export class AdminLayoutComponent {
  readonly nav = ADMIN_NAV;
}
