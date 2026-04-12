import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './core/layout/navbar/navbar.component';
import { FooterComponent } from './core/layout/footer/footer.component';
import { CookieConsentComponent } from './features/landing/cookie-consent/cookie-consent.component';

@Component({
  selector: 'lll-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, CookieConsentComponent],
  template: `
    <div class="flex flex-col min-h-screen">
      <lll-navbar />
      <main class="flex-1">
        <router-outlet />
      </main>
      <lll-footer />
      <lll-cookie-consent />
    </div>
  `
})
export class AppComponent implements OnInit {
  ngOnInit(): void {
    // Aplicar tema guardado
    const savedTheme = localStorage.getItem('lll-theme');
    if (savedTheme === 'dark') {
      document.documentElement.classList.add('dark');
    }
  }
}
