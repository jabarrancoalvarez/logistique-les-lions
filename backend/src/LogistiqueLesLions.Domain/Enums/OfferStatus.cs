namespace LogistiqueLesLions.Domain.Enums;

/// <summary>Situación de una oferta dentro de la negociación.</summary>
public enum OfferStatus
{
    /// <summary>Esperando respuesta de la otra parte.</summary>
    EnAttente = 1,
    Acceptee = 2,
    Refusee = 3,
    /// <summary>Superada por una contraoferta de la otra parte.</summary>
    ContreOfferte = 4,
    /// <summary>Retirada por quien la hizo antes de recibir respuesta.</summary>
    Retiree = 5
}
