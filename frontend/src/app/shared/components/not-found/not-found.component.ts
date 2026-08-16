import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Página de ruta inexistente. Es de las pocas pantallas que ve alguien que llega
 * desde un enlace roto o caducado, así que ofrece salidas en vez de solo disculparse.
 */
@Component({
  selector: 'lll-not-found',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="min-h-[60vh] flex flex-col items-center justify-center text-center px-4 py-20">
      <div class="text-6xl mb-6" aria-hidden="true">🛣️</div>
      <h1 class="font-heading text-4xl text-navy font-bold mb-3">Page introuvable</h1>
      <p class="text-navy/60 mb-8 max-w-sm">
        Cette page n'existe pas ou n'est plus disponible. Le lien est peut-être ancien
        ou l'annonce a été retirée.
      </p>
      <div class="flex flex-wrap gap-3 justify-center">
        <a routerLink="/vehiculos" class="btn-primary">Voir les véhicules</a>
        <a routerLink="/" class="btn-outline">Retour à l'accueil</a>
      </div>
    </div>
  `
})
export class NotFoundComponent {}
