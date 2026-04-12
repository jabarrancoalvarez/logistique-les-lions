using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Messaging.Queries.GetMessages;

public class GetMessagesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMessagesQuery, Result<PagedResult<MessageDto>>>
{
    public async Task<Result<PagedResult<MessageDto>>> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        var conversation = await db.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct);

        if (conversation is null)
            return Result<PagedResult<MessageDto>>.Failure("Conversation.NotFound");

        if (conversation.BuyerId != request.RequesterId && conversation.SellerId != request.RequesterId)
            return Result<PagedResult<MessageDto>>.Failure("Conversation.AccessDenied");

        var query = db.Messages
            .Where(m => m.ConversationId == request.ConversationId)
            .Include(m => m.Sender);

        var total = await query.CountAsync(ct);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MessageDto(
                m.Id,
                m.SenderId,
                $"{m.Sender.FirstName} {m.Sender.LastName}",
                m.Sender.AvatarUrl,
                m.Body,
                m.IsRead,
                m.CreatedAt))
            .ToListAsync(ct);

        // Mark as read
        var unread = await db.Messages
            .Where(m => m.ConversationId == request.ConversationId
                     && m.SenderId != request.RequesterId
                     && !m.IsRead)
            .ToListAsync(ct);
        foreach (var m in unread)
        {
            m.IsRead = true;
            m.ReadAt = DateTimeOffset.UtcNow;
        }
        if (unread.Count > 0) await db.SaveChangesAsync(ct);

        return Result<PagedResult<MessageDto>>.Success(
            new PagedResult<MessageDto>(messages, total, request.Page, request.PageSize));
    }
}
