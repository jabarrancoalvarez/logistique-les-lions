using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.API.Endpoints;

public static class NewsletterEndpoints
{
    public static RouteGroupBuilder MapNewsletterEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/subscribe", async (SubscribeRequest req, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
                return Results.BadRequest(new { message = "Email inválido." });

            var exists = await db.NewsletterSubscribers
                .AnyAsync(s => s.Email == req.Email.ToLowerInvariant(), ct);

            if (exists)
                return Results.Ok(new { message = "Ya estás suscrito." });

            db.NewsletterSubscribers.Add(new Domain.Entities.NewsletterSubscriber
            {
                Id = Guid.NewGuid(),
                Email = req.Email.ToLowerInvariant(),
                SubscribedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Suscripción confirmada." });
        })
        .AllowAnonymous()
        .WithSummary("Suscribirse al newsletter")
        .RequireRateLimiting("IpRateLimit");

        return group;
    }

    private record SubscribeRequest(string Email);
}
