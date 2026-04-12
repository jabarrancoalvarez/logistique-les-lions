using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Messaging.Queries.GetConversations;

public class GetConversationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetConversationsQuery, Result<List<ConversationSummaryDto>>>
{
    public async Task<Result<List<ConversationSummaryDto>>> Handle(GetConversationsQuery request, CancellationToken ct)
    {
        var conversations = await db.Conversations
            .AsNoTracking()
            .Where(c => c.BuyerId == request.UserId || c.SellerId == request.UserId)
            .Include(c => c.Buyer)
            .Include(c => c.Seller)
            .Include(c => c.Vehicle).ThenInclude(v => v.Images)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

        var dtos = conversations.Select(c =>
        {
            var isBuyer    = c.BuyerId == request.UserId;
            var other      = isBuyer ? c.Seller : c.Buyer;
            var lastMsg    = c.Messages.FirstOrDefault();
            var unread     = c.Messages.Count(m => !m.IsRead && m.SenderId != request.UserId);
            var thumb      = c.Vehicle.Images.FirstOrDefault(i => i.IsPrimary)?.ThumbnailUrl
                          ?? c.Vehicle.Images.FirstOrDefault()?.ThumbnailUrl;

            return new ConversationSummaryDto(
                c.Id,
                other.Id,
                $"{other.FirstName} {other.LastName}",
                other.AvatarUrl,
                c.VehicleId,
                c.Vehicle.Title,
                thumb,
                lastMsg?.Body,
                c.LastMessageAt,
                unread);
        }).ToList();

        return Result<List<ConversationSummaryDto>>.Success(dtos);
    }
}
