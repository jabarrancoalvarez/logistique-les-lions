using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid? NotificationId, bool All = false)
    : IRequest<Result<int>>;

public class MarkNotificationReadCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<MarkNotificationReadCommand, Result<int>>
{
    public async Task<Result<int>> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result<int>.Failure("Auth.Required");

        var userId = currentUser.UserId.Value;
        var now = DateTimeOffset.UtcNow;
        int updated = 0;

        if (request.All)
        {
            var pending = await db.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(ct);

            foreach (var n in pending)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }
            updated = pending.Count;
        }
        else if (request.NotificationId is { } id)
        {
            var n = await db.UserNotifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (n is null)
                return Result<int>.Failure("Notification.NotFound");
            if (!n.IsRead)
            {
                n.IsRead = true;
                n.ReadAt = now;
                updated = 1;
            }
        }
        else
        {
            return Result<int>.Failure("Notification.IdRequired");
        }

        await db.SaveChangesAsync(ct);
        return Result<int>.Success(updated);
    }
}
