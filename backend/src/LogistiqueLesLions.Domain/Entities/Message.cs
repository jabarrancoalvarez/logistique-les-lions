using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Mensaje dentro de una conversación.</summary>
public class Message : AuditableEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public UserProfile Sender { get; set; } = null!;
}
