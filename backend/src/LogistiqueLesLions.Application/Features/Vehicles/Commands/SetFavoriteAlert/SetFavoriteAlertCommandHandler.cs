using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.SetFavoriteAlert;

public class SetFavoriteAlertCommandHandler(IApplicationDbContext context)
    : IRequestHandler<SetFavoriteAlertCommand, Result>
{
    public async Task<Result> Handle(SetFavoriteAlertCommand request, CancellationToken ct)
    {
        var saved = await context.SavedVehicles
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.VehicleId == request.VehicleId, ct);

        if (saved is null)
            return Result.Failure("Favorite.NotFound");

        saved.PriceAlertEnabled = request.Enabled;
        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class SetAllFavoriteAlertsCommandHandler(IApplicationDbContext context)
    : IRequestHandler<SetAllFavoriteAlertsCommand, Result>
{
    public async Task<Result> Handle(SetAllFavoriteAlertsCommand request, CancellationToken ct)
    {
        var user = await context.UserProfiles.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
            return Result.Failure("User.NotFound");

        user.FavoriteAlertsAllEnabled = request.Enabled;

        if (request.Enabled)
        {
            // Ver la nota del comando: al reactivar el interruptor general se limpian
            // los silencios individuales.
            var favorites = await context.SavedVehicles
                .Where(s => s.UserId == request.UserId && !s.PriceAlertEnabled)
                .ToListAsync(ct);

            foreach (var favorite in favorites)
                favorite.PriceAlertEnabled = true;
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
