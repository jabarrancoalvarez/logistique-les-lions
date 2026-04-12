using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleStats;

public class GetVehicleStatsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehicleStatsQuery, Result<VehicleStatsDto>>
{
    public async Task<Result<VehicleStatsDto>> Handle(
        GetVehicleStatsQuery request,
        CancellationToken cancellationToken)
    {
        var activeVehicles = await context.Vehicles
            .CountAsync(v => v.Status == VehicleStatus.Active, cancellationToken);

        var supportedCountries = await context.Countries
            .CountAsync(c => c.IsActive, cancellationToken);

        var completedTransactions = await context.Vehicles
            .CountAsync(v => v.Status == VehicleStatus.Sold, cancellationToken);

        var totalMakes = await context.VehicleMakes
            .CountAsync(cancellationToken);

        var stats = new VehicleStatsDto(
            ActiveVehicles: activeVehicles,
            SupportedCountries: supportedCountries,
            CompletedTransactions: completedTransactions,
            RegisteredDealers: 0, // Se completará con módulo de usuarios
            TotalMakes: totalMakes
        );

        return Result<VehicleStatsDto>.Success(stats);
    }
}
