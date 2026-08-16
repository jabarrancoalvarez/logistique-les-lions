using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;

public class SendMessageCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null,
    IChatPusher? chat = null)
    : IRequestHandler<SendMessageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FindAsync([request.VehicleId], ct);
        if (vehicle is null) return Result<Guid>.Failure("Vehicle.NotFound");

        // Un anuncio vendido deja de admitir nuevos contactos, según la especificación.
        if (!vehicle.AcceptsNegotiation)
            return Result<Guid>.Failure("Vehicle.NotOpenForNegotiation");

        // Quien publica siempre es el vendedor del anuncio; el otro es el interesado.
        var isSenderSeller = vehicle.SellerId == request.SenderId;
        var buyerId  = isSenderSeller ? request.RecipientId : request.SenderId;
        var sellerId = isSenderSeller ? request.SenderId : request.RecipientId;

        if (sellerId != vehicle.SellerId)
            return Result<Guid>.Failure("Negotiation.InvalidParticipants");

        var now = DateTimeOffset.UtcNow;

        var negotiation = await db.Negotiations
            .FirstOrDefaultAsync(n =>
                n.BuyerId == buyerId &&
                n.SellerId == sellerId &&
                n.VehicleId == request.VehicleId, ct);

        if (negotiation is null)
        {
            // La negociación nace del primer mensaje: es la primera muestra de interés
            // real sobre el vehículo.
            negotiation = new Negotiation
            {
                BuyerId   = buyerId,
                SellerId  = sellerId,
                VehicleId = request.VehicleId,
                Status    = NegotiationStatus.EnCours
            };
            db.Negotiations.Add(negotiation);

            db.NegotiationEvents.Add(new NegotiationEvent
            {
                NegotiationId = negotiation.Id,
                // Primer hito de la cronología.
                Sequence      = 1,
                Type          = NegotiationEventType.ConversationStarted,
                ActorId       = request.SenderId
            });
        }

        var body = request.Body.Trim();

        var message = new Message
        {
            NegotiationId = negotiation.Id,
            SenderId      = request.SenderId,
            Body          = body
        };
        db.Messages.Add(message);

        negotiation.LastMessageAt  = now;
        negotiation.LastActivityAt = now;

        // Un mensaje reabre la conversación: deja de estar a la espera.
        if (negotiation.Status == NegotiationStatus.EnAttente)
            negotiation.Status = NegotiationStatus.EnCours;

        // Quien recibe un mensaje tiene que enterarse aunque no esté mirando la pantalla.
        // Sin esta fila, la campana se quedaba a cero y solo se descubría el mensaje al
        // volver a entrar en la negociación.
        var notification = new UserNotification
        {
            UserId   = request.RecipientId,
            Category = NotificationCategories.Message,
            Title    = "Nouveau message",
            Body     = Resumir(body),
            Link     = $"/mis-negociaciones/{negotiation.Id}"
        };
        db.UserNotifications.Add(notification);

        await db.SaveChangesAsync(ct);

        // Después de guardar: si el envío en vivo falla, el mensaje y su aviso ya existen.
        await pusher.PushAsync([notification], ct);

        if (chat is not null)
        {
            await chat.PushMessageAsync(new PushedChatMessage(
                message.Id, negotiation.Id, request.SenderId, request.RecipientId,
                request.VehicleId, body, message.CreatedAt), ct);
        }

        return Result<Guid>.Success(message.Id);
    }

    /// <summary>Adelanto del mensaje para la campana, sin cortar una palabra por la mitad.</summary>
    private static string Resumir(string body)
    {
        const int maximo = 120;
        if (body.Length <= maximo) return body;

        var corte = body.LastIndexOf(' ', maximo);
        return string.Concat(body.AsSpan(0, corte > 40 ? corte : maximo).TrimEnd(), "…");
    }
}
