using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFilterOptions;

public class GetFilterOptionsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFilterOptionsQuery, Result<FilterOptionsDto>>
{
    public async Task<Result<FilterOptionsDto>> Handle(
        GetFilterOptionsQuery request, CancellationToken ct)
    {
        var equipments = await context.VehicleEquipments
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.DisplayOrder)
            .Select(e => new EquipmentOptionDto(e.Id, e.Code, e.Name))
            .ToListAsync(ct);

        // Solo los colores que existen en anuncios visibles: ofrecer una lista fija
        // llevaría al usuario a filtrar por colores sin ningún resultado.
        var colors = await context.Vehicles
            .AsNoTracking()
            .Where(v => (v.Status == VehicleStatus.Actif || v.Status == VehicleStatus.Reserve)
                        && v.Color != null && v.Color != "")
            .Select(v => v.Color!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

        return Result<FilterOptionsDto>.Success(new FilterOptionsDto(equipments, colors));
    }
}
