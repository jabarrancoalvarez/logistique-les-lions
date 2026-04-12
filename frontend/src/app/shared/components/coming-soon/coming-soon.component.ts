import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'lll-coming-soon',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-ivory/50 flex items-center">
      <div class="container mx-auto px-4 py-20 text-center max-w-xl">
        <div class="w-20 h-20 bg-navy/5 rounded-full flex items-center justify-center mx-auto mb-6">
          <svg class="w-10 h-10 text-navy/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
              d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </div>
        <h1 class="font-heading text-3xl font-bold text-navy mb-3">{{ title() }}</h1>
        <p class="text-navy/60 mb-8 leading-relaxed">
          Este módulo está en desarrollo y estará disponible próximamente.
        </p>
        <a routerLink="/" class="btn-gold py-3 px-6">Volver al inicio</a>
      </div>
    </div>
  `
})
export class ComingSoonComponent {
  title = input('Próximamente');
}
