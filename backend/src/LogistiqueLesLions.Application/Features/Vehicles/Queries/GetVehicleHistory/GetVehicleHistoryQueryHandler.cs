using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleHistory;

public class GetVehicleHistoryQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehicleHistoryQuery, Result<IEnumerable<VehicleHistoryDto>>>
{
    public async Task<Result<IEnumerable<VehicleHistoryDto>>> Handle(
        GetVehicleHistoryQuery request, CancellationToken cancellationToken)
    {
        var exists = await context.Vehicles
            .AnyAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (!exists)
            return Result<IEnumerable<VehicleHistoryDto>>.Failure("Vehículo no encontrado.");

        var history = await context.VehicleHistories
            .AsNoTracking()
            .Where(h => h.VehicleId == request.VehicleId)
            .OrderByDescending(h => h.EventDate)
            .Select(h => new VehicleHistoryDto(
                h.Id, h.EventType, h.Description, h.EventDate,
                h.MileageAtEvent, h.Source, h.IsVerified))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<VehicleHistoryDto>>.Success(history);
    }
}
