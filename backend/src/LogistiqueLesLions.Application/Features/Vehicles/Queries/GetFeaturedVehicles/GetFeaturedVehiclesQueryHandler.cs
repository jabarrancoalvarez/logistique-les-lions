using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFeaturedVehicles;

/// <summary>
/// Anuncios «À la une» de la portada: el nivel de destacado más alto. Se devuelven en
/// <b>rotación equitativa</b> para que, con muchos destacados, todos pasen por delante.
/// El front muestra seis y las flechas recorren el resto.
/// </summary>
public class GetFeaturedVehiclesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFeaturedVehiclesQuery, Result<IEnumerable<FeaturedVehicleDto>>>
{
    /// <summary>Tope de candidatos que se barajan; suficiente para varias páginas de flechas.</summary>
    private const int MaxPool = 200;

    /// <summary>La rotación cambia cada 5 minutos (en segundos).</summary>
    private const int RotationWindowSeconds = 300;

    public async Task<Result<IEnumerable<FeaturedVehicleDto>>> Handle(
        GetFeaturedVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var candidates = await context.Vehicles
            .AsNoTracking()
            .Where(v => v.FeaturedTier == FeaturedTier.ALaUne
                     && v.FeaturedUntil > now
                     && v.Status == VehicleStatus.Actif
                     && v.AdminHiddenAt == null)
            // Orden estable antes de barajar, para que la semilla mande de verdad.
            .OrderBy(v => v.FeaturedAt)
            .Take(MaxPool)
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Images.Where(i => i.IsPrimary).Take(1))
            .Select(v => new FeaturedVehicleDto(
                v.Id,
                v.Slug,
                v.Title,
                v.Make.Name,
                v.Model != null ? v.Model.Name : null,
                v.Year,
                v.Mileage,
                v.Price,
                v.Currency,
                v.CountryOrigin,
                // Sin bandera: el catálogo multi-país se retiró y Yoon u Auto es
                // mono-país. El campo se conserva en el DTO para no romper el contrato.
                null,
                v.Condition,
                v.FuelType,
                v.Transmission,
                v.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                v.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl).FirstOrDefault(),
                v.FavoritesCount,
                v.ViewsCount,
                v.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        // Rotación equitativa: se baraja con una semilla que cambia cada 5 minutos, de
        // modo que a lo largo del día todos los «À la une» ocupan las primeras posiciones.
        var seed = (int)(now.ToUnixTimeSeconds() / RotationWindowSeconds);
        var rng = new Random(seed);
        var rotated = candidates
            .OrderBy(_ => rng.Next())
            .Take(request.Count)
            .ToList();

        return Result<IEnumerable<FeaturedVehicleDto>>.Success(rotated);
    }
}
