// Etiquetas en francés de los enums de negociación (la API los envía como strings).

String negotiationStatusLabel(String? v) => switch (v) {
      'EnCours' => 'En cours',
      'EnAttente' => 'En attente',
      'Terminee' => 'Terminée',
      _ => '—',
    };

String offerStatusLabel(String? v) => switch (v) {
      'EnAttente' => 'En attente',
      'Acceptee' => 'Acceptée',
      'Refusee' => 'Refusée',
      'ContreOfferte' => 'Contre-offre',
      'Retiree' => 'Retirée',
      _ => '—',
    };

/// Texto del hito de la cronología. `byMe` ajusta la voz.
String eventLabel(String? type, {required bool byMe, String? amountLabel}) {
  final who = byMe ? 'Vous' : 'L’autre partie';
  return switch (type) {
    'ConversationStarted' => 'Conversation démarrée',
    'OfferMade' =>
      '$who ${byMe ? 'avez' : 'a'} fait une offre${amountLabel != null ? ' : $amountLabel' : ''}',
    'CounterOffer' =>
      '$who ${byMe ? 'avez' : 'a'} fait une contre-offre${amountLabel != null ? ' : $amountLabel' : ''}',
    'OfferAccepted' => '$who ${byMe ? 'avez' : 'a'} accepté l’offre',
    'OfferRejected' => '$who ${byMe ? 'avez' : 'a'} refusé l’offre',
    'ContractCreated' => 'Contrat créé',
    'ContractChangeRequested' => 'Modification du contrat demandée',
    'ContractValidated' => 'Contrat validé',
    'SaleVerified' => 'Vente vérifiée ✓',
    _ => 'Mise à jour',
  };
}
