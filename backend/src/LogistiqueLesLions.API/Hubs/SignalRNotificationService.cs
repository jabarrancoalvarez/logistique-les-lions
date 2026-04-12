using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace LogistiqueLesLions.API.Hubs;

/// <summary>
/// Implementación del INotificationService apoyada en SignalR.
/// Persiste la notificación y la empuja en tiempo real al destinatario si está conectado.
/// </summary>
public class SignalRNotificationService(
    IApplicationDbContext db,
    IHubContext<NotificationHub> hub,
    ILogger<SignalRNotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(
        Guid userId, string category, string title,
        string? body = null, string? link = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new UserNotification
        {
            UserId   = userId,
            Category = category,
            Title    = title,
            Body     = body,
            Link     = link,
            IsRead   = false
        };

        db.UserNotifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync("notification", new
            {
                id        = notification.Id,
                category,
                title,
                body,
                link,
                createdAt = notification.CreatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            // No romper el flujo de negocio si SignalR falla — la notificación persiste
            logger.LogWarning(ex, "Push SignalR falló para usuario {UserId}", userId);
        }
    }
}
