using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LogistiqueLesLions.API.Hubs;

/// <inheritdoc />
public class SignalRNotificationPusher(
    IHubContext<NotificationHub> hub,
    ILogger<SignalRNotificationPusher> logger) : INotificationPusher
{
    public async Task PushAsync(
        IReadOnlyCollection<PushedNotification> notifications, CancellationToken ct = default)
    {
        foreach (var n in notifications)
        {
            try
            {
                await hub.Clients.User(n.UserId.ToString()).SendAsync("notification", new
                {
                    id = n.Id,
                    category = n.Category,
                    title = n.Title,
                    body = n.Body,
                    link = n.Link
                }, ct);
            }
            catch (Exception ex)
            {
                // La notificación ya está persistida: el usuario la verá al abrir la
                // campana aunque el envío en vivo falle.
                logger.LogWarning(ex, "Push SignalR falló para el usuario {UserId}", n.UserId);
            }
        }
    }
}
