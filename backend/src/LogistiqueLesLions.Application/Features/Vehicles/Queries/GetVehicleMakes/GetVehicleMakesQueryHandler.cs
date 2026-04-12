using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleMakes;

public class GetVehicleMakesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehicleMakesQuery, Result<IEnumerable<VehicleMakeDto>>>
{
    public async Task<Result<IEnumerable<VehicleMakeDto>>> Handle(
        GetVehicleMakesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.VehicleMakes
            .AsNoTracking()
            .AsQueryable();

        if (request.OnlyPopular)
            query = query.Where(m => m.IsPopular);

        var makes = await query
            .OrderByDescending(m => m.IsPopular)
            .ThenBy(m => m.Name)
            .Select(m => new VehicleMakeDto(
                m.Id,
                m.Name,
                m.Country,
                m.LogoUrl,
                m.IsPopular,
                m.Models.Count()
            ))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<VehicleMakeDto>>.Success(makes);
    }
}
