namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Valoración del precio de un anuncio frente a vehículos comparables.
/// </summary>
/// <remarks>
/// Se calcula de forma estadística, <b>sin IA</b>. Si no hay suficientes comparables no
/// se asigna ningún valor: la especificación prohíbe expresamente mostrar un indicador
/// inventado.
/// </remarks>
public enum PriceIndicator
{
    /// <summary>Bonne affaire — claramente por debajo de la referencia.</summary>
    BonneAffaire = 1,
    /// <summary>Prix correct — en línea con la referencia.</summary>
    PrixCorrect = 2,
    /// <summary>Prix élevé — claramente por encima de la referencia.</summary>
    PrixEleve = 3
}
