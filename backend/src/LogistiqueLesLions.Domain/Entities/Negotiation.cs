using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Negociación: todo lo que sucede desde que un usuario demuestra interés real por un
/// vehículo hasta que la operación termina.
/// </summary>
/// <remarks>
/// Es el agregado raíz de la Etapa 2. La especificación es explícita: no existen módulos
/// independientes de mensajes, ofertas y contratos — <b>todo pertenece a una
/// negociación</b>, que relaciona siempre usuario interesado ↔ anuncio ↔ quien publica.
///
/// Comienza cuando el interesado envía un mensaje, solicita información adicional o
/// hace una oferta.
/// </remarks>
public class Negotiation : AuditableEntity
{
    /// <summary>Usuario interesado.</summary>
    public Guid BuyerId { get; set; }
    /// <summary>Usuario que publica el anuncio.</summary>
    public Guid SellerId { get; set; }
    public Guid VehicleId { get; set; }

    public NegotiationStatus Status { get; set; } = NegotiationStatus.EnCours;

    public bool IsArchivedByBuyer { get; set; }
    public bool IsArchivedBySeller { get; set; }

    public DateTimeOffset? LastMessageAt { get; set; }
    /// <summary>Última actividad de cualquier tipo: mensaje, oferta o movimiento contractual.</summary>
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    // ─── Navegación ────────────────────────────────────────────────────────
    public UserProfile Buyer { get; set; } = null!;
    public UserProfile Seller { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<NegotiationEvent> Events { get; set; } = [];
    public ICollection<Offer> Offers { get; set; } = [];

    /// <summary>El usuario indicado participa en esta negociación.</summary>
    public bool Involves(Guid userId) => BuyerId == userId || SellerId == userId;
}

/// <summary>
/// Hito de la cronología de una negociación.
/// </summary>
/// <remarks>
/// Append-only: los hitos nunca se modifican ni se borran, porque la línea de tiempo es
/// el registro de lo que ocurrió en la operación.
/// </remarks>
public class NegotiationEvent : AuditableEntity
{
    public Guid NegotiationId { get; set; }

    /// <summary>
    /// Posición dentro de la cronología, empezando en 1.
    /// </summary>
    /// <remarks>
    /// No basta con ordenar por <c>CreatedAt</c>: varios hitos generados en la misma
    /// operación comparten marca de tiempo —el interceptor de auditoría la fija una vez
    /// por guardado— y la línea de tiempo saldría desordenada.
    /// </remarks>
    public int Sequence { get; set; }

    public NegotiationEventType Type { get; set; }

    /// <summary>Quien provocó el hito. <c>null</c> si lo generó el sistema.</summary>
    public Guid? ActorId { get; set; }

    /// <summary>Importe asociado, cuando el hito es una oferta o contraoferta.</summary>
    public decimal? Amount { get; set; }

    public Negotiation Negotiation { get; set; } = null!;
    public UserProfile? Actor { get; set; }
}
