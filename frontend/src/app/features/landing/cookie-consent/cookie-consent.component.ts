import { Component, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';

interface CookiePreferences {
  necessary: boolean;
  analytics: boolean;
  marketing: boolean;
}

type PrefKey = keyof CookiePreferences;

@Component({
  selector: 'lll-cookie-consent',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, NgClass],
  templateUrl: './cookie-consent.component.html'
})
export class CookieConsentComponent implements OnInit {
  readonly isVisible = signal(false);
  readonly showDetails = signal(false);

  readonly preferences = signal<CookiePreferences>({
    necessary: true,
    analytics: false,
    marketing: false
  });

  readonly cookiePrefs: { key: PrefKey; label: string; desc: string; alwaysOn: boolean }[] = [
    { key: 'necessary', label: 'Nécessaires', desc: 'Indispensables au fonctionnement du site.', alwaysOn: true },
    { key: 'analytics', label: 'Analytiques', desc: 'Nous aident à comprendre comment le site est utilisé.', alwaysOn: false },
    { key: 'marketing', label: 'Marketing', desc: 'Pour afficher des annonces personnalisées.', alwaysOn: false }
  ];

  private readonly STORAGE_KEY = 'lll-cookie-consent';

  ngOnInit(): void {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (!saved) {
      setTimeout(() => this.isVisible.set(true), 1500);
    }
  }

  isPrefEnabled(key: PrefKey): boolean {
    return this.preferences()[key];
  }

  toggleDetails(): void {
    this.showDetails.update(v => !v);
  }

  acceptAll(): void {
    this.save({ necessary: true, analytics: true, marketing: true });
  }

  acceptNecessary(): void {
    this.save({ necessary: true, analytics: false, marketing: false });
  }

  savePreferences(): void {
    this.save(this.preferences());
  }

  togglePref(key: PrefKey): void {
    if (key === 'necessary') return;
    this.preferences.update(p => ({ ...p, [key]: !p[key] }));
  }

  private save(prefs: CookiePreferences): void {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify({ ...prefs, savedAt: new Date().toISOString() }));
    this.isVisible.set(false);
  }
}
