namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Crea notificaciones in-app y las empuja a los clientes conectados vía SignalR.
/// La implementación vive en API/Hubs para mantener Application desacoplada de SignalR.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(
        Guid userId,
        string category,
        string title,
        string? body = null,
        string? link = null,
        CancellationToken cancellationToken = default);
}
