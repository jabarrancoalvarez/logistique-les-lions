using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;

namespace LogistiqueLesLions.Application.Services;

/// <summary>
/// Envío en vivo de notificaciones recién persistidas.
/// </summary>
/// <remarks>
/// El <see cref="INotificationPusher"/> es opcional en los servicios que lo usan: en los
/// tests no hay SignalR, y la notificación ya está guardada, así que la ausencia de push
/// no debe impedir que la lógica de negocio se pueda probar.
/// </remarks>
internal static class NotificationPushExtensions
{
    public static Task PushAsync(
        this INotificationPusher? pusher,
        IReadOnlyCollection<UserNotification> notifications,
        CancellationToken ct)
    {
        if (pusher is null || notifications.Count == 0) return Task.CompletedTask;

        var payload = notifications
            .Select(n => new PushedNotification(n.Id, n.UserId, n.Category, n.Title, n.Body, n.Link))
            .ToList();

        return pusher.PushAsync(payload, ct);
    }
}
