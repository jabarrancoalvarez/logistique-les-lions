using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFeaturedVehicles;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleMakes;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleStats;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LogistiqueLesLions.API.Endpoints;

public static class VehicleEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static RouteGroupBuilder MapVehicleEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/v1/vehicles/featured
        group.MapGet("/featured", async (
            [FromQuery] int count,
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            count = Math.Clamp(count == 0 ? 6 : count, 1, 12);
            var cacheKey = $"featured_vehicles_{count}";

            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
            {
                var cachedResult = JsonSerializer.Deserialize<IEnumerable<FeaturedVehicleDto>>(cached, JsonOpts);
                return Results.Ok(cachedResult);
            }

            var result = await mediator.Send(new GetFeaturedVehiclesQuery(count), ct);
            if (result.IsFailure)
                return Results.Problem(result.Error, statusCode: 400);

            await cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(result.Value, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("GetFeaturedVehicles")
        .WithSummary("Vehículos destacados para la landing page")
        .WithDescription("Devuelve los vehículos marcados como destacados, ordenados por fecha. Cacheado 5 minutos.")
        .Produces<IEnumerable<FeaturedVehicleDto>>()
        .AllowAnonymous();

        // GET /api/v1/vehicles/stats
        group.MapGet("/stats", async (
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            const string cacheKey = "vehicle_stats";

            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Results.Ok(JsonSerializer.Deserialize<VehicleStatsDto>(cached, JsonOpts));

            var result = await mediator.Send(new GetVehicleStatsQuery(), ct);
            if (result.IsFailure)
                return Results.Problem(result.Error, statusCode: 400);

            await cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(result.Value, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("GetVehicleStats")
        .WithSummary("Estadísticas globales de la plataforma")
        .Produces<VehicleStatsDto>()
        .AllowAnonymous();

        // GET /api/v1/vehicles/makes
        group.MapGet("/makes", async (
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct,
            [FromQuery] bool onlyPopular = false) =>
        {
            var cacheKey = $"vehicle_makes_{onlyPopular}";

            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Results.Ok(JsonSerializer.Deserialize<IEnumerable<VehicleMakeDto>>(cached, JsonOpts));

            var result = await mediator.Send(new GetVehicleMakesQuery(onlyPopular), ct);
            if (result.IsFailure)
                return Results.Problem(result.Error, statusCode: 400);

            await cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(result.Value, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) },
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("GetVehicleMakes")
        .WithSummary("Lista de marcas para el buscador")
        .Produces<IEnumerable<VehicleMakeDto>>()
        .AllowAnonymous();

        return group;
    }
}
