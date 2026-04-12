import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

interface Inspector {
  name: string;
  location: string;
  country: string;
  flag: string;
  specialties: string[];
  rating: number;
  reviews: number;
  certified: boolean;
}

@Component({
  selector: 'lll-inspectors-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule],
  template: `
    <div class="min-h-screen bg-ivory">
      <!-- Header -->
      <div class="bg-navy py-20 px-4">
        <div class="container mx-auto max-w-4xl text-center">
          <p class="divider-gold justify-center mb-6">Red de confianza</p>
          <h1 class="font-heading text-4xl lg:text-5xl font-bold text-ivory mb-4">
            Inspectores <span class="text-gold">Certificados</span>
          </h1>
          <p class="text-ivory/70 text-lg leading-relaxed max-w-2xl mx-auto">
            Antes de comprar un vehículo importado, nuestros inspectores certificados realizan una revisión técnica exhaustiva in situ. Compra con total confianza.
          </p>
        </div>
      </div>

      <!-- Benefits -->
      <div class="bg-white border-b border-navy/10 py-10 px-4">
        <div class="container mx-auto max-w-5xl">
          <div class="grid sm:grid-cols-3 gap-6 text-center">
            @for (benefit of benefits; track benefit.title) {
              <div class="flex flex-col items-center gap-3">
                <div class="w-12 h-12 bg-gold/10 rounded-xl flex items-center justify-center">
                  <svg class="w-6 h-6 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" [attr.d]="benefit.icon"/>
                  </svg>
                </div>
                <h3 class="font-semibold text-navy text-sm">{{ benefit.title }}</h3>
                <p class="text-navy/50 text-xs">{{ benefit.description }}</p>
              </div>
            }
          </div>
        </div>
      </div>

      <!-- Inspectors grid -->
      <div class="container mx-auto max-w-5xl px-4 py-12">
        <div class="flex items-center justify-between mb-8">
          <h2 class="font-heading text-2xl font-bold text-navy">Inspectores disponibles</h2>
          <span class="text-navy/50 text-sm">{{ inspectors.length }} inspectores certificados</span>
        </div>

        <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-5 mb-12">
          @for (inspector of inspectors; track inspector.name) {
            <div class="bg-white rounded-xl shadow-card hover:shadow-card-hover transition-shadow p-6">
              <div class="flex items-start justify-between mb-4">
                <div class="w-12 h-12 bg-navy/5 rounded-full flex items-center justify-center text-2xl">
                  {{ inspector.flag }}
                </div>
                @if (inspector.certified) {
                  <span class="badge badge-success text-xs">Certificado</span>
                }
              </div>
              <h3 class="font-semibold text-navy mb-1">{{ inspector.name }}</h3>
              <p class="text-navy/50 text-xs mb-3">{{ inspector.location }}, {{ inspector.country }}</p>

              <!-- Rating -->
              <div class="flex items-center gap-1.5 mb-3">
                <div class="flex">
                  @for (star of [1,2,3,4,5]; track star) {
                    <svg [class]="star <= inspector.rating ? 'w-3.5 h-3.5 text-gold' : 'w-3.5 h-3.5 opacity-20 text-navy'" fill="currentColor" viewBox="0 0 20 20">
                      <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                    </svg>
                  }
                </div>
                <span class="text-navy/50 text-xs">({{ inspector.reviews }} valoraciones)</span>
              </div>

              <!-- Specialties -->
              <div class="flex flex-wrap gap-1.5 mb-4">
                @for (spec of inspector.specialties; track spec) {
                  <span class="badge badge-navy text-xs">{{ spec }}</span>
                }
              </div>

              <a routerLink="/tramitacion" class="block text-center btn-primary text-xs py-2 px-4">
                Solicitar inspección
              </a>
            </div>
          }
        </div>

        <!-- Process -->
        <div class="bg-navy rounded-2xl p-8 mb-12 text-white">
          <h2 class="font-heading text-2xl font-bold text-ivory mb-8 text-center">¿Cómo funciona la inspección?</h2>
          <div class="grid sm:grid-cols-4 gap-6">
            @for (step of inspectionSteps; track step.num) {
              <div class="text-center">
                <div class="w-10 h-10 rounded-full bg-gold flex items-center justify-center text-navy font-bold font-heading mx-auto mb-3">
                  {{ step.num }}
                </div>
                <h3 class="font-semibold text-ivory text-sm mb-1">{{ step.title }}</h3>
                <p class="text-ivory/50 text-xs">{{ step.desc }}</p>
              </div>
            }
          </div>
        </div>

        <!-- CTA -->
        <div class="text-center">
          <h2 class="font-heading text-2xl font-bold text-navy mb-3">¿Quieres ser inspector certificado?</h2>
          <p class="text-navy/60 mb-6">Únete a nuestra red de inspectores y ofrece tus servicios a miles de compradores</p>
          <a routerLink="/auth/register" class="btn-gold py-3 px-8">Solicitar certificación</a>
        </div>
      </div>
    </div>
  `
})
export class InspectorsPageComponent {
  readonly benefits = [
    {
      title: 'Informe de 150 puntos',
      description: 'Revisión mecánica, carrocería, historial de accidentes y documentación',
      icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4'
    },
    {
      title: 'Presencia in situ',
      description: 'El inspector acude al vehículo, no al revés. Cobertura en toda Europa',
      icon: 'M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z M15 11a3 3 0 11-6 0 3 3 0 016 0z'
    },
    {
      title: 'Sin conflicto de interés',
      description: 'Trabajamos para el comprador, no para el vendedor. Independencia total',
      icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z'
    }
  ];

