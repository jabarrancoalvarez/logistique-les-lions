using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(bool UnreadOnly = false, int Take = 50)
    : IRequest<Result<NotificationListDto>>;

public record NotificationListDto(int UnreadCount, IReadOnlyList<NotificationDto> Items);

public record NotificationDto(
    Guid Id, string Category, string Title, string? Body, string? Link,
    bool IsRead, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);

public class GetMyNotificationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyNotificationsQuery, Result<NotificationListDto>>
{
    public async Task<Result<NotificationListDto>> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result<NotificationListDto>.Failure("Auth.Required");

        var userId = currentUser.UserId.Value;
        var take = Math.Clamp(request.Take, 1, 200);

        var q = db.UserNotifications.AsNoTracking().Where(n => n.UserId == userId);
        if (request.UnreadOnly) q = q.Where(n => !n.IsRead);

        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto(
                n.Id, n.Category, n.Title, n.Body, n.Link,
                n.IsRead, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);

        var unread = await db.UserNotifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        return Result<NotificationListDto>.Success(new NotificationListDto(unread, items));
    }
}
