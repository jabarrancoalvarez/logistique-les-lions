using LogistiqueLesLions.Application.Features.PublicTracking.Queries.GetTrackingByCode;
using LogistiqueLesLions.Application.Features.PublicTracking.Queries.GetTrackingRoute;
using MediatR;

namespace LogistiqueLesLions.API.Endpoints;

public static class PublicTrackingEndpoints
{
    public static RouteGroupBuilder MapPublicTrackingEndpoints(this RouteGroupBuilder group)
    {
        // Endpoint público — sin autenticación. Cubierto por el rate limiter
        // general por IP definido en Program.cs.
        group.AllowAnonymous();

        // GET /api/v1/public/tracking/{code}
        group.MapGet("/{code}", async (string code, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTrackingByCodeQuery(code), ct);
            return result.IsSuccess ? Results.Ok(result) : Results.NotFound(result);
        })
        .WithSummary("Consultar el estado público de un proceso por su código de tracking");

        // GET /api/v1/public/tracking/{code}/route
        group.MapGet("/{code}/route", async (string code, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTrackingRouteQuery(code), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithSummary("Coordenadas geográficas del recorrido para mapa Leaflet");

        return group;
    }
}
