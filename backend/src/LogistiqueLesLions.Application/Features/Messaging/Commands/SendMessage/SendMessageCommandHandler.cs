using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;

public class SendMessageCommandHandler(IApplicationDbContext db)
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

        var message = new Message
        {
            NegotiationId = negotiation.Id,
            SenderId      = request.SenderId,
            Body          = request.Body.Trim()
        };
        db.Messages.Add(message);

        negotiation.LastMessageAt  = now;
        negotiation.LastActivityAt = now;

        // Un mensaje reabre la conversación: deja de estar a la espera.
        if (negotiation.Status == NegotiationStatus.EnAttente)
            negotiation.Status = NegotiationStatus.EnCours;

        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(message.Id);
    }
}
