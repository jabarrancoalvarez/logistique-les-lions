import { Component, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface FinancingPlan {
  name: string;
  tag: string;
  rate: string;
  term: string;
  minAmount: string;
  description: string;
  features: string[];
  highlighted: boolean;
}

@Component({
  selector: 'lll-financing-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule, DecimalPipe, FormsModule],
  template: `
    <div class="min-h-screen bg-ivory">

      <!-- ── Hero ───────────────────────────────────────────────────────── -->
      <div class="bg-navy py-20 px-4">
        <div class="container mx-auto max-w-5xl">
          <div class="grid lg:grid-cols-2 gap-12 items-center">
            <div>
              <p class="divider-gold mb-6">Financiación inteligente</p>
              <h1 class="font-heading text-4xl lg:text-5xl font-bold text-ivory mb-4 leading-tight">
                Tu vehículo de importación,
                <span class="text-gold"> sin esperar</span>
              </h1>
              <p class="text-lg leading-relaxed mb-8" style="color:rgba(248,246,240,0.7)">
                Financiación flexible con las mejores condiciones del mercado. Desde vehículos de ocasión
                hasta superdeportivos, tenemos una solución para cada cliente.
              </p>
              <div class="flex flex-wrap gap-3">
                <a routerLink="/vehiculos" class="btn-gold py-3 px-6">Ver vehículos</a>
                <button (click)="scrollToCalc()" class="btn-outline btn-outline-gold py-3 px-6">
                  Calcular cuota
                </button>
              </div>
            </div>
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

      <div class="container mx-auto max-w-5xl px-4 py-16">

        <!-- ── Plans ────────────────────────────────────────────────────── -->
        <div class="text-center mb-12">
          <p class="divider-gold justify-center mb-4">Productos de financiación</p>
          <h2 class="font-heading text-3xl font-bold text-navy">Elige el plan que más te convenga</h2>
        </div>
        <div class="grid md:grid-cols-3 gap-6 mb-16">
          @for (plan of plans; track plan.name) {
            <div [class]="planCardClass(plan)">
              @if (plan.highlighted) {
                <div class="badge badge-gold self-start mb-4">Más solicitado</div>
              }
              <div class="mb-4">
                <p class="badge badge-navy text-xs mb-3">{{ plan.tag }}</p>
                <h3 [class]="plan.highlighted ? 'font-heading text-xl font-bold text-ivory mb-1' : 'font-heading text-xl font-bold text-navy mb-1'">
                  {{ plan.name }}
                </h3>
                <p [class]="plan.highlighted ? 'text-sm opacity-60 text-ivory' : 'text-sm opacity-60 text-navy'">
                  {{ plan.description }}
                </p>
              </div>
              <div class="py-4 my-4" [style]="plan.highlighted ? 'border-top:1px solid rgba(255,255,255,0.1);border-bottom:1px solid rgba(255,255,255,0.1)' : 'border-top:1px solid rgba(11,31,58,0.08);border-bottom:1px solid rgba(11,31,58,0.08)'">
                <div class="flex justify-between items-center mb-2">
                  <span [class]="plan.highlighted ? 'text-xs text-ivory/50' : 'text-xs opacity-50 text-navy'">TIN desde</span>
                  <span [class]="plan.highlighted ? 'font-heading font-bold text-gold text-xl' : 'font-heading font-bold text-gold text-xl'">{{ plan.rate }}</span>
                </div>
                <div class="flex justify-between items-center mb-2">
                  <span [class]="plan.highlighted ? 'text-xs text-ivory/50' : 'text-xs opacity-50 text-navy'">Plazo</span>
                  <span [class]="plan.highlighted ? 'text-sm font-medium text-ivory' : 'text-sm font-medium text-navy'">{{ plan.term }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span [class]="plan.highlighted ? 'text-xs text-ivory/50' : 'text-xs opacity-50 text-navy'">Importe mínimo</span>
                  <span [class]="plan.highlighted ? 'text-sm font-medium text-ivory' : 'text-sm font-medium text-navy'">{{ plan.minAmount }}</span>
                </div>
              </div>
              <ul class="space-y-2.5 mb-6 flex-1">
                @for (feat of plan.features; track feat) {
                  <li class="flex items-start gap-2 text-sm">
                    <svg class="w-4 h-4 shrink-0 mt-0.5 text-gold" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                    </svg>
                    <span [class]="plan.highlighted ? 'opacity-80 text-ivory' : 'opacity-70 text-navy'">{{ feat }}</span>
                  </li>
                }
              </ul>
              <a routerLink="/vehiculos" [class]="plan.highlighted ? 'block text-center py-3 px-6 rounded-lg font-semibold text-sm bg-gold text-navy hover:bg-gold-dark transition-colors' : 'block text-center py-3 px-6 rounded-lg font-semibold text-sm bg-navy text-ivory hover:bg-navy-light transition-colors'">
                Solicitar financiación
              </a>
            </div>
          }
        </div>

        <!-- ── Calculator ──────────────────────────────────────────────── -->
        <div id="calculadora" class="bg-white rounded-2xl shadow-card overflow-hidden mb-16">
          <div class="bg-navy px-8 py-6">
            <h2 class="font-heading text-xl font-bold text-ivory">Calculadora de cuota mensual</h2>
            <p class="text-sm mt-1" style="color:rgba(248,246,240,0.6)">Simulación orientativa. La cuota real puede variar según tu perfil crediticio.</p>
          </div>
          <div class="p-8">
            <div class="grid lg:grid-cols-2 gap-10 items-start">
              <!-- Inputs -->
              <div class="space-y-6">
                <div>
                  <label class="block text-sm font-semibold text-navy mb-2">
                    Precio del vehículo: <span class="text-gold">{{ vehiclePrice() | number:'1.0-0' }}€</span>
                  </label>
                  <input type="range" [min]="5000" [max]="200000" [step]="1000"
                    [(ngModel)]="vehiclePriceVal"
                    class="w-full h-2 bg-navy/10 rounded-full appearance-none cursor-pointer accent-gold">
                  <div class="flex justify-between text-xs mt-1" style="color:rgba(11,31,58,0.4)">
                    <span>5.000€</span><span>200.000€</span>
                  </div>
                </div>
                <div>
                  <label class="block text-sm font-semibold text-navy mb-2">
                    Entrada: <span class="text-gold">{{ downPayment() | number:'1.0-0' }}€ ({{ downPaymentPct() }}%)</span>
                  </label>
                  <input type="range" [min]="0" [max]="50" [step]="5"
                    [(ngModel)]="downPaymentPctVal"
                    class="w-full h-2 bg-navy/10 rounded-full appearance-none cursor-pointer accent-gold">
                  <div class="flex justify-between text-xs mt-1" style="color:rgba(11,31,58,0.4)">
                    <span>Sin entrada</span><span>50%</span>
                  </div>
                </div>
                <div>
                  <label class="block text-sm font-semibold text-navy mb-2">
                    Plazo: <span class="text-gold">{{ termMonths() }} meses</span>
                  </label>
                  <input type="range" [min]="12" [max]="84" [step]="12"
                    [(ngModel)]="termMonthsVal"
                    class="w-full h-2 bg-navy/10 rounded-full appearance-none cursor-pointer accent-gold">
                  <div class="flex justify-between text-xs mt-1" style="color:rgba(11,31,58,0.4)">
                    <span>12 meses</span><span>84 meses</span>
                  </div>
                </div>
                <div>
                  <label class="block text-sm font-semibold text-navy mb-2">
                    TIN: <span class="text-gold">{{ tinPct() }}%</span>
                  </label>
                  <input type="range" [min]="3" [max]="12" [step]="0.5"
                    [(ngModel)]="tinPctVal"
                    class="w-full h-2 bg-navy/10 rounded-full appearance-none cursor-pointer accent-gold">
                  <div class="flex justify-between text-xs mt-1" style="color:rgba(11,31,58,0.4)">
                    <span>3%</span><span>12%</span>
                  </div>
                </div>
              </div>

              <!-- Result -->
              <div class="bg-navy rounded-2xl p-8 text-center">
                <p class="text-sm mb-2" style="color:rgba(248,246,240,0.6)">Cuota mensual estimada</p>
                <p class="font-heading text-5xl font-bold text-gold mb-1">{{ monthlyPayment() | number:'1.0-0' }}<span class="text-2xl">€</span></p>
                <p class="text-xs mb-8" style="color:rgba(248,246,240,0.4)">/mes durante {{ termMonths() }} meses</p>

                <div class="space-y-3 text-left mb-8">
                  @for (row of calcSummary(); track row.label) {
                    <div class="flex justify-between items-center py-2" style="border-bottom:1px solid rgba(255,255,255,0.07)">
                      <span class="text-xs" style="color:rgba(248,246,240,0.5)">{{ row.label }}</span>
                      <span class="text-sm font-medium text-ivory">{{ row.value }}</span>
                    </div>
                  }
                </div>

                <a routerLink="/vehiculos" class="btn-gold w-full text-center py-3 px-6 block">
                  Buscar vehículo a financiar
                </a>
                <p class="text-xs mt-3" style="color:rgba(248,246,240,0.3)">
                  Simulación sin carácter contractual. Sujeta a aprobación crediticia.
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- ── Requirements ────────────────────────────────────────────── -->
        <div class="grid md:grid-cols-2 gap-8 mb-16">
          <div class="bg-white rounded-2xl shadow-card p-8">
            <h2 class="font-heading text-xl font-bold text-navy mb-6">Requisitos para particulares</h2>
            <ul class="space-y-4">
              @for (req of reqsParticular; track req.label) {
                <li class="flex items-start gap-3">
                  <div class="w-8 h-8 rounded-full bg-gold/10 flex items-center justify-center shrink-0 mt-0.5">
                    <svg class="w-4 h-4 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" [attr.d]="req.icon"/>
                    </svg>
                  </div>
                  <div>
                    <p class="font-semibold text-navy text-sm">{{ req.label }}</p>
                    <p class="text-xs mt-0.5" style="color:rgba(11,31,58,0.55)">{{ req.desc }}</p>
                  </div>
                </li>
              }
            </ul>
          </div>
          <div class="bg-white rounded-2xl shadow-card p-8">
            <h2 class="font-heading text-xl font-bold text-navy mb-6">Requisitos para empresas</h2>
            <ul class="space-y-4">
              @for (req of reqsEmpresa; track req.label) {
                <li class="flex items-start gap-3">
                  <div class="w-8 h-8 rounded-full bg-gold/10 flex items-center justify-center shrink-0 mt-0.5">
                    <svg class="w-4 h-4 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" [attr.d]="req.icon"/>
                    </svg>
                  </div>
                  <div>
                    <p class="font-semibold text-navy text-sm">{{ req.label }}</p>
                    <p class="text-xs mt-0.5" style="color:rgba(11,31,58,0.55)">{{ req.desc }}</p>
                  </div>
                </li>
              }
            </ul>
          </div>
        </div>

        <!-- ── Process ─────────────────────────────────────────────────── -->
        <div class="bg-navy rounded-2xl p-8 lg:p-12 mb-16">
          <h2 class="font-heading text-2xl font-bold text-ivory mb-10 text-center">Proceso de solicitud</h2>
          <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-8">
            @for (step of steps; track step.num) {
              <div class="text-center">
                <div class="w-12 h-12 rounded-full bg-gold flex items-center justify-center text-navy font-bold font-heading text-lg mx-auto mb-4">
                  {{ step.num }}
                </div>
                <h3 class="font-semibold text-ivory text-sm mb-2">{{ step.title }}</h3>
                <p class="text-xs leading-relaxed" style="color:rgba(248,246,240,0.5)">{{ step.desc }}</p>
              </div>
            }
          </div>
        </div>

        <!-- ── Partners ────────────────────────────────────────────────── -->
        <div class="text-center mb-16">
          <p class="text-sm font-semibold uppercase tracking-widest mb-6" style="color:rgba(11,31,58,0.4)">Trabajamos con las principales entidades financieras</p>
          <div class="flex flex-wrap justify-center gap-6">
            @for (bank of banks; track bank) {
              <div class="bg-white rounded-xl px-6 py-4 shadow-card text-navy/50 font-semibold text-sm">{{ bank }}</div>
            }
          </div>
        </div>

        <!-- ── CTA ─────────────────────────────────────────────────────── -->
        <div class="text-center">
          <h2 class="font-heading text-2xl font-bold text-navy mb-3">¿Tienes alguna duda?</h2>
          <p class="mb-6" style="color:rgba(11,31,58,0.6)">Nuestros asesores financieros resuelven todas tus preguntas sin compromiso</p>
          <div class="flex flex-wrap gap-3 justify-center">
            <a routerLink="/vehiculos" class="btn-primary py-3 px-8">Buscar vehículo</a>
            <a routerLink="/tramitacion" class="btn-outline py-3 px-8">Tramitación documental</a>
          </div>
        </div>
      </div>
    </div>
  `
})
export class FinancingPageComponent {
  // Calculator signals
  vehiclePriceVal = 30000;
  downPaymentPctVal = 20;
  termMonthsVal = 48;
  tinPctVal = 5.9;

  readonly vehiclePrice = signal(30000);
  readonly downPaymentPctSignal = signal(20);
  readonly termMonthsSignal = signal(48);
  readonly tinPctSignal = signal(5.9);

  get vehiclePriceNum() { return this.vehiclePriceVal; }
  downPayment() { return this.vehiclePriceVal * (this.downPaymentPctVal / 100); }
  downPaymentPct() { return this.downPaymentPctVal; }
  termMonths() { return this.termMonthsVal; }
  tinPct() { return this.tinPctVal; }

  monthlyPayment(): number {
    const principal = this.vehiclePriceVal - this.downPayment();
    const monthlyRate = this.tinPctVal / 100 / 12;
    const n = this.termMonthsVal;
    if (monthlyRate === 0) return principal / n;
    return principal * (monthlyRate * Math.pow(1 + monthlyRate, n)) / (Math.pow(1 + monthlyRate, n) - 1);
  }

  calcSummary(): { label: string; value: string }[] {
    const totalPaid = this.monthlyPayment() * this.termMonthsVal + this.downPayment();
    const totalInterest = totalPaid - this.vehiclePriceVal;
    return [
      { label: 'Precio del vehículo', value: `${this.vehiclePriceVal.toLocaleString('es-ES')}€` },
      { label: 'Entrada', value: `${this.downPayment().toLocaleString('es-ES', { maximumFractionDigits: 0 })}€` },
      { label: 'Capital a financiar', value: `${(this.vehiclePriceVal - this.downPayment()).toLocaleString('es-ES', { maximumFractionDigits: 0 })}€` },
      { label: 'Total intereses', value: `${totalInterest.toLocaleString('es-ES', { maximumFractionDigits: 0 })}€` },
      { label: 'Coste total', value: `${totalPaid.toLocaleString('es-ES', { maximumFractionDigits: 0 })}€` },
    ];
  }

  scrollToCalc() {
    document.getElementById('calculadora')?.scrollIntoView({ behavior: 'smooth' });
  }

  planCardClass(plan: FinancingPlan): string {
    const base = 'rounded-2xl p-7 flex flex-col';
    return plan.highlighted
      ? `${base} bg-navy ring-2 ring-gold shadow-navy`
      : `${base} bg-white shadow-card`;
  }

  readonly heroStats = [
    { value: '3,9%', label: 'TIN desde' },
    { value: '84', label: 'Meses máximo' },
    { value: '48h', label: 'Respuesta en' },
    { value: '100%', label: 'Online, sin papeles' }
  ];

  readonly plans: FinancingPlan[] = [
    {
      name: 'Financiación Clásica',
      tag: 'Préstamo personal',
      rate: '5,9%',
      term: '12 – 84 meses',
      minAmount: '5.000€',
      description: 'La opción más sencilla. Un préstamo al consumo para comprar cualquier vehículo.',
      highlighted: false,
      features: [
        'Sin entrada obligatoria',
        'Sin restricción de marca ni modelo',
        'Vehículos nuevos y de ocasión',
        'Amortización anticipada sin penalización',
        'Gestión 100% online'
      ]
    },
    {
      name: 'Financiación Premium',
      tag: 'Más flexible',
      rate: '3,9%',
      term: '24 – 60 meses',
      minAmount: '15.000€',
      description: 'Cuota reducida con valor residual garantizado al final del contrato.',
      highlighted: true,
      features: [
        'Cuotas hasta 30% más bajas',
        'Valor residual garantizado (VRG)',
        'Opción de compra al final',
        'Extensión del contrato',
        'Mantenimiento incluido opcional'
      ]
    },
    {
      name: 'Leasing Empresarial',
      tag: 'Para empresas',
      rate: '4,5%',
      term: '24 – 60 meses',
      minAmount: '10.000€',
      description: 'Deduce las cuotas como gasto empresarial. Renovación garantizada de flota.',
      highlighted: false,
      features: [
        'IVA deducible al 100%',
        'Cuotas como gasto deducible',
        'Gestoría y matriculación incluidas',
        'Vehículos de sustitución',
        'Renovación automática de flota'
      ]
    }
  ];

  readonly reqsParticular = [
    {
      label: 'Documentación de identidad',
      desc: 'DNI o NIE en vigor del solicitante y del aval si aplica',
      icon: 'M10 6H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V8a2 2 0 00-2-2h-5m-4 0V5a2 2 0 114 0v1m-4 0a2 2 0 104 0'
    },
    {
      label: 'Justificante de ingresos',
      desc: 'Últimas 3 nóminas o declaración de la renta (autónomos)',
      icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z'
    },
    {
      label: 'Vida laboral',
      desc: 'Informe de vida laboral actualizado (menos de 3 meses)',
      icon: 'M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z'
    },
    {
      label: 'Sin incidencias CIRBE',
      desc: 'Sin deudas activas en el Banco de España ni en ficheros de morosidad',
      icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z'
    }
  ];

  readonly reqsEmpresa = [
    {
      label: 'NIF de la empresa',
      desc: 'CIF de la empresa y DNI del representante legal con poderes',
      icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4'
    },
    {
      label: 'Últimas 2 declaraciones anuales',
      desc: 'Modelos 200 (IS) o IRPF del autónomo correspondientes',
      icon: 'M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21l-7-7-7 7V5a2 2 0 012-2h10a2 2 0 012 2v16z'
    },
    {
      label: 'Balance y cuenta de resultados',
      desc: 'Documentos contables del último ejercicio cerrado',
      icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z'
    },
    {
      label: 'Antigüedad mínima 1 año',
      desc: 'La empresa debe estar constituida y activa al menos 12 meses',
      icon: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z'
    }
  ];

  readonly steps = [
    { num: 1, title: 'Solicitud online', desc: 'Completa el formulario con tus datos en menos de 5 minutos' },
    { num: 2, title: 'Análisis en 48h', desc: 'Nuestros analistas estudian tu perfil y te contactan' },
    { num: 3, title: 'Firma digital', desc: 'Firma el contrato de financiación 100% online, sin desplazamientos' },
    { num: 4, title: 'Vehículo listo', desc: 'Fondos disponibles en 48h. Tu vehículo importado te espera' }
  ];

  readonly banks = ['Santander Consumer', 'CaixaBank', 'BBVA Autorenting', 'Sabadell', 'Cetelem', 'Bankinter'];
}
