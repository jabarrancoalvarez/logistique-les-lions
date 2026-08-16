using LogistiqueLesLions.Application.Common.Formatting;
using System.Globalization;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Services;

/// <inheritdoc />
public class PriceDropAlertService(
    IApplicationDbContext context,
    INotificationPusher? pusher = null) : IPriceDropAlertService
{
    public async Task<int> NotifyPriceDropAsync(
        Guid vehicleId, decimal previousPrice, decimal newPrice, CancellationToken ct = default)
    {
        // Solo interesan las bajadas: una subida de precio no genera aviso.
        if (newPrice >= previousPrice) return 0;

        var vehicle = await context.Vehicles
            .AsNoTracking()
            .Include(v => v.Make)
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, ct);

        if (vehicle is null) return 0;

        // El interruptor general del usuario manda; el del favorito solo se consulta
        // cuando aquel está desactivado.
        var followers = await context.SavedVehicles
            .Where(s => s.VehicleId == vehicleId)
            .Join(context.UserProfiles,
                  s => s.UserId,
                  u => u.Id,
                  (s, u) => new { Saved = s, User = u })
            .Where(x => x.User.FavoriteAlertsAllEnabled || x.Saved.PriceAlertEnabled)
            // No repetir el aviso si ya se notificó este mismo precio.
            .Where(x => x.Saved.LastAlertedPrice == null || x.Saved.LastAlertedPrice != newPrice)
            .ToListAsync(ct);

        if (followers.Count == 0) return 0;

        var name = string.Join(' ', new[] { vehicle.Make.Name, vehicle.Model?.Name }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var title = "Baisse de prix";
        var body = $"La {name} que vous suivez est passée de {Format(previousPrice)} " +
                   $"à {Format(newPrice)} FCFA.";

        var created = new List<UserNotification>(followers.Count);

        foreach (var follower in followers)
        {
            var notification = new UserNotification
            {
                UserId   = follower.Saved.UserId,
                Category = NotificationCategories.PriceDrop,
                Title    = title,
                Body     = body,
                Link     = $"/vehiculos/{vehicle.Slug}"
            };

            context.UserNotifications.Add(notification);
            created.Add(notification);

            follower.Saved.LastAlertedPrice = newPrice;
        }

        await context.SaveChangesAsync(ct);

        // El envío en vivo va después de persistir: si falla, la notificación sigue
        // esperando en la campana.
        await pusher.PushAsync(created, ct);

        return followers.Count;
    }

    /// <summary>Formato del documento: 8.900.000</summary>
    private static string Format(decimal amount) => FcfaFormat.Amount(amount);
}
