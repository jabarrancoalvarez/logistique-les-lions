using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LogistiqueLesLions.API.Hubs;

/// <summary>
/// Hub SignalR para notificaciones in-app. Los clientes se suscriben a:
///   connection.on("notification", payload => ...)
/// El servidor empuja con SignalRNotificationService.NotifyAsync(...).
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    // No expone métodos al cliente — solo recibe pushes del servidor.
}
