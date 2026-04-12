using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Hilo de mensajería entre dos usuarios sobre un vehículo concreto.</summary>
public class Conversation : AuditableEntity
{
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid VehicleId { get; set; }
    public bool IsArchivedByBuyer { get; set; }
    public bool IsArchivedBySeller { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }

    // Navigation
    public UserProfile Buyer { get; set; } = null!;
    public UserProfile Seller { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = [];
}
