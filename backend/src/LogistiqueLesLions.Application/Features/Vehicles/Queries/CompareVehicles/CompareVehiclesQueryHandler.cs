using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.CompareVehicles;

public class CompareVehiclesQueryHandler(
    IApplicationDbContext context,
    IPriceIndicatorService priceIndicator)
    : IRequestHandler<CompareVehiclesQuery, Result<List<VehicleComparisonDto>>>
{
    /// <summary>Tope de la especificación. Configurable desde el backoffice (P34).</summary>
    private const int MaxVehicles = 3;

    public async Task<Result<List<VehicleComparisonDto>>> Handle(
        CompareVehiclesQuery request, CancellationToken ct)
    {
        var ids = request.VehicleIds.Distinct().Take(MaxVehicles).ToList();
        if (ids.Count == 0)
            return Result<List<VehicleComparisonDto>>.Success([]);

        var items = await context.Vehicles
            .AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            // Un vehículo vendido sigue apareciendo: el usuario conserva la referencia
            // de lo que estaba comparando. Solo se excluye lo que nunca fue público.
            .Where(v => v.Status != VehicleStatus.Brouillon && v.Status != VehicleStatus.Archive)
            .Select(v => new VehicleComparisonDto(
                v.Id,
                v.PublicReference,
                v.Slug,
                v.Make.Name,
                v.Model != null ? v.Model.Name : null,
                v.Version,
                v.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl ?? i.Url).FirstOrDefault(),
                v.Price,
                null,   // el indicador de precio se calcula después, en bloque
                v.City,
                v.Status,
                v.Year,
                v.Mileage,
                v.FuelType,
                v.Transmission,
                v.BodyType,
                v.PowerCv,
                v.EngineDisplacementCc,
                v.Drivetrain,
                v.Doors,
                v.Seats,
                v.Color,
                v.CustomsStatus,
                v.Equipments.Select(e => e.Equipment.Code).ToList(),
                v.SellerId))
            .ToListAsync(ct);

        var indicators = await priceIndicator.CalculateManyAsync(
            items.Select(i => i.Id).ToList(), ct);

        var withIndicator = items
            .Select(i => indicators.TryGetValue(i.Id, out var r) && r.Indicator is not null
                ? i with { PriceIndicator = r.Indicator }
                : i)
            // Se respeta el orden en que el usuario los seleccionó.
            .OrderBy(i => ids.IndexOf(i.Id))
            .ToList();

        return Result<List<VehicleComparisonDto>>.Success(withIndicator);
    }
}
