import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

interface Route {
  from: string;
  fromFlag: string;
  to: string;
  toFlag: string;
  days: string;
  price: string;
  method: string;
}

interface ServiceCard {
  icon: string;
  title: string;
  description: string;
  features: string[];
  tag: string;
}

@Component({
  selector: 'lll-transport-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule],
  template: `
    <div class="min-h-screen bg-ivory">

      <!-- ── Hero ───────────────────────────────────────────────────────── -->
      <div class="bg-navy py-20 px-4">
        <div class="container mx-auto max-w-5xl">
          <div class="grid lg:grid-cols-2 gap-12 items-center">
            <div>
              <p class="divider-gold mb-6">Logística internacional</p>
              <h1 class="font-heading text-4xl lg:text-5xl font-bold text-ivory mb-4 leading-tight">
                Transportamos tu vehículo<br>
                <span class="text-gold">a cualquier destino</span>
              </h1>
              <p class="text-lg leading-relaxed mb-8" style="color:rgba(248,246,240,0.7)">
                Red de transporte puerta a puerta en toda Europa y transporte marítimo a más de 50 países.
                Seguimiento en tiempo real y seguro incluido.
              </p>
              <div class="flex flex-wrap gap-3">
                <a routerLink="/vehiculos" class="btn-gold py-3 px-6">Ver vehículos disponibles</a>
                <a routerLink="/tramitacion" class="btn-outline btn-outline-gold py-3 px-6">Solicitar presupuesto</a>
              </div>
            </div>
            <!-- Stats -->
            <div class="grid grid-cols-2 gap-4">
              @for (stat of heroStats; track stat.label) {
                <div class="bg-white/5 border border-white/10 rounded-xl p-5 text-center">
                  <p class="font-heading text-3xl font-bold text-gold mb-1">{{ stat.value }}</p>
                  <p class="text-xs" style="color:rgba(248,246,240,0.6)">{{ stat.label }}</p>
                </div>
              }
            </div>
          </div>
        </div>
      </div>

      <!-- ── Services ───────────────────────────────────────────────────── -->
      <div class="container mx-auto max-w-5xl px-4 py-16">
        <div class="text-center mb-12">
          <p class="divider-gold justify-center mb-4">Nuestros servicios</p>
          <h2 class="font-heading text-3xl font-bold text-navy">Soluciones de transporte a medida</h2>
        </div>
        <div class="grid md:grid-cols-3 gap-6 mb-16">
          @for (svc of services; track svc.title) {
            <div class="bg-white rounded-2xl p-7 shadow-card hover:shadow-card-hover transition-all duration-300 group">
              <div class="w-12 h-12 bg-gold/10 rounded-xl flex items-center justify-center mb-5 group-hover:bg-gold/20 transition-colors">
                <svg class="w-6 h-6 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" [attr.d]="svc.icon"/>
                </svg>
              </div>
              <div class="badge badge-navy mb-3">{{ svc.tag }}</div>
              <h3 class="font-heading font-bold text-navy text-lg mb-2">{{ svc.title }}</h3>
              <p class="text-sm leading-relaxed mb-5" style="color:rgba(11,31,58,0.6)">{{ svc.description }}</p>
              <ul class="space-y-2">
                @for (feat of svc.features; track feat) {
                  <li class="flex items-center gap-2 text-xs" style="color:rgba(11,31,58,0.7)">
                    <svg class="w-3.5 h-3.5 text-gold shrink-0" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                    </svg>
                    {{ feat }}
                  </li>
                }
              </ul>
            </div>
          }
        </div>

        <!-- ── Routes table ────────────────────────────────────────────── -->
        <div class="bg-white rounded-2xl shadow-card overflow-hidden mb-16">
          <div class="px-8 py-6 border-b" style="border-color:rgba(11,31,58,0.08)">
            <h2 class="font-heading text-xl font-bold text-navy">Rutas más solicitadas</h2>
            <p class="text-sm mt-1" style="color:rgba(11,31,58,0.5)">Precios orientativos. Solicita un presupuesto exacto para tu vehículo.</p>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full">
              <thead>
                <tr style="background:rgba(11,31,58,0.03)">
                  <th class="text-left px-6 py-3 text-xs font-semibold uppercase tracking-wider" style="color:rgba(11,31,58,0.5)">Origen</th>
                  <th class="text-left px-6 py-3 text-xs font-semibold uppercase tracking-wider" style="color:rgba(11,31,58,0.5)">Destino</th>
                  <th class="text-left px-6 py-3 text-xs font-semibold uppercase tracking-wider" style="color:rgba(11,31,58,0.5)">Método</th>
                  <th class="text-left px-6 py-3 text-xs font-semibold uppercase tracking-wider" style="color:rgba(11,31,58,0.5)">Plazo</th>
                  <th class="text-right px-6 py-3 text-xs font-semibold uppercase tracking-wider" style="color:rgba(11,31,58,0.5)">Precio desde</th>
                </tr>
              </thead>
              <tbody>
                @for (route of routes; track route.from + route.to; let odd = $odd) {
                  <tr [class]="odd ? '' : 'bg-navy/[0.02]'" style="border-bottom:1px solid rgba(11,31,58,0.06)">
                    <td class="px-6 py-4">
                      <span class="text-lg mr-2">{{ route.fromFlag }}</span>
                      <span class="text-sm font-medium text-navy">{{ route.from }}</span>
                    </td>
                    <td class="px-6 py-4">
                      <span class="text-lg mr-2">{{ route.toFlag }}</span>
                      <span class="text-sm font-medium text-navy">{{ route.to }}</span>
                    </td>
                    <td class="px-6 py-4">
                      <span class="badge badge-navy text-xs">{{ route.method }}</span>
                    </td>
                    <td class="px-6 py-4 text-sm" style="color:rgba(11,31,58,0.6)">{{ route.days }}</td>
                    <td class="px-6 py-4 text-right">
                      <span class="font-heading font-bold text-gold">{{ route.price }}</span>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>

        <!-- ── How it works ────────────────────────────────────────────── -->
        <div class="mb-16">
          <h2 class="font-heading text-2xl font-bold text-navy mb-10 text-center">¿Cómo funciona?</h2>
          <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
            @for (step of steps; track step.num) {
              <div class="relative">
                @if (!$last) {
                  <div class="hidden lg:block absolute top-5 left-full w-full h-px" style="background:rgba(201,168,76,0.3);z-index:0"></div>
                }
                <div class="relative z-10 text-center">
                  <div class="w-10 h-10 rounded-full bg-gold flex items-center justify-center text-navy font-bold font-heading mx-auto mb-4">
                    {{ step.num }}
                  </div>
                  <h3 class="font-semibold text-navy text-sm mb-2">{{ step.title }}</h3>
                  <p class="text-xs leading-relaxed" style="color:rgba(11,31,58,0.6)">{{ step.desc }}</p>
                </div>
              </div>
            }
          </div>
        </div>

        <!-- ── Tracking feature ────────────────────────────────────────── -->
        <div class="bg-navy rounded-2xl p-8 lg:p-12 mb-16">
          <div class="grid lg:grid-cols-2 gap-10 items-center">
            <div>
              <p class="divider-gold mb-5">Seguimiento en tiempo real</p>
              <h2 class="font-heading text-2xl lg:text-3xl font-bold text-ivory mb-4">
                Sabe dónde está tu vehículo en todo momento
              </h2>
              <p class="leading-relaxed mb-6" style="color:rgba(248,246,240,0.7)">
                Accede a la plataforma desde cualquier dispositivo y consulta el estado exacto de tu envío.
                Recibirás notificaciones en cada etapa del trayecto.
              </p>
              <ul class="space-y-3">
                @for (item of trackingFeatures; track item) {
                  <li class="flex items-center gap-3 text-sm" style="color:rgba(248,246,240,0.8)">
                    <svg class="w-4 h-4 text-gold shrink-0" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                    </svg>
                    {{ item }}
                  </li>
                }
              </ul>
            </div>
            <!-- Mock tracker -->
            <div class="bg-white/5 border border-white/10 rounded-xl p-6 space-y-4">
              @for (event of mockTracking; track event.time) {
                <div class="flex gap-4 items-start">
                  <div class="flex flex-col items-center">
                    <div [class]="event.done ? 'w-3 h-3 rounded-full bg-gold' : 'w-3 h-3 rounded-full border-2 border-white/30'"></div>
                    @if (!$last) {
                      <div class="w-px flex-1 mt-1" style="height:28px;background:rgba(255,255,255,0.15)"></div>
                    }
                  </div>
                  <div class="pb-4">
                    <p [class]="event.done ? 'text-sm font-medium text-ivory' : 'text-sm text-ivory/40'">{{ event.label }}</p>
                    <p class="text-xs mt-0.5" style="color:rgba(248,246,240,0.4)">{{ event.time }}</p>
                  </div>
                </div>
              }
            </div>
          </div>
        </div>

        <!-- ── Insurance ───────────────────────────────────────────────── -->
        <div class="bg-gold/5 border border-gold/20 rounded-2xl p-8 mb-16">
          <div class="flex flex-col lg:flex-row gap-8 items-center">
            <div class="w-16 h-16 bg-gold/20 rounded-2xl flex items-center justify-center shrink-0">
              <svg class="w-8 h-8 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
              </svg>
            </div>
            <div class="flex-1">
              <h2 class="font-heading text-xl font-bold text-navy mb-2">Seguro de transporte incluido</h2>
              <p class="text-sm leading-relaxed" style="color:rgba(11,31,58,0.7)">
                Todos nuestros envíos incluyen seguro de transporte a todo riesgo hasta el valor declarado del vehículo.
                En caso de daño o robo durante el transporte, gestionamos la reclamación por ti y te compensamos en un plazo máximo de 30 días.
              </p>
            </div>
            <a routerLink="/tramitacion" class="btn-gold py-3 px-6 shrink-0 whitespace-nowrap">
              Solicitar presupuesto
            </a>
          </div>
        </div>

        <!-- ── CTA ─────────────────────────────────────────────────────── -->
        <div class="text-center">
          <h2 class="font-heading text-2xl font-bold text-navy mb-3">¿Listo para mover tu vehículo?</h2>
          <p class="mb-6" style="color:rgba(11,31,58,0.6)">Solicita un presupuesto personalizado sin compromiso</p>
          <div class="flex flex-wrap gap-3 justify-center">
            <a routerLink="/tramitacion" class="btn-primary py-3 px-8">Solicitar presupuesto</a>
            <a routerLink="/guias/importacion" class="btn-outline py-3 px-8">Guía de importación</a>
          </div>
        </div>
      </div>
    </div>
  `
})
export class TransportPageComponent {
  readonly heroStats = [
    { value: '+50', label: 'Países de destino' },
    { value: '2.400+', label: 'Vehículos transportados' },
    { value: '4,9/5', label: 'Valoración media' },
    { value: '100%', label: 'Seguimiento incluido' }
  ];

