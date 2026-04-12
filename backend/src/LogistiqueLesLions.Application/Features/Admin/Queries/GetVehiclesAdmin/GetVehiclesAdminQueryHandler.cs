using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetVehiclesAdmin;

public class GetVehiclesAdminQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVehiclesAdminQuery, Result<PagedResult<VehicleAdminDto>>>
{
    public async Task<Result<PagedResult<VehicleAdminDto>>> Handle(GetVehiclesAdminQuery request, CancellationToken ct)
    {
        var query = db.Vehicles
            .IgnoreQueryFilters()
            .Include(v => v.Make)
            .Include(v => v.Seller)
            .AsNoTracking();

        if (request.Status.HasValue)
            query = query.Where(v => v.Status == request.Status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VehicleAdminDto(
                v.Id, v.Title, v.Slug, v.Status.ToString(),
                v.Price, v.Currency,
                v.Seller != null ? v.Seller.Email : "—",
                v.Make.Name, v.Year, v.CreatedAt, v.ExpiresAt))
            .ToListAsync(ct);

        return Result<PagedResult<VehicleAdminDto>>.Success(
            new PagedResult<VehicleAdminDto>(items, total, request.Page, request.PageSize));
    }
}
