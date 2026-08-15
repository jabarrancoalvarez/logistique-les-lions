import {
  ApplicationConfig,
  provideZoneChangeDetection,
  provideAppInitializer,
  isDevMode,
  inject,
  LOCALE_ID,
  DEFAULT_CURRENCY_CODE
} from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { provideRouter, withInMemoryScrolling, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideServiceWorker } from '@angular/service-worker';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { errorInterceptor } from './core/auth/error.interceptor';
import { YOON_CURRENCY_CODE } from './shared/pipes/fcfa.pipe';
import { PlatformService } from './core/services/platform.service';

// Yoon u Auto es una plataforma monolingüe en francés (Senegal).
registerLocaleData(localeFr);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),

    { provide: LOCALE_ID, useValue: 'fr' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: YOON_CURRENCY_CODE },

    provideRouter(
      routes,
      withViewTransitions(),
      withInMemoryScrolling({
        scrollPositionRestoration: 'top',
        anchorScrolling: 'enabled'
      })
    ),

    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, errorInterceptor])
    ),

    provideAnimationsAsync(),

    // Los parámetros de la plataforma (límite del comparador, funcionalidades activas)
    // se piden una vez al arrancar. No se espera a la respuesta: hasta que llegue valen
    // los valores de respaldo, para no retrasar el primer render por una configuración.
    provideAppInitializer(() => {
      inject(PlatformService).load().subscribe({ error: () => {} });
    }),

    provideServiceWorker('ngsw-worker.js', {
      enabled: false,
      registrationStrategy: 'registerWhenStable:30000'
    })
  ]
};
