import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';

interface GuideStep {
  number: number;
  title: string;
  description: string;
}

interface GuideContent {
  heading: string;
  subtitle: string;
  duration: string;
  complexity: string;
  intro: string;
  steps: GuideStep[];
  tips: string[];
  documents: string[];
}

const GUIDES: Record<string, GuideContent> = {
  importacion: {
    heading: 'Guía de Importación de Vehículos',
    subtitle: 'Todo lo que necesitas saber para importar un vehículo a España desde cualquier país',
    duration: '4-8 semanas',
    complexity: 'Media',
    intro: 'Importar un vehículo a España puede parecer complejo, pero con la documentación correcta y siguiendo los pasos adecuados el proceso es completamente viable. Esta guía te explica todo el proceso paso a paso.',
    steps: [
      {
        number: 1,
        title: 'Verificación del vehículo',
        description: 'Antes de comprar, verifica el historial del vehículo, su estado mecánico y que no tenga cargas pendientes. Solicita el informe de homologación provisional y confirma que el modelo puede ser homologado en España.'
      },
      {
        number: 2,
        title: 'Documentación de compra',
        description: 'Obtén la factura de compraventa, el título de propiedad del país de origen (título de propiedad o certificado de registro), el certificado de conformidad CE (si es vehículo europeo) y el permiso de circulación original.'
      },
      {
        number: 3,
        title: 'Despacho aduanero',
        description: 'Presenta el vehículo en aduana con toda la documentación. Para vehículos de fuera de la UE es necesario pagar los aranceles correspondientes (6.5% para vehículos de pasajeros). Para vehículos de la UE el proceso es más sencillo al no haber aranceles.'
      },
      {
        number: 4,
        title: 'Pago del IVA e impuestos',
        description: 'Abona el IVA (21%) y el Impuesto de Matriculación (0-14.75% según emisiones CO₂). Estos importes se calculan sobre el valor en aduana del vehículo.'
      },
      {
        number: 5,
        title: 'Homologación',
        description: 'Lleva el vehículo a un laboratorio de homologación autorizado. Se comprueba que cumple las normativas técnicas españolas y europeas. Este proceso puede requerir adaptar algunos elementos del vehículo.'
      },
      {
        number: 6,
        title: 'Inspección técnica (ITV)',
        description: 'Supera la Inspección Técnica de Vehículos. Para vehículos importados de países con conducción por la derecha el proceso es estándar; para vehículos con volante a la derecha puede requerir adaptaciones.'
      },
      {
        number: 7,
        title: 'Matriculación',
        description: 'Presenta toda la documentación en la Jefatura Provincial de Tráfico para obtener la matrícula española. Necesitarás el DNI/NIE, justificante de domicilio, informe de homologación y ficha técnica reducida.'
      }
    ],
    tips: [
      'Contrata siempre un inspector profesional antes de la compra para evitar sorpresas',
      'Los vehículos fabricados en la UE son más fáciles de homologar que los de fuera',
      'El coste total de importación puede suponer entre el 25% y el 40% adicional sobre el precio del vehículo',
      'Algunos modelos exóticos o con modificaciones especiales pueden no homologarse',
      'Utiliza un transitario o agente de aduanas para simplificar la gestión aduanera'
    ],
    documents: [
      'Factura de compraventa (original y traducción jurada si aplica)',
      'Título de propiedad del país de origen',
      'Certificado de conformidad CE (vehículos europeos)',
      'Informe aduanero DUA (Documento Único Administrativo)',
      'Justificante de pago de aranceles e IVA',
      'Informe de homologación del Ministerio de Industria',
      'Ficha técnica del vehículo',
      'DNI/NIE del importador'
    ]
  },
  exportacion: {
    heading: 'Guía de Exportación de Vehículos',
    subtitle: 'Proceso completo para exportar un vehículo desde España a cualquier destino',
    duration: '2-6 semanas',
    complexity: 'Baja-Media',
    intro: 'Exportar un vehículo desde España es generalmente más sencillo que importarlo. El proceso varía según el país de destino, pero esta guía cubre los aspectos esenciales del proceso.',
    steps: [
      {
        number: 1,
        title: 'Preparación documental',
        description: 'Reúne toda la documentación del vehículo: permiso de circulación, ficha técnica, ITV en vigor (si aplica), y factura de compraventa. Asegúrate de que el vehículo no tiene cargas pendientes (embargos, multas).'
      },
      {
        number: 2,
        title: 'Baja de matrícula española',
        description: 'Solicita en la Jefatura Provincial de Tráfico la baja definitiva del vehículo. Necesitarás el permiso de circulación, la placa de matrícula y el DNI. Recibirás un certificado de baja para exportación.'
      },
      {
        number: 3,
        title: 'Despacho de exportación',
        description: 'Presenta el vehículo en aduana de salida con la documentación necesaria. Para exportaciones fuera de la UE hay que presentar el DUA de exportación y podrás solicitar la devolución del IVA si el vehículo fue comprado en España.'
      },
      {
        number: 4,
        title: 'Transporte internacional',
        description: 'Gestiona el transporte mediante camión de portacoches o en contenedor marítimo según el destino. Obtén el seguro de transporte para cubrir posibles daños durante el traslado.'
      },
      {
        number: 5,
        title: 'Documentación en destino',
        description: 'Entrega al comprador todos los originales de documentación. El comprador necesitará estos documentos para registrar el vehículo en el país de destino según su legislación local.'
      }
    ],
    tips: [
      'Investiga los requisitos específicos del país de destino antes de comenzar',
      'Para exportaciones fuera de la UE puedes recuperar el IVA si el vehículo fue comprado con IVA en España',
      'Algunos países tienen restricciones de edad para vehículos importados',
      'El valor del vehículo puede depreciarse durante el transporte — asegúralo correctamente',
      'Logistique Les Lions puede gestionar todo el proceso por ti, incluyendo el transporte'
    ],
    documents: [
      'Permiso de circulación original',
      'Ficha técnica del vehículo',
      'Certificado de baja de matrícula española',
      'Factura de compraventa',
      'DUA de exportación (para destinos fuera de la UE)',
      'Seguro de transporte',
      'Pasaporte/DNI del exportador'
    ]
  },
  homologacion: {
    heading: 'Homologaciones en la Unión Europea',
    subtitle: 'Guía completa sobre el proceso de homologación de vehículos importados en España y la UE',
    duration: '2-12 semanas',
    complexity: 'Alta',
    intro: 'La homologación es el proceso por el cual se certifica que un vehículo importado cumple con las normativas técnicas y de seguridad de la Unión Europea. Es un requisito obligatorio para poder circular legalmente en España.',
    steps: [
      {
        number: 1,
        title: 'Evaluación previa',
        description: 'Determina qué tipo de homologación necesita tu vehículo. Los vehículos de países de la UE generalmente tienen homologación CE y el proceso es más sencillo. Los de fuera de la UE requieren una homologación individual o de pequeña serie.'
      },
      {
        number: 2,
        title: 'Solicitud al Ministerio de Industria',
        description: 'Presenta la solicitud de homologación individual ante el Ministerio de Industria, Comercio y Turismo, junto con la documentación técnica del vehículo: especificaciones, medidas, datos del motor y sistemas de seguridad.'
      },
      {
        number: 3,
        title: 'Inspección técnica del vehículo',
        description: 'Un laboratorio de homologación oficial realiza una inspección completa del vehículo. Comprueba sistemas de frenos, emisiones, iluminación, cinturones de seguridad, airbags, y todos los sistemas de seguridad activa y pasiva.'
      },
      {
        number: 4,
        title: 'Adaptaciones necesarias',
        description: 'Si el vehículo no cumple algún requisito, deberás realizar las modificaciones necesarias. Las más comunes son: adaptación de luces para circulación por la derecha, instalación de limitadores de velocidad, o modificaciones en el escape para cumplir normativas de emisiones Euro 6.'
      },
      {
        number: 5,
        title: 'Certificado de homologación',
        description: 'Una vez superadas todas las pruebas, recibes el Certificado de Homologación y la Ficha Técnica reducida. Estos documentos son necesarios para la posterior matriculación en la DGT.'
      },
      {
        number: 6,
        title: 'ITV de importación',
        description: 'Con el certificado de homologación, el vehículo debe pasar la ITV de primera matriculación. Esta inspección verifica que el vehículo real coincide con el homologado.'
      }
    ],
    tips: [
      'Los vehículos con más de 30 años pueden acceder a la categoría de Vehículo Histórico, con requisitos diferentes',
      'Las marcas conocidas (BMW, Mercedes, Toyota, etc.) suelen tener la documentación técnica disponible en las filiales europeas',
      'Para vehículos americanos, japoneses o surcoreanos el proceso es más complejo y costoso',
      'El coste de homologación individual varía entre 500€ y 5.000€ según el vehículo y las adaptaciones necesarias',
      'Nuestro equipo de tramitación puede gestionar todo el proceso de homologación'
    ],
    documents: [
      'Documentación técnica del fabricante (manual del propietario, especificaciones técnicas)',
      'Certificado del fabricante sobre emisiones y normativa de seguridad',
      'Factura de compra y documentación aduanera',
      'Solicitud de homologación individual (formulario oficial)',
      'Informe de inspección del laboratorio homologador',
      'Justificante de pago de tasas administrativas'
    ]
  }
};

