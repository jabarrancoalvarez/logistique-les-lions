using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Oferta económica dentro de una negociación.
/// </summary>
/// <remarks>
/// No es un módulo aparte: pertenece siempre a una negociación, igual que el chat y el
/// contrato. Una contraoferta es simplemente una oferta nueva de la otra parte, que deja
/// la anterior en estado <see cref="OfferStatus.ContreOfferte"/>.
/// </remarks>
public class Offer : AuditableEntity
{
    public Guid NegotiationId { get; set; }

    /// <summary>Quien hace la oferta.</summary>
    public Guid FromUserId { get; set; }

    /// <summary>Importe ofrecido en FCFA.</summary>
    public decimal Amount { get; set; }

    /// <summary>Precio publicado en el momento de ofertar, para poder comparar después.</summary>
    public decimal ListedPrice { get; set; }

    public string? Message { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.EnAttente;
    public DateTimeOffset? RespondedAt { get; set; }

    /// <summary>Oferta a la que responde esta contraoferta, si lo es.</summary>
    public Guid? RepliesToOfferId { get; set; }

    public Negotiation Negotiation { get; set; } = null!;
    public UserProfile From { get; set; } = null!;

    /// <summary>Solo una oferta pendiente admite respuesta.</summary>
    public bool IsPending => Status == OfferStatus.EnAttente;
}