  readonly services: ServiceCard[] = [
    {
      tag: 'Europa',
      title: 'Transporte en camión',
      icon: 'M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0zM13 6V4H4a1 1 0 00-1 1v10h2m0 0h10m-10 0H4m9-9h4l3 3v4h-7V7z',
      description: 'Portacoches cerrados o abiertos para transporte intraeuropeo. Ideal para distancias cortas y medias con entrega puerta a puerta.',
      features: [
        'Portacoches cerrado (premium)',
        'Portacoches abierto (estándar)',
        'Entrega puerta a puerta',
        'Plazo 2–7 días laborables',
        'GPS en tiempo real'
      ]
    },
    {
      tag: 'Intercontinental',
      title: 'Contenedor marítimo',
      icon: 'M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3',
      description: 'Exportación e importación intercontinental en contenedor individual (1 vehículo) o compartido (RoRo) para África, América y Asia.',
      features: [
        'Contenedor 20ft individual',
        'RoRo compartido (economía)',
        'Gestión aduanera incluida',
        'Plazo 10–35 días según destino',
        'Bill of Lading certificado'
      ]
    },
    {
      tag: 'Urgente',
      title: 'Transporte express',
      icon: 'M13 10V3L4 14h7v7l9-11h-7z',
      description: 'Cuando el tiempo es crítico, nuestro servicio express garantiza recogida en 24h y entrega en el menor plazo posible en cualquier punto de Europa.',
      features: [
        'Recogida en 24 horas',
        'Conductor dedicado',
        'Entrega en 1–3 días UE',
        'Comunicación directa',
        'Prioridad absoluta en ruta'
      ]
    }
  ];