@Component({
  selector: 'lll-guide-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule],
  template: `
    <div class="min-h-screen bg-ivory">
      <!-- Header -->
      <div class="bg-navy py-16 px-4">
        <div class="container mx-auto max-w-4xl">
          <a routerLink="/" class="inline-flex items-center gap-2 text-ivory/60 hover:text-gold text-sm mb-6 transition-colors">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
            </svg>
            Volver al inicio
          </a>
          <h1 class="font-heading text-3xl lg:text-5xl font-bold text-ivory mb-4">{{ guide().heading }}</h1>
          <p class="text-ivory/70 text-lg mb-8 leading-relaxed">{{ guide().subtitle }}</p>
          <div class="flex flex-wrap gap-4">
            <div class="flex items-center gap-2 bg-white/10 rounded-lg px-4 py-2">
              <svg class="w-4 h-4 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              <span class="text-ivory/80 text-sm">Duración: <strong class="text-ivory">{{ guide().duration }}</strong></span>
            </div>
            <div class="flex items-center gap-2 bg-white/10 rounded-lg px-4 py-2">
              <svg class="w-4 h-4 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
              </svg>
              <span class="text-ivory/80 text-sm">Complejidad: <strong class="text-ivory">{{ guide().complexity }}</strong></span>
            </div>
          </div>
        </div>
      </div>

      <div class="container mx-auto max-w-4xl px-4 py-12">
        <!-- Intro -->
        <p class="text-navy/70 text-lg leading-relaxed mb-12">{{ guide().intro }}</p>

        <!-- Steps -->
        <h2 class="font-heading text-2xl font-bold text-navy mb-8">Proceso paso a paso</h2>
        <div class="space-y-6 mb-12">
          @for (step of guide().steps; track step.number) {
            <div class="flex gap-5 p-6 bg-white rounded-xl shadow-card hover:shadow-card-hover transition-shadow">
              <div class="w-10 h-10 rounded-full bg-gold flex items-center justify-center text-navy font-bold font-heading text-lg shrink-0">
                {{ step.number }}
              </div>
              <div>
                <h3 class="font-semibold text-navy text-base mb-2">{{ step.title }}</h3>
                <p class="text-navy/60 text-sm leading-relaxed">{{ step.description }}</p>
              </div>
            </div>
          }
        </div>

        <!-- Two columns: Tips + Documents -->
        <div class="grid lg:grid-cols-2 gap-8 mb-12">
          <!-- Tips -->
          <div class="bg-gold/5 border border-gold/20 rounded-xl p-6">
            <h2 class="font-heading text-xl font-bold text-navy mb-5 flex items-center gap-2">
              <svg class="w-5 h-5 text-gold" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2a7 7 0 017 7c0 2.38-1.19 4.47-3 5.74V17a1 1 0 01-1 1H9a1 1 0 01-1-1v-2.26C6.19 13.47 5 11.38 5 9a7 7 0 017-7zm3 19H9v1a1 1 0 001 1h4a1 1 0 001-1v-1z"/>
              </svg>
              Consejos prácticos
            </h2>
            <ul class="space-y-3">
              @for (tip of guide().tips; track tip) {
                <li class="flex gap-2 text-sm text-navy/70">
                  <svg class="w-4 h-4 text-gold shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                  </svg>
                  {{ tip }}
                </li>
              }
            </ul>
          </div>

          <!-- Documents -->
          <div class="bg-navy/5 border border-navy/10 rounded-xl p-6">
            <h2 class="font-heading text-xl font-bold text-navy mb-5 flex items-center gap-2">
              <svg class="w-5 h-5 text-navy/60" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              Documentos necesarios
            </h2>
            <ul class="space-y-3">
              @for (doc of guide().documents; track doc) {
                <li class="flex gap-2 text-sm text-navy/70">
                  <svg class="w-4 h-4 text-navy/40 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4"/>
                  </svg>
                  {{ doc }}
                </li>
              }
            </ul>
          </div>
        </div>

        <!-- CTA -->
        <div class="bg-navy rounded-2xl p-8 text-center">
          <h2 class="font-heading text-2xl font-bold text-ivory mb-3">¿Necesitas ayuda con el proceso?</h2>
          <p class="text-ivory/70 mb-6">Nuestro equipo de especialistas gestiona todo el proceso por ti. Desde la compra hasta la matriculación.</p>
          <div class="flex flex-wrap gap-3 justify-center">
            <a routerLink="/tramitacion" class="btn-gold py-3 px-6">Ver servicios de tramitación</a>
            <a routerLink="/vehiculos" class="btn-outline btn-outline-gold py-3 px-6">Buscar vehículos</a>
          </div>
        </div>
      </div>
    </div>
  `
})
export class GuidePageComponent {
  private route = inject(ActivatedRoute);

  readonly guide = toSignal(
    this.route.data.pipe(map(data => GUIDES[data['slug']] ?? GUIDES['importacion'])),
    { initialValue: GUIDES['importacion'] }
  );
}
