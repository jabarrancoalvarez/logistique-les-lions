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

/**
 * Aviso que encabeza las cinco páginas legales.
 *
 * Estos textos son **provisionales**: sustituyen a unos anteriores que describían una
 * sociedad española con domicilio en Madrid y citaban la Ley 34/2002 y el RGPD, que no
 * aplican a una plataforma senegalesa. Están redactados en francés y apuntan a la
 * normativa de Senegal, pero **no los ha revisado un abogado** y los datos de la empresa
 * siguen sin rellenar: aparecen como «[à compléter]» a propósito, para que nadie los
 * confunda con información real.
 */
const PROVISIONAL =
  'Ce texte est provisoire et doit être validé par un conseil juridique au Sénégal ' +
  'avant l’ouverture au public. Les mentions « [à compléter] » attendent les ' +
  'informations officielles de la société.';

const LEGAL_CONTENT: Record<string, LegalContent> = {
  'aviso-legal': {
    heading: 'Mentions légales',
    lastUpdate: 'Août 2026',
    intro: PROVISIONAL,
    sections: [
      {
        title: '1. Éditeur du site',
        body: 'Le présent site est édité par Yoon u Auto. Forme juridique : [à compléter]. Siège social : [à compléter], Sénégal. Numéro d’identification (NINEA) : [à compléter]. Registre du Commerce et du Crédit Mobilier (RCCM) : [à compléter]. Contact : [à compléter].'
      },
      {
        title: '2. Objet de la plateforme',
        body: 'Yoon u Auto est une plateforme sénégalaise de petites annonces de véhicules d’occasion. Elle met en relation des personnes qui vendent et des personnes qui achètent, met à leur disposition un espace de négociation et un contrat de vente, et permet à chacun de tenir le carnet d’entretien de son véhicule. L’accès au site vaut acceptation des présentes mentions.'
      },
      {
        title: '3. Rôle de Yoon u Auto',
        body: 'Yoon u Auto n’est ni vendeur, ni acheteur, ni mandataire des parties. Elle n’est pas partie au contrat de vente conclu entre elles et ne détient pas les fonds. Les véhicules, leur état et les informations publiées relèvent de la seule responsabilité de la personne qui publie l’annonce.'
      },
      {
        title: '4. Propriété intellectuelle',
        body: 'La marque Yoon u Auto, le logo, la charte graphique et les développements du site sont protégés. Les photographies et les textes des annonces restent la propriété de leurs auteurs, qui concèdent à Yoon u Auto le droit de les afficher sur la plateforme le temps de la publication.'
      },
      {
        title: '5. Hébergement',
        body: 'Le site et ses données sont hébergés par des prestataires situés hors du Sénégal. Le détail des prestataires et des pays d’hébergement figure dans la politique de confidentialité.'
      },
      {
        title: '6. Droit applicable',
        body: 'Les présentes mentions sont régies par le droit sénégalais. À défaut de règlement amiable, les tribunaux compétents de Dakar seront saisis.'
      }
    ]
  },
  'privacidad': {
    heading: 'Politique de confidentialité',
    lastUpdate: 'Août 2026',
    intro: PROVISIONAL,
    sections: [
      {
        title: '1. Responsable du traitement',
        body: 'Yoon u Auto, [à compléter], Sénégal. Pour toute question relative à vos données : [à compléter].'
      },
      {
        title: '2. Cadre légal',
        body: 'Les traitements sont réalisés conformément à la loi n° 2008-12 du 25 janvier 2008 sur la protection des données à caractère personnel, sous le contrôle de la Commission de Protection des Données Personnelles (CDP) du Sénégal.'
      },
      {
        title: '3. Données collectées',
        body: 'Le numéro de téléphone, qui identifie le compte ; le nom affiché ; l’adresse e-mail, facultative et utilisée uniquement pour les notifications ; la ville et la région ; les annonces publiées et leurs photographies ; les messages et les offres échangés dans une négociation ; le contenu de Mon Garage ; et des données techniques de connexion.'
      },
      {
        title: '4. Finalités',
        body: 'Tenir votre compte, publier vos annonces, vous mettre en relation avec l’autre partie, produire le contrat de vente, vous avertir de ce qui vous concerne (baisse de prix, nouvelle annonce correspondant à une recherche enregistrée, échéance d’entretien), et prévenir la fraude.'
      },
      {
        title: '5. Ce que nous ne faisons pas',
        body: 'Nous ne vendons pas vos données. Nous n’affichons jamais votre numéro de téléphone à une personne non inscrite. Le contenu de Mon Garage est privé : ni les autres utilisateurs ni l’administration n’y accèdent, et seul ce que vous cochez expressément apparaît sur une annonce. Le contenu de vos conversations n’est consultable par l’administration que pour un motif justifié, qui est enregistré avec le nom de l’administrateur.'
      },
      {
        title: '6. Durée de conservation',
        body: 'Vos données sont conservées tant que votre compte existe. Après suppression, seules sont conservées les informations que la loi impose de garder, notamment celles rattachées à un contrat de vente déjà conclu.'
      },
      {
        title: '7. Vos droits',
        body: 'Vous pouvez demander l’accès à vos données, leur rectification, leur effacement, ou vous opposer à un traitement, en écrivant à [à compléter]. Vous pouvez également saisir la Commission de Protection des Données Personnelles (CDP).'
      },
      {
        title: '8. Transferts hors du Sénégal',
        body: 'Nos prestataires techniques (hébergement, base de données, envoi de notifications) sont situés hors du Sénégal. Les transferts sont limités à ce qui est nécessaire au fonctionnement du service.'
      }
    ]
  },
  'cookies': {
    heading: 'Politique de cookies',
    lastUpdate: 'Août 2026',
    intro: PROVISIONAL,
    sections: [
      {
        title: 'Ce qu’est un cookie',
        body: 'Un cookie est un petit fichier déposé par le site dans votre navigateur. Il lui permet de se souvenir de certaines choses d’une page à l’autre, par exemple que vous êtes connecté.'
      },
      {
        title: 'Cookies strictement nécessaires',
        body: 'Ils font fonctionner le site et ne peuvent pas être désactivés : maintien de votre session une fois connecté, mémorisation de votre choix concernant les cookies, et sécurité des formulaires.'
      },
      {
        title: 'Stockage local',
        body: 'Votre sélection du comparateur est conservée dans votre navigateur, et non sur nos serveurs. Elle ne vous suit donc pas d’un appareil à l’autre, et disparaît si vous effacez les données du site.'
      },
      {
        title: 'Mesure d’audience',
        body: 'Aucune mesure d’audience tierce n’est active à ce jour. Si elle le devenait, cette page serait mise à jour et votre consentement vous serait demandé au préalable.'
      },
      {
        title: 'Gérer vos cookies',
        body: 'Vous pouvez à tout moment supprimer les cookies déposés et configurer votre navigateur pour les refuser. En refusant les cookies nécessaires, vous ne pourrez plus rester connecté.'
      }
    ]
  },
  'terminos': {
    heading: 'Conditions générales d’utilisation',
    lastUpdate: 'Août 2026',
    intro: PROVISIONAL,
    sections: [
      {
        title: '1. Objet',
        body: 'Les présentes conditions régissent l’utilisation de Yoon u Auto. En créant un compte, vous les acceptez.'
      },
      {
        title: '2. Un service gratuit',
        body: 'Publier une annonce, la mettre en avant, chercher, négocier, établir un contrat et utiliser Mon Garage sont gratuits et sans limite de nombre. Aucun abonnement n’est proposé. Le fait de se déclarer particulier ou professionnel n’ouvre aucun droit supplémentaire.'
      },
      {
        title: '3. Compte',
        body: 'Le compte est identifié par un numéro de téléphone sénégalais. Vous êtes responsable de votre mot de passe et de ce qui est fait depuis votre compte. Signalez-nous sans délai toute utilisation que vous n’auriez pas autorisée.'
      },
      {
        title: '4. Vos annonces',
        body: 'Vous garantissez que le véhicule existe, qu’il vous appartient ou que vous êtes autorisé à le vendre, et que les informations publiées — kilométrage, année, prix — sont exactes. Chaque changement de prix est conservé dans l’historique de l’annonce.'
      },
      {
        title: '5. Modération',
        body: 'Une annonce peut être masquée, signalée pour révision ou archivée si elle enfreint ces conditions ; la mesure est motivée et vous en êtes informé. L’administration ne modifie jamais elle-même le titre, le prix ni la description d’une annonce : elle vous demande de les corriger.'
      },
      {
        title: '6. Négociation et contrat',
        body: 'La négociation, les offres et le contrat se déroulent entre les deux parties. Le contrat est rédigé par l’une et validé par l’autre : personne ne peut valider son propre contrat, et l’administration ne valide jamais à la place des parties. Yoon u Auto ne garantit ni le paiement, ni la remise du véhicule, ni la régularité des documents.'
      },
      {
        title: '7. Responsabilité',
        body: 'Yoon u Auto met un outil à disposition. Elle ne répond pas de l’état réel des véhicules, de l’exactitude des annonces ni de l’issue des transactions. Vérifiez le véhicule et ses papiers avant de payer.'
      },
      {
        title: '8. Modification des conditions',
        body: 'Ces conditions peuvent évoluer. Tout changement de version est daté et vous en êtes informé sur la plateforme. Continuer à utiliser le service vaut acceptation de la nouvelle version.'
      }
    ]
  },
  'rgpd': {
    heading: 'Protection des données',
    lastUpdate: 'Août 2026',
    intro: PROVISIONAL,
    sections: [
      {
        title: 'Texte applicable',
        body: 'Yoon u Auto est une plateforme sénégalaise : les données personnelles y sont traitées selon la loi n° 2008-12 du 25 janvier 2008, sous le contrôle de la Commission de Protection des Données Personnelles (CDP). Le règlement européen (RGPD) ne s’applique pas à ce service ; il ne concernerait que des personnes résidant dans l’Union européenne.'
      },
      {
        title: 'Base des traitements',
        body: 'Les traitements reposent sur l’exécution du service que vous demandez en créant un compte, sur le respect des obligations légales, et sur votre consentement pour ce qui est facultatif, comme les notifications par e-mail.'
      },
      {
        title: 'Catégories de données',
        body: 'Données d’identification (téléphone, nom affiché, e-mail facultatif), de localisation déclarée (région et ville), d’activité sur la plateforme (annonces, favoris, recherches enregistrées, négociations, contrats) et le contenu privé de Mon Garage. Aucune donnée sensible n’est demandée.'
      },
      {
        title: 'Destinataires',
        body: 'Nos prestataires techniques, dans la stricte mesure nécessaire au fonctionnement du service, et les autorités compétentes lorsque la loi l’impose. Aucune cession commerciale à des tiers.'
      },
      {
        title: 'Exercer vos droits',
        body: 'Accès, rectification, effacement, opposition et limitation : écrivez à [à compléter]. Une réclamation peut être adressée à la Commission de Protection des Données Personnelles (CDP) du Sénégal.'
      },
      {
        title: 'Sécurité',
        body: 'Les mots de passe sont stockés sous forme chiffrée, les échanges avec le site sont protégés, et les documents de Mon Garage ne sont accessibles qu’à leur propriétaire, authentifié.'
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
    <div class="min-h-screen bg-frost">
      <!-- Header -->
      <div class="bg-navy py-16 px-4">
        <div class="container mx-auto max-w-3xl">
          <a routerLink="/" class="inline-flex items-center gap-2 text-frost/60 hover:text-azure text-sm mb-6 transition-colors">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
            </svg>
            Retour à l’accueil
          </a>
          <h1 class="font-heading text-3xl lg:text-4xl font-bold text-frost mb-3">{{ content().heading }}</h1>
          <p class="text-frost/50 text-sm">Dernière mise à jour : {{ content().lastUpdate }}</p>
        </div>
      </div>

      <!-- Content -->
      <div class="container mx-auto max-w-3xl px-4 py-12">
        <!-- El aviso de provisionalidad no puede leerse como un párrafo más: mientras los
             datos de la sociedad estén sin rellenar, tiene que verse antes que el texto. -->
        <p class="mb-10 rounded-xl border-l-4 border-azure-dark bg-frost-dark px-5 py-4
                  text-base leading-relaxed text-navy">
          <span class="font-semibold">Document provisoire.</span>
          {{ content().intro }}
        </p>

        <div class="space-y-8">
          @for (section of content().sections; track section.title) {
            <div class="border-l-2 border-azure/30 pl-6">
              <h2 class="font-heading text-xl font-bold text-navy mb-3">{{ section.title }}</h2>
              <p class="text-navy/70 leading-relaxed">{{ section.body }}</p>
            </div>
          }
        </div>

        <div class="mt-12 pt-8 border-t border-navy/10 flex gap-4">
          <a routerLink="/" class="btn-primary py-2.5 px-5 text-sm">Retour à l’accueil</a>
          <a routerLink="/vehiculos" class="btn-outline py-2.5 px-5 text-sm">Voir les annonces</a>
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
