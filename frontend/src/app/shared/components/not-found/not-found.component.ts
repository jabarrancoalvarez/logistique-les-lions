import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'lll-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-[60vh] flex flex-col items-center justify-center text-center px-4 py-20">
      <div class="text-6xl mb-6" aria-hidden="true">🦁</div>
      <h1 class="font-heading text-4xl text-navy font-bold mb-3">Página no encontrada</h1>
      <p class="text-navy/60 mb-8 max-w-sm">
        Esta ruta no existe o ha sido eliminada. Vuelve al inicio para continuar explorando.
      </p>
      <a routerLink="/" class="btn-primary">Volver al inicio</a>
    </div>
  `
})
export class NotFoundComponent {}
