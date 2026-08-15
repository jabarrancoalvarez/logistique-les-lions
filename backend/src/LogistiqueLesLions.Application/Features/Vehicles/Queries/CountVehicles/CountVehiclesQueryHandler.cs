using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.CountVehicles;

public class CountVehiclesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<CountVehiclesQuery, Result<int>>
{
    public async Task<Result<int>> Handle(CountVehiclesQuery request, CancellationToken ct)
    {
        var count = await VehicleQueryFilters
            .Apply(context, request.Filters)
            .CountAsync(ct);

        return Result<int>.Success(count);
    }
}
