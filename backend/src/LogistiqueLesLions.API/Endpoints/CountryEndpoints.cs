using LogistiqueLesLions.Application.Features.Countries.Queries.GetSupportedCountries;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LogistiqueLesLions.API.Endpoints;

public static class CountryEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static RouteGroupBuilder MapCountryEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/v1/countries/supported
        group.MapGet("/supported", async (
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            const string cacheKey = "supported_countries";

            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Results.Ok(JsonSerializer.Deserialize<IEnumerable<SupportedCountryDto>>(cached, JsonOpts));

            var result = await mediator.Send(new GetSupportedCountriesQuery(), ct);
            if (result.IsFailure)
                return Results.Problem(result.Error, statusCode: 400);

            await cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(result.Value, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("GetSupportedCountries")
        .WithSummary("Países soportados por la plataforma")
        .WithDescription("Lista de países donde la plataforma gestiona documentación de importación/exportación. Cacheado 1 hora.")
        .Produces<IEnumerable<SupportedCountryDto>>()
        .AllowAnonymous();

        return group;
    }
}
