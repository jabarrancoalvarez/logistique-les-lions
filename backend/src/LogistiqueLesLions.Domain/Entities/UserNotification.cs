using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Notificación in-app para un usuario. Persiste hasta que se lee/archiva.
/// El delivery en tiempo real va por SignalR (NotificationHub) — la persistencia
/// asegura que las notificaciones generadas mientras el usuario estaba offline
/// estén disponibles cuando vuelva.
/// </summary>
public class UserNotification : AuditableEntity
{
    public Guid UserId { get; set; }

    /// <summary>Categoría libre: incident, process, message, system, marketplace.</summary>
    public string Category { get; set; } = "system";

    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }

    /// <summary>URL relativa del frontend a la que enlaza la notificación (ej: /admin/incidents/{id}).</summary>
    public string? Link { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
