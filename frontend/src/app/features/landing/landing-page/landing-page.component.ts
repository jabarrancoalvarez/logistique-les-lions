import { Component, OnInit, ChangeDetectionStrategy, inject, signal, AfterViewInit, ElementRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { HeroSearchComponent } from '../hero-search/hero-search.component';
import { FeaturedVehiclesComponent } from '../featured-vehicles/featured-vehicles.component';
import { HowItWorksComponent } from '../how-it-works/how-it-works.component';
import { CountryMapComponent } from '../country-map/country-map.component';
import { NewsletterComponent } from '../newsletter/newsletter.component';
import { StatsCountersComponent } from '../stats-counters/stats-counters.component';

@Component({
  selector: 'lll-landing-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    HeroSearchComponent,
    StatsCountersComponent,
    FeaturedVehiclesComponent,
    HowItWorksComponent,
    CountryMapComponent,
    NewsletterComponent
  ],
  templateUrl: './landing-page.component.html'
})
export class LandingPageComponent implements OnInit, AfterViewInit {
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);

  readonly vehicleCategories = [
    { id: 'Sedan', emoji: '🚗', label: 'Turismos', count: 340 },
    { id: 'SUV', emoji: '🚙', label: 'SUV / 4x4', count: 280 },
    { id: 'Van', emoji: '🚐', label: 'Furgonetas', count: 95 },
    { id: 'Motorcycle', emoji: '🏍️', label: 'Motos', count: 120 },
    { id: 'Truck', emoji: '🚛', label: 'Camiones', count: 45 },
    { id: 'Electric', emoji: '⚡', label: 'Eléctricos', count: 160 }
  ];

  readonly pricingPlans = [
    {
      tag: 'Para compradores',
      name: 'Particular',
      price: 'Gratis',
      period: 'siempre',
      description: 'Busca, contacta y compra vehículos importados sin coste.',
      highlighted: false,
      cta: 'Empezar gratis',
      features: [
        'Hasta 3 anuncios publicados',
        'Búsqueda avanzada con todos los filtros',
        'Contacto directo con vendedores',
        'Guardado de favoritos',
        'Alertas de nuevos vehículos'
      ]
    },
    {
      tag: 'Para vendedores',
      name: 'Profesional',
      price: '49€',
      period: '/ mes',
      description: 'Para importadores y vendedores con volumen de vehículos.',
      highlighted: true,
      cta: 'Empezar 14 días gratis',
      features: [
        'Anuncios ilimitados',
        'Posición destacada en búsquedas',
        'Calculadora avanzada de importación',
        'Estadísticas de tus anuncios',
        'Soporte prioritario por chat',
        'Badge de vendedor verificado'
      ]
    },
    {
      tag: 'Para empresas',
      name: 'Concesionario',
      price: '199€',
      period: '/ mes',
      description: 'Gestión completa de flota y documentación para empresas.',
      highlighted: false,
      cta: 'Contactar con ventas',
      features: [
        'Todo lo del plan Profesional',
        'Página de concesionario personalizada',
        'API para importación de stock',
        'Hasta 10 usuarios por cuenta',
        'Gestor de cuenta dedicado',
        'Informes mensuales de mercado'
      ]
    }
  ];

  readonly testimonials = [
    {
      id: 1,
      text: 'Importé un BMW Serie 3 desde Alemania sin salir de casa. La plataforma gestionó todo: homologación, aranceles, ITV. En 3 semanas el coche estaba en mi garaje.',
      name: 'Carlos M.',
      initials: 'CM',
      role: 'Comprador privado',
      country: '🇪🇸 España'
    },
    {
      id: 2,
      text: 'Como concesionario, hemos abierto un canal de importación desde Japón que antes era impensable. La documentación que genera la plataforma es impecable.',
      name: 'Jean-Pierre D.',
      initials: 'JP',
      role: 'Concesionario',
      country: '🇫🇷 Francia'
    },
    {
      id: 3,
      text: 'Vendí mi Range Rover a un comprador en Alemania. El escrow me dio seguridad total. Los papeles de exportación se generaron automáticamente.',
      name: 'Fatima A.',
      initials: 'FA',
      role: 'Vendedora privada',
      country: '🇲🇦 Marruecos'
    }
  ];

  ngOnInit(): void {
    this.title.setTitle('Logistique Les Lions — Compraventa Internacional de Vehículos');
    this.meta.updateTag({ name: 'description', content: 'Plataforma líder en importación y exportación de vehículos entre España, Alemania, Francia, Marruecos y más de 12 países. Gestión documental completa: aduanas, homologaciones, trámites.' });
    this.meta.updateTag({ property: 'og:title', content: 'Logistique Les Lions — Compraventa Internacional de Vehículos' });
    this.meta.updateTag({ property: 'og:description', content: 'Compra y vende vehículos a nivel internacional con toda la documentación gestionada.' });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
  }

  ngAfterViewInit(): void {
    // Intersection Observer para animaciones al scroll
    const observer = new IntersectionObserver(
      (entries) => entries.forEach(e => {
        if (e.isIntersecting) {
          e.target.classList.add('visible');
          observer.unobserve(e.target);
        }
      }),
      { threshold: 0.15 }
    );
    document.querySelectorAll('.reveal').forEach(el => observer.observe(el));
  }
}
