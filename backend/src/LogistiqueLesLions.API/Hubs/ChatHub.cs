using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LogistiqueLesLions.API.Hubs;

[Authorize]
public class ChatHub(ISender sender, ICurrentUser currentUser) : Hub
{
    /// <summary>
    /// Enviar mensaje a través de SignalR.
    /// El cliente llama a: connection.invoke("SendMessage", recipientId, vehicleId, body)
    /// </summary>
    public async Task SendMessage(Guid recipientId, Guid vehicleId, string body)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new HubException("No autenticado.");
        }

        var command = new SendMessageCommand(currentUser.UserId.Value, recipientId, vehicleId, body);
        var result  = await sender.Send(command);

        if (!result.IsSuccess)
            throw new HubException(result.Error);

        // Notificar al destinatario si está conectado
        await Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", new
        {
            MessageId     = result.Value,
            SenderId      = currentUser.UserId.Value,
            VehicleId     = vehicleId,
            Body          = body,
            CreatedAt     = DateTimeOffset.UtcNow
        });

        // Confirmar al emisor
        await Clients.Caller.SendAsync("MessageSent", result.Value);
    }

    /// <summary>Unirse al grupo de la conversación para recibir mensajes de ese hilo.</summary>
    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public override async Task OnConnectedAsync()
    {
        if (currentUser.UserId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, currentUser.UserId.Value.ToString());
        await base.OnConnectedAsync();
    }
}
