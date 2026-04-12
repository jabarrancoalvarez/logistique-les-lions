import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'lll-dealers',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="min-h-screen bg-ivory/50">
      <div class="container mx-auto px-4 py-20 text-center max-w-2xl">
        <div class="w-20 h-20 bg-gold/10 rounded-full flex items-center justify-center mx-auto mb-6">
          <svg class="w-10 h-10 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
          </svg>
        </div>
        <h1 class="font-heading text-3xl font-bold text-navy mb-4">Concesionarios</h1>
        <p class="text-navy/60 mb-8 leading-relaxed">
          El directorio de concesionarios especializados en importación internacional está en desarrollo.
          Si eres un dealer, regístrate ahora y aparece en el directorio cuando se lance.
        </p>
        <div class="flex flex-col sm:flex-row gap-3 justify-center">
          <a routerLink="/auth/register" class="btn-gold py-3 px-6">Registrarme como dealer</a>
          <a routerLink="/vehiculos" class="btn-outline py-3 px-6">Ver vehículos disponibles</a>
        </div>
      </div>
    </div>
  `
})
export class DealersComponent {}
