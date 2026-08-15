import { Component, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';

type UserType = 'buyer' | 'seller';

/**
 * «Comment ça marche», visto desde cada lado.
 *
 * Cada paso corresponde a algo que la aplicación hace hoy. ❌ Nada de escrow, de
 * transporte ni de traducción automática: eso era el producto anterior y no existe.
 */
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
      title: 'Cherchez et comparez',
      description: 'Filtrez par marque, région, budget, carburant et statut douanier. ' +
                   'Comparez plusieurs voitures côte à côte.'
    },
    {
      step: '02',
      icon: '🔔',
      title: 'Enregistrez votre recherche',
      description: 'Recevez une alerte dès qu\'une voiture correspondant à vos ' +
                   'critères est publiée, et quand un favori baisse de prix.'
    },
    {
      step: '03',
      icon: '💬',
      title: 'Contactez le vendeur',
      description: 'Ouvrez une négociation, posez vos questions et suivez toute ' +
                   'la discussion au même endroit.'
    },
    {
      step: '04',
      icon: '💰',
      title: 'Faites une offre',
      description: 'Proposez votre prix, recevez une contre-offre et mettez-vous ' +
                   'd\'accord. Chaque étape reste inscrite dans la chronologie.'
    },
    {
      step: '05',
      icon: '📝',
      title: 'Vérifiez avant d\'acheter',
      description: 'Remplissez votre checklist d\'inspection. Elle est privée : ' +
                   'le vendeur ne la voit pas.'
    },
    {
      step: '06',
      icon: '✅',
      title: 'Signez le contrat',
      description: 'Le contrat est validé par les deux parties, avec PDF et code QR. ' +
                   'La voiture entre automatiquement dans votre Mon Garage.'
    }
  ];

  readonly sellerSteps = [
    {
      step: '01',
      icon: '📸',
      title: 'Publiez votre annonce',
      description: 'Photos, fiche technique, prix en FCFA et statut douanier. ' +
                   'Gratuit et sans limite d\'annonces.'
    },
    {
      step: '02',
      icon: '📊',
      title: 'Situez votre prix',
      description: 'L\'indicateur compare votre prix à celui des voitures ' +
                   'semblables. Un calcul statistique, sans intelligence artificielle.'
    },
    {
      step: '03',
      icon: '⭐',
      title: 'Soignez la qualité de l\'annonce',
      description: 'Un score vous indique ce qu\'il manque : photos, description, ' +
                   'équipements. Une annonce complète est plus consultée.'
    },
    {
      step: '04',
      icon: '💬',
      title: 'Répondez aux acheteurs',
      description: 'Toutes les négociations réunies, avec des réponses types pour ' +
                   'les questions qui reviennent.'
    },
    {
      step: '05',
      icon: '🤝',
      title: 'Acceptez une offre',
      description: 'Comparez les offres reçues, contre-proposez et concluez. ' +
                   'Vous restez maître de votre prix.'
    },
    {
      step: '06',
      icon: '🏅',
      title: 'Vente vérifiée',
      description: 'Le contrat validé ajoute une vente vérifiée à votre profil. ' +
                   'C\'est la réputation qui rassure le prochain acheteur.'
    }
  ];

  readonly activeSteps = computed(() =>
    this.activeTab() === 'buyer' ? this.buyerSteps : this.sellerSteps);
}
