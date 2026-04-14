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

    /// <summary>
    /// Indicar que el usuario está escribiendo. Se reenvía al destinatario.
    /// Cliente llama: connection.invoke("StartTyping", recipientId, vehicleId)
    /// </summary>
    public async Task StartTyping(Guid recipientId, Guid vehicleId)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null) return;

        await Clients.User(recipientId.ToString()).SendAsync("UserTyping", new
        {
            SenderId  = currentUser.UserId.Value,
            VehicleId = vehicleId
        });
    }

    /// <summary>
    /// Marcar mensajes como leídos. Notifica al emisor con el evento MessageRead.
    /// Cliente llama: connection.invoke("MarkAsRead", senderId, vehicleId)
    /// </summary>
    public async Task MarkAsRead(Guid senderId, Guid vehicleId)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null) return;

        await Clients.User(senderId.ToString()).SendAsync("MessageRead", new
        {
            ReaderId  = currentUser.UserId.Value,
            VehicleId = vehicleId,
            ReadAt    = DateTimeOffset.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        if (currentUser.UserId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, currentUser.UserId.Value.ToString());
        await base.OnConnectedAsync();
    }
}
