import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';

type UserType = 'buyer' | 'seller';

@Component({
  selector: 'lll-how-it-works',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, NgClass],
  templateUrl: './how-it-works.component.html'
})
export class HowItWorksComponent {
  readonly activeTab = signal<UserType>('buyer');

  readonly buyerSteps = [
    {
      step: '01',
      icon: '🔍',
      title: 'Busca y compara',
      description: 'Explora miles de vehículos verificados de toda Europa y más allá. Filtra por marca, país, precio y tipo de combustible.'
    },
    {
      step: '02',
      icon: '📋',
      title: 'Calcula el coste total',
      description: 'Nuestra calculadora te muestra el precio final real: aranceles, IVA, homologación, transporte y tasas de matriculación.'
    },
    {
      step: '03',
      icon: '📄',
      title: 'Gestión documental automática',
      description: 'La plataforma genera el checklist completo de documentos necesarios para importar el vehículo a tu país.'
    },
    {
      step: '04',
      icon: '🔒',
      title: 'Pago seguro con escrow',
      description: 'Tu dinero queda protegido hasta confirmar la recepción del vehículo y la documentación en regla.'
    },
    {
      step: '05',
      icon: '🚛',
      title: 'Transporte y entrega',
      description: 'Coordinamos el transporte desde origen hasta tu puerta. Seguimiento en tiempo real incluido.'
    },
    {
      step: '06',
      icon: '✅',
      title: 'Matriculación y disfrute',
      description: 'Con todos los documentos homologados y validados, registra el vehículo y a disfrutar.'
    }
  ];

  readonly sellerSteps = [
    {
      step: '01',
      icon: '📸',
      title: 'Publica tu anuncio',
      description: 'Describe tu vehículo con hasta 40 fotos. Nuestro asistente IA sugiere el precio óptimo de mercado internacional.'
    },
    {
      step: '02',
      icon: '✔️',
      title: 'Verificación del vehículo',
      description: 'Sube la documentación del vehículo. Nuestros verificadores comprueban la autenticidad y el historial.'
    },
    {
      step: '03',
      icon: '🌍',
      title: 'Visibilidad internacional',
      description: 'Tu anuncio llega a compradores en España, Alemania, Francia, Marruecos y 8 países más simultáneamente.'
    },
    {
      step: '04',
      icon: '💬',
      title: 'Comunícate con compradores',
      description: 'Chat con traducción automática integrada. Gestiona ofertas y responde preguntas en tu idioma.'
    },
    {
      step: '05',
      icon: '📦',
      title: 'Documentación de exportación',
      description: 'La plataforma genera automáticamente todos los documentos de exportación requeridos por el país destino.'
    },
    {
      step: '06',
      icon: '💰',
      title: 'Recibe el pago',
      description: 'Una vez el comprador confirma la recepción, el pago se libera del escrow a tu cuenta en 24-48h.'
    }
  ];

  get activeSteps() {
    return this.activeTab() === 'buyer' ? this.buyerSteps : this.sellerSteps;
  }
}
