using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.ToggleFavorite;

public class ToggleFavoriteCommandHandler(IApplicationDbContext context)
    : IRequestHandler<ToggleFavoriteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (vehicle is null)
            return Result<bool>.Failure("Vehículo no encontrado.");

        var saved = await context.SavedVehicles
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.VehicleId == request.VehicleId,
                cancellationToken);

        bool isNowSaved;
        if (saved is not null)
        {
            context.SavedVehicles.Remove(saved);
            vehicle.FavoritesCount = Math.Max(0, vehicle.FavoritesCount - 1);
            isNowSaved = false;
        }
        else
        {
            context.SavedVehicles.Add(new SavedVehicle
            {
                UserId    = request.UserId,
                VehicleId = request.VehicleId
            });
            vehicle.FavoritesCount++;
            isNowSaved = true;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(isNowSaved);
    }
}
