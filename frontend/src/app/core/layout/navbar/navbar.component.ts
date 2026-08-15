import {
  Component, HostListener, signal, inject, ChangeDetectionStrategy
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../auth/auth.service';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

/** Un enlace del menú. */
interface NavItem {
  label: string;
  link: string;
}

/**
 * Navegación pública: lo que necesita un visitante.
 */
const PUBLIC_NAV: readonly NavItem[] = [
  { label: 'Voitures',                link: '/vehiculos' },
  { label: 'Vendre',                  link: '/vehiculos/nuevo' },
  { label: 'Trouvez-moi une voiture', link: '/mis-pedidos' }
];

/**
 * Los cuatro espacios personales del usuario autenticado.
 *
 * Representan el ciclo real: lo que quiero comprar, lo que estoy negociando, lo que ya
 * tengo y lo que estoy vendiendo.
 */
const PERSONAL_NAV: readonly NavItem[] = [
  { label: 'Mes recherches',   link: '/mis-busquedas' },
  { label: 'Mes négociations', link: '/mis-negociaciones' },
  { label: 'Mon Garage',       link: '/mi-garaje' },
  { label: 'Mes annonces',     link: '/mis-vehiculos' }
];

@Component({
  selector: 'lll-navbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, CommonModule, NotificationBellComponent],
  templateUrl: './navbar.component.html'
})
export class NavbarComponent {
  protected readonly auth = inject(AuthService);

  readonly isScrolled = signal(false);
  readonly isMobileMenuOpen = signal(false);

  readonly publicNav = PUBLIC_NAV;
  readonly personalNav = PERSONAL_NAV;

  @HostListener('window:scroll')
  onScroll(): void {
    this.isScrolled.set(window.scrollY > 48);
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen.update(v => !v);
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
  }
}
