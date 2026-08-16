import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

interface AdminNavItem {
  readonly path: string;
  readonly label: string;
  readonly icon: string;
}

/**
 * Secciones del backoffice.
 *
 * Las del documento se van incorporando parte a parte; las heredadas del producto
 * anterior (procesos, incidencias, partners) siguen aquí hasta que se decida su destino.
 */
const ADMIN_NAV: AdminNavItem[] = [
  { path: '',              label: 'Tableau de bord', icon: '📊' },
  { path: 'vehiculos',     label: 'Annonces',        icon: '🚗' },
  { path: 'demandes',      label: 'Demandes',        icon: '🔎' },
  { path: 'negociations',  label: 'Négociations',    icon: '💬' },
  { path: 'contrats',      label: 'Contrats',        icon: '📄' },
  { path: 'moderation',    label: 'Modération',      icon: '🚩' },
  { path: 'usuarios',      label: 'Utilisateurs',    icon: '👥' },
  { path: 'notificaciones',label: 'Communications',  icon: '🔔' },
  { path: 'statistiques',  label: 'Statistiques',    icon: '📈' },
  { path: 'configuration', label: 'Configuration',   icon: '⚙️' },
];

@Component({
  selector: 'lll-admin-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="flex min-h-screen bg-frost">
      <aside class="w-64 bg-navy text-frost flex flex-col">
        <div class="px-6 py-5 border-b border-white/10">
          <h1 class="font-heading text-lg font-semibold">Yoon u Auto</h1>
          <p class="text-xs text-frost/50 mt-1">Administration</p>
        </div>
        <nav class="flex-1 px-3 py-4 space-y-1">
          @for (item of nav; track item.path) {
            <a
              [routerLink]="item.path ? ['/admin', item.path] : ['/admin']"
              [routerLinkActiveOptions]="{ exact: item.path === '' }"
              routerLinkActive="bg-azure text-navy font-semibold"
              class="flex items-center gap-3 px-3 py-2 rounded-md text-sm hover:bg-white/5 transition-colors">
              <span class="text-base">{{ item.icon }}</span>
              <span>{{ item.label }}</span>
            </a>
          }
        </nav>
        <div class="px-4 py-3 border-t border-white/10 text-xs text-frost/40">
          <a routerLink="/" class="hover:text-azure-light">← Retour au site</a>
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
