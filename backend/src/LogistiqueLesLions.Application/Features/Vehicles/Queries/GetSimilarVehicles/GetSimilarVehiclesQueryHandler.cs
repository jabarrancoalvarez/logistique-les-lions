using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetSimilarVehicles;

public class GetSimilarVehiclesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSimilarVehiclesQuery, Result<List<VehicleListDto>>>
{
    /// <summary>Horquilla de precio considerada «similar».</summary>
    private const decimal PriceBand = 0.25m;
    /// <summary>Años arriba y abajo considerados «similares».</summary>
    private const int YearBand = 3;

    public async Task<Result<List<VehicleListDto>>> Handle(
        GetSimilarVehiclesQuery request, CancellationToken ct)
    {
        var current = await context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (current is null)
            return Result<List<VehicleListDto>>.Failure("Vehicle.NotFound");

        var take = Math.Clamp(request.Take, 1, 12);
        var minPrice = current.Price * (1 - PriceBand);
        var maxPrice = current.Price * (1 + PriceBand);

        // Prioridad de la especificación. Cada criterio solo se consulta si los
        // anteriores no han llenado el cupo, de modo que los más parecidos van primero.
        var rules = new List<Func<IQueryable<Vehicle>, IQueryable<Vehicle>>>
        {
            // 1. Misma marca y modelo
            q => q.Where(v => v.MakeId == current.MakeId
                           && current.ModelId != null && v.ModelId == current.ModelId),
            // 2. Misma marca en un rango de precio similar
            q => q.Where(v => v.MakeId == current.MakeId
                           && v.Price >= minPrice && v.Price <= maxPrice),
            // 3. Precio y año similares
            q => q.Where(v => v.Price >= minPrice && v.Price <= maxPrice
                           && v.Year >= current.Year - YearBand
                           && v.Year <= current.Year + YearBand),
            // 4. Misma ubicación
            q => q.Where(v => current.Region != null && v.Region == current.Region),
            // 5. Misma carrocería
            q => q.Where(v => current.BodyType != null && v.BodyType == current.BodyType)
        };

        var collected = new List<Guid>();

        foreach (var rule in rules)
        {
            if (collected.Count >= take) break;

            var found = await rule(VisibleOthers(current.Id))
                .Where(v => !collected.Contains(v.Id))
                // Dentro de cada criterio, primero los de precio más parecido.
                .OrderBy(v => v.Price > current.Price ? v.Price - current.Price : current.Price - v.Price)
                .Take(take - collected.Count)
                .Select(v => v.Id)
                .ToListAsync(ct);

            collected.AddRange(found);
        }

        if (collected.Count == 0)
            return Result<List<VehicleListDto>>.Success([]);

        var items = await ToCards(context.Vehicles.AsNoTracking().Where(v => collected.Contains(v.Id)))
            .ToListAsync(ct);

        // Se restablece el orden de prioridad, que el IN de la consulta no preserva.
        var ordered = collected
            .Select(id => items.First(i => i.Id == id))
            .ToList();

        return Result<List<VehicleListDto>>.Success(ordered);
    }

    /// <summary>Otros anuncios visibles al público.</summary>
    private IQueryable<Vehicle> VisibleOthers(Guid excludeId) =>
        context.Vehicles
            .AsNoTracking()
            .Where(v => v.Id != excludeId
                     && (v.Status == VehicleStatus.Actif || v.Status == VehicleStatus.Reserve));

    /// <summary>Los similares usan la misma tarjeta reducida que el Marketplace.</summary>
    private static IQueryable<VehicleListDto> ToCards(IQueryable<Vehicle> query)
    {
        var now = DateTimeOffset.UtcNow;
        return query.Select(v => new VehicleListDto(
            v.Id,
            v.PublicReference,
            v.Slug,
            v.Title,
            v.Make.Name,
            v.Model != null ? v.Model.Name : null,
            v.Version,
            v.Year,
            v.Mileage,
            v.Price,
            v.Currency,
            v.Region,
            v.City,
            v.Condition,
            v.FuelType,
            v.Transmission,
            v.BodyType,
            v.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
            v.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl).FirstOrDefault(),
            v.Images
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                .Select(i => i.ThumbnailUrl ?? i.Url)
                .Take(8)
                .ToList(),
            v.Images.Count,
            v.FeaturedUntil > now ? v.FeaturedTier : FeaturedTier.Aucune,
            v.FavoritesCount,
            v.ViewsCount,
            v.CreatedAt,
            v.Status,
            v.SellerId,
            null));   // el indicador de precio se calcula después, en bloque
    }
}
