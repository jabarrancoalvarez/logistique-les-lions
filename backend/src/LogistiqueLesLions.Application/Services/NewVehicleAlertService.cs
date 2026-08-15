using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.SavedSearches;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Services;

/// <inheritdoc />
public class NewVehicleAlertService(
    IApplicationDbContext context,
    INotificationPusher? pusher = null) : INewVehicleAlertService
{
    public async Task<int> NotifyMatchingSearchesAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var vehicle = await context.Vehicles
            .AsNoTracking()
            .Include(v => v.Make)
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, ct);

        if (vehicle is null || !vehicle.IsPubliclyVisible) return 0;

        var searches = await context.SavedSearches
            .Where(s => s.AlertEnabled)
            // Quien publica no necesita que le avisen de su propio anuncio.
            .Where(s => s.UserId != vehicle.SellerId)
            .ToListAsync(ct);

        if (searches.Count == 0) return 0;

        var name = string.Join(' ', new[] { vehicle.Make.Name, vehicle.Model?.Name }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var created = new List<UserNotification>();

        foreach (var search in searches)
        {
            var filters = SavedSearchFilters.Deserialize(search.FiltersJson);

            // La coincidencia la decide la base de datos con los mismos criterios que
            // aplica el Marketplace. Evaluar los filtros en memoria habría duplicado la
            // lógica de búsqueda y las dos copias acabarían divergiendo.
            var matches = await VehicleQueryFilters
                .Apply(context, filters)
                .AnyAsync(v => v.Id == vehicleId, ct);

            if (!matches) continue;

            var notification = new UserNotification
            {
                UserId   = search.UserId,
                Category = NotificationCategories.NewListing,
                Title    = "Nouvelle annonce",
                Body     = $"Un nouveau véhicule correspond à votre recherche « {search.Name} » : {name}.",
                Link     = $"/vehiculos/{vehicle.Slug}"
            };

            context.UserNotifications.Add(notification);
            created.Add(notification);

            search.LastNotifiedAt = DateTimeOffset.UtcNow;
        }

        if (created.Count == 0) return 0;

        await context.SaveChangesAsync(ct);
        await pusher.PushAsync(created, ct);

        return created.Count;
    }
}
