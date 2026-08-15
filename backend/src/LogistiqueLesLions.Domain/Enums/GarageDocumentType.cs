namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Clasificación de un documento de Mon Garage.
/// </summary>
/// <remarks>
/// La lista sale de la especificación. «Autre» existe para que ningún papel se quede
/// fuera: la ficha documental no debe obligar a inventarse una categoría.
/// </remarks>
public enum GarageDocumentType
{
    /// <summary>Contrat de vente.</summary>
    ContratDeVente = 1,
    /// <summary>Carte grise / documentación del vehículo.</summary>
    CarteGrise = 2,
    /// <summary>Documentación aduanera.</summary>
    Douane = 3,
    /// <summary>Assurance.</summary>
    Assurance = 4,
    /// <summary>Contrôle technique / inspecciones.</summary>
    ControleTechnique = 5,
    /// <summary>Facture d'entretien.</summary>
    FactureEntretien = 6,
    /// <summary>Facture de réparation.</summary>
    FactureReparation = 7,
    /// <summary>Facture d'achat.</summary>
    FactureAchat = 8,
    /// <summary>Autre document.</summary>
    Autre = 99
}
