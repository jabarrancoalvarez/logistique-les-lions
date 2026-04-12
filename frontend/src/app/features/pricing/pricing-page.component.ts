import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

interface PricingPlan {
  name: string;
  price: string;
  period: string;
  description: string;
  features: string[];
  highlighted: boolean;
  cta: string;
}

interface MarketFee {
  category: string;
  fee: string;
  description: string;
}

@Component({
  selector: 'lll-pricing-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule],
  template: `
    <div class="min-h-screen bg-ivory">
      <!-- Header -->
      <div class="bg-navy py-20 px-4 text-center">
        <div class="container mx-auto max-w-2xl">
          <p class="divider-gold justify-center mb-6">Precios transparentes</p>
          <h1 class="font-heading text-4xl lg:text-5xl font-bold text-ivory mb-4">
            Sin sorpresas.<br><span class="text-gold">Sin comisiones ocultas.</span>
          </h1>
          <p class="text-lg leading-relaxed" style="color: rgba(248,246,240,0.7)">
            Elige el plan que mejor se adapta a tus necesidades. Todos incluyen acceso completo a la plataforma.
          </p>
        </div>
      </div>

      <!-- Plans -->
      <div class="container mx-auto max-w-5xl px-4 py-16">
        <div class="grid md:grid-cols-3 gap-6 mb-16">
          @for (plan of plans; track plan.name) {
            <div [class]="cardClass(plan)">
              @if (plan.highlighted) {
                <div class="badge badge-gold self-start mb-4">Más popular</div>
              }
              <h2 [class]="plan.highlighted ? 'font-heading text-xl font-bold mb-1 text-ivory' : 'font-heading text-xl font-bold mb-1 text-navy'">
                {{ plan.name }}
              </h2>
              <p [class]="plan.highlighted ? 'text-sm mb-6 opacity-60 text-ivory' : 'text-sm mb-6 opacity-50 text-navy'">
                {{ plan.description }}
              </p>
              <div class="mb-6">
                <span [class]="plan.highlighted ? 'font-heading text-4xl font-bold text-gold' : 'font-heading text-4xl font-bold text-navy'">
                  {{ plan.price }}
                </span>
                <span [class]="plan.highlighted ? 'text-sm ml-1 opacity-50 text-ivory' : 'text-sm ml-1 opacity-40 text-navy'">
                  {{ plan.period }}
                </span>
              </div>
              <ul class="space-y-3 mb-8 flex-1">
                @for (feature of plan.features; track feature) {
                  <li class="flex items-start gap-2 text-sm">
                    <svg
                      [class]="plan.highlighted ? 'w-4 h-4 shrink-0 mt-0.5 text-gold' : 'w-4 h-4 shrink-0 mt-0.5 text-gold-dark'"
                      fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                    </svg>
                    <span [class]="plan.highlighted ? 'opacity-80 text-ivory' : 'opacity-70 text-navy'">{{ feature }}</span>
                  </li>
                }
              </ul>
              <a
                routerLink="/auth/register"
                [class]="ctaClass(plan)"
              >{{ plan.cta }}</a>
            </div>
          }
        </div>

        <!-- Market fees -->
        <div class="mb-16">
          <h2 class="font-heading text-2xl font-bold text-navy mb-2 text-center">Tarifas de tramitación</h2>
          <p class="text-center mb-8" style="color: rgba(11,31,58,0.6)">Servicios adicionales con precio fijo, sin sorpresas</p>
          <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
            @for (fee of fees; track fee.category) {
              <div class="bg-white rounded-xl p-6 shadow-card">
                <div class="flex items-center justify-between mb-2">
                  <h3 class="font-semibold text-navy text-sm">{{ fee.category }}</h3>
                  <span class="font-heading font-bold text-gold text-lg">{{ fee.fee }}</span>
                </div>
                <p class="text-xs" style="color: rgba(11,31,58,0.5)">{{ fee.description }}</p>
              </div>
            }
          </div>
        </div>

        <!-- FAQ -->
        <div class="bg-white rounded-2xl p-8 shadow-card mb-12">
          <h2 class="font-heading text-2xl font-bold text-navy mb-8">Preguntas frecuentes sobre precios</h2>
          <div class="space-y-6">
            @for (faq of faqs; track faq.q) {
              <div class="border-b pb-6 last:border-0 last:pb-0" style="border-color: rgba(11,31,58,0.1)">
                <h3 class="font-semibold text-navy mb-2">{{ faq.q }}</h3>
                <p class="text-sm leading-relaxed" style="color: rgba(11,31,58,0.6)">{{ faq.a }}</p>
              </div>
            }
          </div>
        </div>

        <!-- CTA -->
        <div class="text-center">
          <h2 class="font-heading text-2xl font-bold text-navy mb-3">¿Tienes dudas?</h2>
          <p class="mb-6" style="color: rgba(11,31,58,0.6)">Habla con nuestro equipo y te ayudamos a elegir el mejor plan</p>
          <div class="flex flex-wrap gap-3 justify-center">
            <a routerLink="/tramitacion" class="btn-primary py-3 px-6">Ver servicios</a>
            <a routerLink="/vehiculos" class="btn-outline py-3 px-6">Buscar vehículos</a>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PricingPageComponent {
  cardClass(plan: PricingPlan): string {
    const base = 'rounded-2xl p-8 flex flex-col';
    return plan.highlighted
      ? `${base} bg-navy shadow-navy ring-2 ring-gold`
      : `${base} bg-white shadow-card`;
  }

  ctaClass(plan: PricingPlan): string {
    const base = 'text-center py-3 px-6 rounded-lg font-semibold text-sm transition-all duration-200 block';
    return plan.highlighted
      ? `${base} bg-gold text-navy hover:bg-gold-dark`
      : `${base} bg-navy text-ivory hover:bg-navy-light`;
  }

  readonly plans: PricingPlan[] = [
    {
      name: 'Particular',
      price: 'Gratis',
      period: 'siempre',
      description: 'Perfecto para compradores y vendedores ocasionales',
      highlighted: false,
      cta: 'Empezar gratis',
      features: [
        'Hasta 3 anuncios activos',
        'Búsqueda avanzada de vehículos',
        'Contacto con vendedores',
        'Guardado de favoritos',
        'Alertas de nuevos vehículos',
        'Soporte por email'
      ]
    },
    {
      name: 'Profesional',
      price: '49€',
      period: '/ mes',
      description: 'Ideal para importadores y dealers independientes',
      highlighted: true,
      cta: 'Empezar gratis 14 días',
      features: [
        'Anuncios ilimitados',
        'Posición destacada en búsquedas',
        'Acceso a calculadora de importación',
        'Seguimiento de procesos en tiempo real',
        'Estadísticas de tus anuncios',
        'Soporte prioritario por chat',
        'Badge de vendedor verificado'
      ]
    },
    {
      name: 'Concesionario',
      price: '199€',
      period: '/ mes',
      description: 'Para concesionarios y empresas de importación',
      highlighted: false,
      cta: 'Contactar con ventas',
      features: [
        'Todo lo del plan Profesional',
        'Página de perfil de concesionario',
        'API para importación masiva de stock',
        'Multi-usuario (hasta 10 usuarios)',
        'Informes mensuales de mercado',
        'Gestor de cuenta dedicado',
        'Integraciones con CRM'
      ]
    }
  ];

  readonly fees: MarketFee[] = [
    {
      category: 'Tramitación documental básica',
      fee: '299€',
      description: 'Gestión de todos los documentos para importación desde la UE'
    },
    {
      category: 'Tramitación extra-UE',
      fee: '499€',
      description: 'Documentación para importación desde fuera de la Unión Europea'
    },
    {
      category: 'Homologación individual',
      fee: 'Desde 800€',
      description: 'Proceso completo de homologación con laboratorio certificado'
    },
    {
      category: 'Gestión aduanera',
      fee: '350€',
      description: 'Despacho aduanero completo incluido asesoramiento fiscal'
    },
    {
      category: 'Inspección pre-compra',
      fee: '150€',
      description: 'Informe técnico completo del vehículo antes de la compra'
    },
    {
      category: 'Transporte EU → España',
      fee: 'Desde 450€',
      description: 'Transporte en camión portacoches según origen y distancia'
    }
  ];

  readonly faqs = [
    {
      q: '¿Puedo cambiar de plan en cualquier momento?',
      a: 'Sí, puedes actualizar o cancelar tu plan en cualquier momento. Si actualizas, el cambio es inmediato. Si cancelas, mantienes las funcionalidades hasta el final del período pagado.'
    },
    {
      q: '¿Las tarifas de tramitación están incluidas en los planes?',
      a: 'No. Los planes mensuales cubren el acceso a la plataforma. Los servicios de tramitación, homologación y transporte son servicios adicionales con coste fijo independiente del plan.'
    },
    {
      q: '¿Existe alguna comisión por la venta de un vehículo?',
      a: 'No cobramos comisión sobre las transacciones. El precio que pagas por los planes es todo lo que cobramos por el acceso a la plataforma.'
    },
    {
      q: '¿Ofrecéis descuentos por pago anual?',
      a: 'Sí, el pago anual tiene un descuento del 20% sobre el precio mensual. Puedes cambiar al plan anual desde tu panel de usuario.'
    }
  ];
}
