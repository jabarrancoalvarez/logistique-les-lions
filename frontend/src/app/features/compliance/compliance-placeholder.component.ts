import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'lll-compliance-placeholder',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="container mx-auto px-4 py-20 text-center">
      <h1 class="font-heading text-3xl text-navy mb-4">Tramitación Internacional</h1>
      <p class="text-navy/60 mb-8">Módulo en desarrollo. Próxima iteración.</p>
      <a routerLink="/" class="btn-primary">Volver al inicio</a>
    </div>
  `
})
export class CompliancePlaceholderComponent {}
