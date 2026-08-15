using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleModels;

public class GetVehicleModelsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehicleModelsQuery, Result<List<VehicleModelDto>>>
{
    public async Task<Result<List<VehicleModelDto>>> Handle(
        GetVehicleModelsQuery request, CancellationToken ct)
    {
        var models = await context.VehicleModels
            .AsNoTracking()
            .Where(m => m.MakeId == request.MakeId)
            .OrderBy(m => m.Name)
            .Select(m => new VehicleModelDto(
                m.Id,
                m.Name,
                m.Vehicles.Count(v => v.Status == VehicleStatus.Actif || v.Status == VehicleStatus.Reserve)))
            .ToListAsync(ct);

        return Result<List<VehicleModelDto>>.Success(models);
    }
}
