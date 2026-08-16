using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LogistiqueLesLions.API.Hubs;

/// <summary>
/// Señales efímeras del chat: quién está escribiendo y quién ha leído.
/// </summary>
/// <remarks>
/// Lo que se guarda no pasa por aquí. Enviar un mensaje es
/// <c>POST /messaging/send</c>, y es su handler quien avisa a la otra parte con
/// <see cref="IChatPusher"/>.
///
/// ⚠️ Antes el hub tenía su propio <c>SendMessage</c>, que ejecutaba el comando y
/// notificaba él mismo. Convivía con el envío por REST, que no notificaba a nadie: según
/// por dónde entrara el mensaje, el destinatario se enteraba o no. La lógica de negocio
/// vive en el handler y el aviso sale de allí; el hub solo transporta lo que no se
/// persiste.
/// </remarks>
[Authorize]
public class ChatHub(ICurrentUser currentUser) : Hub
{
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
    /// Avisar a quien escribió de que se han leído sus mensajes.
    /// Cliente llama: connection.invoke("MarkAsRead", senderId, vehicleId)
    /// </summary>
    /// <remarks>
    /// Solo emite la señal. Marcar en la base de datos ocurre al pedir los mensajes de la
    /// conversación, que es cuando se sabe de verdad que se han visto.
    /// </remarks>
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
