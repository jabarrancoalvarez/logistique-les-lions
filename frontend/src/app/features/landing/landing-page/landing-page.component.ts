import {
  Component, OnInit, ChangeDetectionStrategy, inject
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { HeroSearchComponent } from '../hero-search/hero-search.component';
import { FeaturedVehiclesComponent } from '../featured-vehicles/featured-vehicles.component';
import { HowItWorksComponent } from '../how-it-works/how-it-works.component';
import { AuthService } from '@core/auth/auth.service';
import { IconComponent } from '@shared/components/icon/icon.component';

/**
 * Portada de Yoon u Auto.
 *
 * Cuenta las tres etapas del documento —trouver, négocier, garder— y nada más. Se han
 * retirado el mapa multi-país, los planes de suscripción y los testimonios inventados
 * del producto anterior: la plataforma es de Senegal, es gratuita, y no puede citar a
 * clientes que no existen.
 */
@Component({
  selector: 'lll-landing-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    HeroSearchComponent,
    FeaturedVehiclesComponent,
    HowItWorksComponent,
    IconComponent
  ],
  templateUrl: './landing-page.component.html'
})
export class LandingPageComponent implements OnInit {
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);
  private readonly auth = inject(AuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;

  /**
   * Carrocerías del catálogo.
   *
   * Sin recuentos: el número real depende de lo publicado en cada momento, y una cifra
   * inventada en la portada es una promesa que la búsqueda no cumple.
   */
  readonly vehicleCategories = [
    { id: 'Citadine',   emoji: '🚗', label: 'Citadines' },
    { id: 'Berline',    emoji: '🚘', label: 'Berlines' },
    { id: 'Suv',        emoji: '🚙', label: 'SUV / 4x4' },
    { id: 'Break',      emoji: '🚐', label: 'Breaks' },
    { id: 'PickUp',     emoji: '🛻', label: 'Pick-up' },
    { id: 'Utilitaire', emoji: '🚚', label: 'Utilitaires' }
  ];

  /** Las tres etapas del documento, tal cual. */
  readonly stages = [
    {
      number: '1',
      title: 'Trouvez votre voiture',
      text: 'Cherchez, filtrez et comparez. Enregistrez vos recherches et recevez ' +
            'une alerte quand la voiture que vous attendez est publiée.',
      link: '/vehiculos',
      cta: 'Voir les annonces'
    },
    {
      number: '2',
      title: 'Négociez et achetez',
      text: 'Discutez avec le vendeur, faites une offre, remplissez votre checklist ' +
            'd’inspection et signez un contrat vérifié par les deux parties.',
      link: '/vehiculos',
      cta: 'Comment ça marche'
    },
    {
      number: '3',
      title: 'Gardez tout dans Mon Garage',
      text: 'Papiers, entretiens, factures et rappels au même endroit. Le jour où ' +
            'vous revendez, tout est déjà prêt.',
      link: '/mi-garaje',
      cta: 'Mon Garage'
    }
  ];

  /**
   * Lo que distingue a Yoon u Auto.
   *
   * Todo lo de aquí está construido: nada es una promesa a futuro.
   */
  readonly advantages = [
    {
      icon: 'location',
      title: 'Pensé pour le Sénégal',
      text: 'Les 14 régions, les prix en FCFA et le statut douanier affiché sur ' +
            'chaque annonce.'
    },
    {
      icon: 'gift',
      title: 'Entièrement gratuit',
      text: 'Publier, chercher, négocier et acheter ne coûte rien. Sans limite ' +
            'd’annonces ni abonnement.'
    },
    {
      icon: 'document',
      title: 'Contrat et vente vérifiée',
      text: 'Un contrat validé par l’acheteur et le vendeur, avec PDF et code QR ' +
            'de vérification.'
    },
    {
      icon: 'chart',
      title: 'Indicateur de prix',
      text: 'Une comparaison statistique avec les véhicules semblables. Sans ' +
            'intelligence artificielle, sans invention.'
    }
  ];

  ngOnInit(): void {
    this.title.setTitle('Yoon u Auto — Achat et vente de véhicules au Sénégal');

    this.meta.updateTag({
      name: 'description',
      content: 'La plateforme sénégalaise pour acheter et vendre une voiture : ' +
               'annonces en FCFA, statut douanier, négociation, contrat vérifié et ' +
               'Mon Garage. Gratuit et sans limite.'
    });
    this.meta.updateTag({
      property: 'og:title',
      content: 'Yoon u Auto — Achat et vente de véhicules au Sénégal'
    });
    this.meta.updateTag({
      property: 'og:description',
      content: 'Trouvez votre voiture, négociez en confiance et gardez tout son ' +
               'historique dans Mon Garage. Gratuit.'
    });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
  }
}