  readonly routes: Route[] = [
    { from: 'Alemania', fromFlag: '🇩🇪', to: 'España', toFlag: '🇪🇸', method: 'Camión', days: '3–5 días', price: '450€' },
    { from: 'Francia', fromFlag: '🇫🇷', to: 'España', toFlag: '🇪🇸', method: 'Camión', days: '2–3 días', price: '320€' },
    { from: 'Italia', fromFlag: '🇮🇹', to: 'España', toFlag: '🇪🇸', method: 'Camión', days: '3–4 días', price: '390€' },
    { from: 'Reino Unido', fromFlag: '🇬🇧', to: 'España', toFlag: '🇪🇸', method: 'Camión', days: '4–6 días', price: '580€' },
    { from: 'Japón', fromFlag: '🇯🇵', to: 'España', toFlag: '🇪🇸', method: 'Contenedor', days: '28–35 días', price: '1.200€' },
    { from: 'EE.UU.', fromFlag: '🇺🇸', to: 'España', toFlag: '🇪🇸', method: 'RoRo', days: '18–25 días', price: '950€' },
    { from: 'Marruecos', fromFlag: '🇲🇦', to: 'España', toFlag: '🇪🇸', method: 'Ferry + Camión', days: '3–5 días', price: '380€' },
    { from: 'Polonia', fromFlag: '🇵🇱', to: 'España', toFlag: '🇪🇸', method: 'Camión', days: '4–6 días', price: '520€' }
  ];

