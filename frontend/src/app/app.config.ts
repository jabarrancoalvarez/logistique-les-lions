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
import {
  provideRouter,
  withInMemoryScrolling,
  withNavigationErrorHandler,
  withViewTransitions
} from '@angular/router';
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
      }),
      // Cada despliegue renombra los trozos de código que Angular carga bajo demanda.
      // Una pestaña abierta desde antes pide uno que ya no existe, recibe el index.html
      // en su lugar y falla con «Failed to fetch dynamically imported module»: la
      // pantalla se queda rota hasta que alguien recarga a mano.
      //
      // Al detectarlo, se recarga una sola vez. La marca en sessionStorage evita el
      // bucle si el fallo no fuera por un despliegue sino por falta de red.
      withNavigationErrorHandler(error => {
        const mensaje = String((error as { message?: string })?.message ?? error);
        const esTrozoPerdido =
          /dynamically imported module|Importing a module script failed|ChunkLoadError/i
            .test(mensaje);

        if (!esTrozoPerdido || typeof sessionStorage === 'undefined') return;
        if (sessionStorage.getItem('yu_recarga_por_version')) return;

        sessionStorage.setItem('yu_recarga_por_version', '1');
        location.reload();
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
      // Si la aplicación ha arrancado, la recarga por versión nueva funcionó: se borra
      // la marca para que vuelva a estar disponible en el siguiente despliegue.
      if (typeof sessionStorage !== 'undefined') {
        sessionStorage.removeItem('yu_recarga_por_version');
      }
      inject(PlatformService).load().subscribe({ error: () => {} });
    }),

    provideServiceWorker('ngsw-worker.js', {
      enabled: false,
      registrationStrategy: 'registerWhenStable:30000'
    })
  ]
};
