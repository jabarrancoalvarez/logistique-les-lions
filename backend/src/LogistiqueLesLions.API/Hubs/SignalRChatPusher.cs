using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LogistiqueLesLions.API.Hubs;

/// <inheritdoc />
public class SignalRChatPusher(
    IHubContext<ChatHub> hub,
    ILogger<SignalRChatPusher> logger) : IChatPusher
{
    public async Task PushMessageAsync(PushedChatMessage message, CancellationToken ct = default)
    {
        try
        {
            // Se reparte por usuario, no por grupo: así llega a todas las sesiones que
            // tenga abiertas —móvil y ordenador a la vez— sin que ninguna se haya tenido
            // que apuntar antes a nada.
            await hub.Clients.User(message.RecipientId.ToString()).SendAsync("ReceiveMessage", new
            {
                MessageId     = message.MessageId,
                NegotiationId = message.NegotiationId,
                SenderId      = message.SenderId,
                VehicleId     = message.VehicleId,
                Body          = message.Body,
                CreatedAt     = message.CreatedAt
            }, ct);
        }
        catch (Exception ex)
        {
            // El mensaje ya está guardado: se verá al abrir la negociación aunque el
            // envío en vivo falle.
            logger.LogWarning(ex, "Push del mensaje {MessageId} falló", message.MessageId);
        }
    }
}
