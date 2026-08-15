namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Estado de una negociación. La especificación organiza la lista en tres grupos:
/// En cours · En attente · Terminées.
/// </summary>
public enum NegotiationStatus
{
    /// <summary>Hay actividad reciente entre las partes.</summary>
    EnCours = 1,
    /// <summary>Esperando respuesta de la otra parte: una oferta o un contrato pendiente.</summary>
    EnAttente = 2,
    /// <summary>Cerrada: venta verificada, rechazo o abandono.</summary>
    Terminee = 3
}

/// <summary>
/// Hitos de la cronología única de una negociación.
/// </summary>
/// <remarks>
/// La especificación muestra la operación entera como una sola línea de tiempo, desde
/// «Conversation commencée» hasta «Vente vérifiée ✓». Cada funcionalidad de la Etapa 2
/// va añadiendo sus hitos al mismo hilo.
/// </remarks>
public enum NegotiationEventType
{
    /// <summary>Conversation commencée.</summary>
    ConversationStarted = 1,
    /// <summary>Offre de X FCFA.</summary>
    OfferMade = 2,
    /// <summary>Contre-offre de X FCFA.</summary>
    CounterOffer = 3,
    /// <summary>Offre acceptée.</summary>
    OfferAccepted = 4,
    /// <summary>Offre refusée.</summary>
    OfferRejected = 5,
    /// <summary>Contrat créé.</summary>
    ContractCreated = 6,
    /// <summary>Modification demandée.</summary>
    ContractChangeRequested = 7,
    /// <summary>Contrat validé.</summary>
    ContractValidated = 8,
    /// <summary>Vente vérifiée ✓.</summary>
    SaleVerified = 9
}