  readonly steps = [
    { num: 1, title: 'Solicitud online', desc: 'Rellena el formulario con los datos del vehículo y el destino' },
    { num: 2, title: 'Presupuesto', desc: 'Recibes un presupuesto en menos de 2 horas en días laborables' },
    { num: 3, title: 'Recogida', desc: 'Nuestro transportista recoge el vehículo en la dirección acordada' },
    { num: 4, title: 'Entrega', desc: 'Entrega en el destino con firma y verificación del estado del vehículo' }
  ];

  readonly trackingFeatures = [
    'Actualizaciones automáticas por email y SMS',
    'Localización GPS del transportista',
    'Notificación de recogida, tránsito y entrega',
    'Acceso desde móvil y ordenador',
    'Historial completo de eventos descargable'
  ];

  readonly mockTracking = [
    { label: 'Vehículo recogido en Munich 🇩🇪', time: 'Lun 14 abr — 09:15', done: true },
    { label: 'En tránsito — Paso fronterizo Lyon', time: 'Lun 14 abr — 18:40', done: true },
    { label: 'En tránsito — Zaragoza', time: 'Mar 15 abr — 11:20', done: true },
    { label: 'Entrega en Madrid 🇪🇸', time: 'Mar 15 abr — 17:00 (estimado)', done: false },
  ];
}
