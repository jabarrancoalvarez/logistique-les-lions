using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;

public class SendMessageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SendMessageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken ct)
    {
        // Check vehicle exists
        var vehicle = await db.Vehicles.FindAsync([request.VehicleId], ct);
        if (vehicle is null) return Result<Guid>.Failure("Vehicle.NotFound");

        // Determine buyer/seller
        var isSenderSeller = vehicle.SellerId == request.SenderId;
        var buyerId  = isSenderSeller ? request.RecipientId : request.SenderId;
        var sellerId = isSenderSeller ? request.SenderId : request.RecipientId;

        // Find or create conversation
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c =>
                c.BuyerId == buyerId &&
                c.SellerId == sellerId &&
                c.VehicleId == request.VehicleId, ct);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                BuyerId   = buyerId,
                SellerId  = sellerId,
                VehicleId = request.VehicleId
            };
            db.Conversations.Add(conversation);
        }

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId       = request.SenderId,
            Body           = request.Body.Trim()
        };
        db.Messages.Add(message);

        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(message.Id);
    }
}
