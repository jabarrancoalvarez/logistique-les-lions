import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';

interface LegalSection {
  title: string;
  body: string;
}

interface LegalContent {
  heading: string;
  lastUpdate: string;
  intro: string;
  sections: LegalSection[];
}

const LEGAL_CONTENT: Record<string, LegalContent> = {
  'aviso-legal': {
    heading: 'Aviso Legal',
    lastUpdate: 'Enero 2025',
    intro: 'En cumplimiento con el deber de información recogido en el artículo 10 de la Ley 34/2002, de 11 de julio, de Servicios de la Sociedad de la Información y del Comercio Electrónico (LSSI-CE), a continuación se reflejan los datos identificativos de la empresa titular de este sitio web.',
    sections: [
      {
        title: '1. Datos del titular',
        body: 'Logistique Les Lions, S.L. (en adelante, "la Empresa") es titular del sitio web logistiqueleslions.com. Domicilio social: Calle de ejemplo, 123, 28001 Madrid, España. NIF: B-12345678. Registro Mercantil de Madrid, Tomo XXXXX, Folio XXX, Hoja M-XXXXXX.'
      },
      {
        title: '2. Objeto y ámbito de aplicación',
        body: 'El presente Aviso Legal regula el acceso y uso del sitio web logistiqueleslions.com, plataforma dedicada a la compraventa e importación de vehículos de origen europeo y mundial. El acceso a este sitio web implica la aceptación plena y sin reservas de las presentes condiciones de uso.'
      },
      {
        title: '3. Propiedad intelectual e industrial',
        body: 'Todos los contenidos de este sitio web —textos, fotografías, gráficos, imágenes, iconos, tecnología, software, marcas, denominaciones comerciales y demás elementos susceptibles de protección— son titularidad de la Empresa o de terceros que han autorizado su uso. Queda expresamente prohibida su reproducción, distribución, comunicación pública o transformación sin autorización expresa.'
      },
      {
        title: '4. Exclusión de garantías y responsabilidad',
        body: 'La Empresa no garantiza la ausencia de errores en el acceso al sitio web ni en los contenidos. No se responsabiliza de los daños o perjuicios que pudieran derivarse del uso del sitio web, de errores en los contenidos, ni de la presencia de virus u otros elementos dañinos. La información contenida en los anuncios de vehículos es responsabilidad exclusiva de los anunciantes.'
      },
      {
        title: '5. Legislación aplicable y jurisdicción',
        body: 'Las relaciones entre la Empresa y el usuario se regirán por la legislación española. Para la resolución de cualquier controversia, las partes se someten a los Juzgados y Tribunales de Madrid, con renuncia expresa a cualquier otro fuero que pudiera corresponderles.'
      }
    ]
  },
  'privacidad': {
    heading: 'Política de Privacidad',
    lastUpdate: 'Enero 2025',
    intro: 'Logistique Les Lions, S.L. se compromete a proteger y respetar su privacidad. Esta política explica cuándo y por qué recopilamos información personal, cómo la usamos y en qué condiciones podemos compartirla con terceros.',
    sections: [
      {
        title: '1. Responsable del tratamiento',
        body: 'Logistique Les Lions, S.L., NIF B-12345678, con domicilio en Calle de ejemplo, 123, 28001 Madrid. Email de contacto: privacidad@logistiqueleslions.com.'
      },
      {
        title: '2. Datos que recopilamos',
        body: 'Recopilamos datos que usted nos proporciona directamente (nombre, email, teléfono al registrarse o contactar), datos de uso del sitio web (páginas visitadas, búsquedas realizadas, vehículos consultados) y datos técnicos (dirección IP, tipo de navegador, dispositivo). En ningún caso recopilamos datos especialmente protegidos.'
      },
      {
        title: '3. Finalidades del tratamiento',
        body: 'Sus datos son utilizados para: gestionar su cuenta de usuario; facilitar la comunicación entre compradores y vendedores; mejorar nuestros servicios y personalizar su experiencia; enviar comunicaciones comerciales, si lo ha consentido expresamente; cumplir obligaciones legales aplicables.'
      },
      {
        title: '4. Base jurídica',
        body: 'El tratamiento de sus datos se basa en: la ejecución del contrato de uso de la plataforma (art. 6.1.b RGPD); el consentimiento expreso para comunicaciones comerciales (art. 6.1.a RGPD); el interés legítimo para mejorar nuestros servicios (art. 6.1.f RGPD).'
      },
      {
        title: '5. Conservación de datos',
        body: 'Conservaremos sus datos mientras mantenga su cuenta activa y durante los plazos legalmente establecidos. Puede solicitar la supresión de su cuenta en cualquier momento, lo que conllevará la eliminación de sus datos personales, salvo los que deban conservarse por obligación legal.'
      },
      {
        title: '6. Sus derechos',
        body: 'Tiene derecho a acceder a sus datos, rectificarlos, suprimirlos, oponerse al tratamiento, solicitar la limitación del tratamiento y la portabilidad de los datos. Puede ejercer estos derechos escribiendo a privacidad@logistiqueleslions.com, adjuntando copia de su DNI. Tiene derecho a presentar reclamación ante la Agencia Española de Protección de Datos (www.aepd.es).'
      },
      {
        title: '7. Transferencias internacionales',
        body: 'Algunos de nuestros proveedores de servicios (alojamiento en la nube, analítica web) pueden encontrarse fuera del Espacio Económico Europeo. En tal caso, garantizamos que se aplican las salvaguardas adecuadas conforme al RGPD (cláusulas contractuales tipo u otras garantías).'
      }
    ]
  },
  'cookies': {
    heading: 'Política de Cookies',
    lastUpdate: 'Enero 2025',
    intro: 'Este sitio web utiliza cookies propias y de terceros para mejorar la experiencia de navegación, analizar el tráfico y personalizar el contenido. A continuación le explicamos qué cookies usamos y para qué.',
    sections: [
      {
        title: '¿Qué son las cookies?',
        body: 'Las cookies son pequeños archivos de texto que se almacenan en su navegador cuando visita un sitio web. Permiten que el sitio recuerde sus acciones y preferencias durante un período de tiempo, para que no tenga que volver a introducirlas cada vez que visite el sitio o navegue de una página a otra.'
      },
      {
        title: 'Cookies estrictamente necesarias',
        body: 'Estas cookies son imprescindibles para el funcionamiento del sitio. Incluyen cookies de sesión para mantener su estado de autenticación, cookies CSRF para protección de seguridad, y cookies de preferencias básicas. No pueden desactivarse sin afectar al funcionamiento del sitio.'
      },
      {
        title: 'Cookies analíticas',
        body: 'Utilizamos cookies analíticas (Google Analytics) para entender cómo los visitantes interactúan con nuestro sitio: páginas más visitadas, tiempo de permanencia, fuentes de tráfico. Esta información se utiliza para mejorar el sitio web. Los datos son anónimos y agregados.'
      },
      {
        title: 'Cookies de funcionalidad',
        body: 'Estas cookies permiten que el sitio web recuerde las elecciones que ha realizado (idioma preferido, región, etc.) para ofrecerle una experiencia más personalizada.'
      },
      {
        title: 'Gestión de cookies',
        body: 'Puede controlar y/o eliminar las cookies cuando desee. Puede eliminar todas las cookies que ya estén en su equipo y configurar la mayoría de los navegadores para que no las acepten. Sin embargo, si lo hace, es posible que tenga que ajustar manualmente algunas preferencias cada vez que visite un sitio web.'
      }
    ]
  },
  'terminos': {
    heading: 'Términos y Condiciones',
    lastUpdate: 'Enero 2025',
    intro: 'Los presentes Términos y Condiciones regulan el uso de la plataforma Logistique Les Lions y los servicios ofrecidos a través de ella. Al registrarse o utilizar nuestra plataforma, usted acepta estos términos en su totalidad.',
    sections: [
      {
        title: '1. Descripción del servicio',
        body: 'Logistique Les Lions es una plataforma online que conecta a compradores y vendedores de vehículos de origen internacional, facilitando además servicios de tramitación documental, homologación e importación. Actuamos como intermediarios y no somos parte en las transacciones entre usuarios.'
      },
      {
        title: '2. Registro y cuenta de usuario',
        body: 'Para acceder a determinadas funcionalidades es necesario registrarse. Usted es responsable de mantener la confidencialidad de sus credenciales de acceso y de todas las actividades realizadas desde su cuenta. Debe notificarnos inmediatamente cualquier uso no autorizado de su cuenta.'
      },
      {
        title: '3. Publicación de anuncios',
        body: 'Los usuarios pueden publicar anuncios de vehículos en la plataforma. Al publicar un anuncio, garantiza que la información es veraz y que tiene derecho a vender el vehículo. Queda prohibida la publicación de anuncios fraudulentos, duplicados o que incumplan la legislación aplicable. Nos reservamos el derecho a retirar anuncios que incumplan estas normas.'
      },
      {
        title: '4. Tarifas y pagos',
        body: 'La publicación básica de anuncios es gratuita. Existen planes de destacado y servicios premium con coste, detallados en nuestra página de precios. Los pagos se procesan a través de plataformas seguras de pago. No almacenamos datos de tarjetas de crédito.'
      },
      {
        title: '5. Limitación de responsabilidad',
        body: 'Logistique Les Lions no asume responsabilidad por el estado real de los vehículos anunciados, la veracidad de la información proporcionada por los usuarios, ni por las transacciones realizadas entre compradores y vendedores. Recomendamos siempre verificar el vehículo antes de la compra.'
      },
      {
        title: '6. Modificaciones',
        body: 'Nos reservamos el derecho de modificar estos Términos en cualquier momento. Le notificaremos los cambios significativos mediante email o mediante aviso prominente en la plataforma. El uso continuado de la plataforma tras la notificación implica la aceptación de los nuevos términos.'
      }
    ]
  },
  'rgpd': {
    heading: 'Protección de Datos (RGPD)',
    lastUpdate: 'Enero 2025',
    intro: 'Información sobre el tratamiento de datos personales de conformidad con el Reglamento (UE) 2016/679 del Parlamento Europeo y del Consejo (RGPD) y la Ley Orgánica 3/2018, de 5 de diciembre, de Protección de Datos Personales y garantía de los derechos digitales (LOPDGDD).',
    sections: [
      {
        title: 'Base de legitimación',
        body: 'Los tratamientos de datos personales realizados por Logistique Les Lions tienen como base jurídica: (a) el consentimiento del interesado para el envío de comunicaciones comerciales; (b) la ejecución del contrato de uso de la plataforma; (c) el cumplimiento de obligaciones legales; (d) el interés legítimo de la empresa en la mejora de sus servicios y la prevención del fraude.'
      },
      {
        title: 'Categorías de datos tratados',
        body: 'Tratamos datos identificativos (nombre, apellidos, NIF/NIE), datos de contacto (email, teléfono, dirección), datos de perfil (fotografía de perfil, preferencias), datos de uso de la plataforma, y datos de transacciones. No tratamos categorías especiales de datos (datos de salud, origen étnico, creencias religiosas, etc.).'
      },
      {
        title: 'Destinatarios',
        body: 'Sus datos podrán ser comunicados a: proveedores de servicios técnicos que actúan como encargados del tratamiento (hosting, email, análisis); entidades del grupo empresarial; Administración Pública y autoridades competentes en los casos legalmente previstos. No cedemos datos a terceros con fines comerciales sin su consentimiento.'
      },
      {
        title: 'Plazo de conservación',
        body: 'Los datos se conservarán mientras dure la relación comercial o hasta que el interesado solicite su supresión, y posteriormente durante los plazos legalmente establecidos (4 años para obligaciones fiscales, 3 años para datos comerciales, etc.).'
      },
      {
        title: 'Ejercicio de derechos',
        body: 'El interesado tiene derecho a: acceso, rectificación, supresión ("derecho al olvido"), limitación del tratamiento, portabilidad, oposición, y a no ser objeto de decisiones individualizadas automatizadas. Para ejercer estos derechos, dirija una solicitud a: dpo@logistiqueleslions.com. Tiene derecho a presentar reclamación ante la Agencia Española de Protección de Datos (www.aepd.es).'
      },
      {
        title: 'Delegado de Protección de Datos',
        body: 'Si tiene cualquier consulta sobre el tratamiento de sus datos personales o el ejercicio de sus derechos, puede contactar con nuestro Delegado de Protección de Datos (DPO) en: dpo@logistiqueleslions.com.'
      }
    ]
  }
};

