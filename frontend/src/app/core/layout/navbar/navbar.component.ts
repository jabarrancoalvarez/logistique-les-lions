import {
  Component, HostListener, OnInit, signal, inject, ChangeDetectionStrategy
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'lll-navbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './navbar.component.html'
})
export class NavbarComponent implements OnInit {
  protected readonly auth = inject(AuthService);

  readonly isScrolled = signal(false);
  readonly isMobileMenuOpen = signal(false);
  readonly currentLang = signal('ES');


  @HostListener('window:scroll')
  onScroll(): void {
    this.isScrolled.set(window.scrollY > 48);
  }

  ngOnInit(): void {
    const savedLang = localStorage.getItem('lll-lang') || 'ES';
    this.currentLang.set(savedLang.toUpperCase());
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen.update(v => !v);
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
  }

  changeLang(lang: string): void {
    this.currentLang.set(lang);
    localStorage.setItem('lll-lang', lang.toLowerCase());
  }

  get langs() { return ['ES', 'EN', 'FR', 'DE']; }
}
