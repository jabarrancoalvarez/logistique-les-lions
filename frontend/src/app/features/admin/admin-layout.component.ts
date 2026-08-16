import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
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
      <!-- Velo oscuro tras el menú abierto en móvil -->
      @if (menuOpen()) {
        <div class="fixed inset-0 bg-navy/50 z-40 lg:hidden" (click)="menuOpen.set(false)"
             aria-hidden="true"></div>
      }

      <!-- Barra lateral: fija en escritorio, cajón deslizable en móvil.
           Antes era w-64 sin más y en móvil se comía media pantalla, dejando el
           contenido en una columna ilegible. -->
      <aside
        class="w-64 bg-navy text-frost flex-col z-50
               fixed inset-y-0 left-0 lg:static
               lg:flex"
        [class.flex]="menuOpen()"
        [class.hidden]="!menuOpen()">
        <div class="px-6 py-5 border-b border-white/10 flex items-center justify-between">
          <div>
            <h1 class="font-heading text-lg font-semibold">Yoon u Auto</h1>
            <p class="text-xs text-frost/50 mt-1">Administration</p>
          </div>
          <button type="button" (click)="menuOpen.set(false)"
                  class="lg:hidden text-frost/70 hover:text-frost text-xl leading-none"
                  aria-label="Fermer le menu">✕</button>
        </div>
        <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
          @for (item of nav; track item.path) {
            <a
              [routerLink]="item.path ? ['/admin', item.path] : ['/admin']"
              [routerLinkActiveOptions]="{ exact: item.path === '' }"
              routerLinkActive="bg-azure text-navy font-semibold"
              (click)="menuOpen.set(false)"
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

      <div class="flex-1 flex flex-col min-w-0">
        <!-- Barra superior solo en móvil, con el botón para abrir el menú -->
        <header class="lg:hidden flex items-center gap-3 bg-navy text-frost px-4 py-3 sticky top-0 z-30">
          <button type="button" (click)="menuOpen.set(true)"
                  class="text-2xl leading-none" aria-label="Ouvrir le menu">☰</button>
          <span class="font-heading font-semibold">Administration</span>
        </header>

        <main class="flex-1 overflow-auto min-w-0">
          <router-outlet />
        </main>
      </div>
    </div>
  `
})
export class AdminLayoutComponent {
  readonly nav = ADMIN_NAV;

  /** El menú lateral es un cajón en móvil; en escritorio siempre está visible. */
  readonly menuOpen = signal(false);
}