  readonly inspectors: Inspector[] = [
    {
      name: 'Hans Mueller',
      location: 'Munich',
      country: 'Alemania',
      flag: '🇩🇪',
      specialties: ['BMW', 'Mercedes', 'Audi'],
      rating: 5,
      reviews: 127,
      certified: true
    },
    {
      name: 'Jean-Pierre Dupont',
      location: 'Lyon',
      country: 'Francia',
      flag: '🇫🇷',
      specialties: ['Peugeot', 'Renault', 'DS'],
      rating: 5,
      reviews: 89,
      certified: true
    },
    {
      name: 'Marco Rossi',
      location: 'Milano',
      country: 'Italia',
      flag: '🇮🇹',
      specialties: ['Ferrari', 'Maserati', 'Alfa Romeo'],
      rating: 4,
      reviews: 63,
      certified: true
    },
    {
      name: 'Carlos Ferreira',
      location: 'Lisboa',
      country: 'Portugal',
      flag: '🇵🇹',
      specialties: ['Multimarca', 'Eléctricos'],
      rating: 5,
      reviews: 45,
      certified: true
    },
    {
      name: 'Mikael Lindqvist',
      location: 'Estocolmo',
      country: 'Suecia',
      flag: '🇸🇪',
      specialties: ['Volvo', 'Koenigsegg'],
      rating: 4,
      reviews: 38,
      certified: true
    },
    {
      name: 'Piotr Kowalski',
      location: 'Varsovia',
      country: 'Polonia',
      flag: '🇵🇱',
      specialties: ['Multimarca', 'Usados'],
      rating: 4,
      reviews: 71,
      certified: true
    }
  ];

  readonly inspectionSteps = [
    { num: 1, title: 'Solicitud online', desc: 'Selecciona el vehículo y el inspector más cercano' },
    { num: 2, title: 'Visita in situ', desc: 'El inspector acude al vehículo en 24-48h' },
    { num: 3, title: 'Informe detallado', desc: 'Recibes el informe en 24h con fotos y valoración' },
    { num: 4, title: 'Decide seguro', desc: 'Compra con total confianza o rechaza el vehículo' }
  ];
}