@Component({
  selector: 'lll-legal-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule],
  template: `
    <div class="min-h-screen bg-ivory">
      <!-- Header -->
      <div class="bg-navy py-16 px-4">
        <div class="container mx-auto max-w-3xl">
          <a routerLink="/" class="inline-flex items-center gap-2 text-ivory/60 hover:text-gold text-sm mb-6 transition-colors">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
            </svg>
            Volver al inicio
          </a>
          <h1 class="font-heading text-3xl lg:text-4xl font-bold text-ivory mb-3">{{ content().heading }}</h1>
          <p class="text-ivory/50 text-sm">Última actualización: {{ content().lastUpdate }}</p>
        </div>
      </div>

      <!-- Content -->
      <div class="container mx-auto max-w-3xl px-4 py-12">
        <p class="text-navy/70 leading-relaxed mb-10 text-base">{{ content().intro }}</p>

        <div class="space-y-8">
          @for (section of content().sections; track section.title) {
            <div class="border-l-2 border-gold/30 pl-6">
              <h2 class="font-heading text-xl font-bold text-navy mb-3">{{ section.title }}</h2>
              <p class="text-navy/70 leading-relaxed">{{ section.body }}</p>
            </div>
          }
        </div>

        <div class="mt-12 pt-8 border-t border-navy/10 flex gap-4">
          <a routerLink="/" class="btn-primary py-2.5 px-5 text-sm">Volver al inicio</a>
          <a routerLink="/vehiculos" class="btn-outline py-2.5 px-5 text-sm">Ver vehículos</a>
        </div>
      </div>
    </div>
  `
})
export class LegalPageComponent {
  private route = inject(ActivatedRoute);

  readonly content = toSignal(
    this.route.data.pipe(map(data => LEGAL_CONTENT[data['slug']] ?? LEGAL_CONTENT['aviso-legal'])),
    { initialValue: LEGAL_CONTENT['aviso-legal'] }
  );
}
