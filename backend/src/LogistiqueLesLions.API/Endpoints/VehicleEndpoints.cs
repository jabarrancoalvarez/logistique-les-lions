using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Garage;
using LogistiqueLesLions.Application.Features.Moderation;
using LogistiqueLesLions.Domain.Enums;
using System.Security.Claims;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFeaturedVehicles;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleMakes;
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
            // Anulable por lo mismo: sin valor, el parámetro sería obligatorio.
            [FromQuery] int? count,
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var size = Math.Clamp(count is null or 0 ? 12 : count.Value, 1, 30);
            // La franja de rotación (5 min) entra en la clave: cada ventana cachea su
            // propio barajado, así el carrusel rota con equidad sin servir datos rancios.
            var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 300;
            var cacheKey = $"featured_vehicles_{size}_{window}";

            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
            {
                var cachedResult = JsonSerializer.Deserialize<IEnumerable<FeaturedVehicleDto>>(cached, JsonOpts);
                return Results.Ok(cachedResult);
            }

            var result = await mediator.Send(new GetFeaturedVehiclesQuery(size), ct);
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

        // ❌ Retirado «/stats»: alimentaba el contador de la portada (países soportados,
        // dealers registrados) que se fue con el producto anterior.

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

        // ─── GET /api/v1/vehicles/{id}/transparency ──────────────────────────
        // «Transparence du véhicule»: solo lo que quien vende ha compartido a propósito.
        group.MapGet("/{id:guid}/transparency", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetVehicleTransparencyQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetVehicleTransparency")
        .WithSummary("Historial que quien vende ha decidido enseñar en el anuncio")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/{id}/transparency/invoices/{documentId} ────
        // La factura sigue siendo privada: solo se sirve si se compartió expresamente.
        group.MapGet("/{id:guid}/transparency/invoices/{documentId:guid}", async (
            Guid id,
            Guid documentId,
            IMediator mediator,
            IStorageService storage,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSharedInvoiceQuery(id, documentId), ct);
            if (!result.IsSuccess) return Results.NotFound(result.Error);

            var file = result.Value!;
            var stream = await storage.OpenPrivateAsync(file.StorageKey, ct);
            if (stream is null) return Results.NotFound("GarageDocument.FileMissing");

            return Results.File(stream, file.ContentType, fileDownloadName: file.FileName);
        })
        .WithName("GetSharedInvoice")
        .WithSummary("Descargar una factura compartida en el anuncio")
        .AllowAnonymous();

        // ─── POST /api/v1/vehicles/reports ───────────────────────────────────
        // «Signaler» un anuncio, a una persona o una conversación.
        group.MapPost("/reports", async (
            [FromBody] CreateReportBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value;
            if (!Guid.TryParse(sub, out var reporterId)) return Results.Unauthorized();

            var result = await mediator.Send(new CreateReportCommand(
                reporterId, body.TargetType, body.TargetId, body.Reason,
                body.Description, body.Evidence), ct);

            return result.IsSuccess
                ? Results.Ok(new { reference = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateReport")
        .WithSummary("Signaler une annonce, un utilisateur ou une négociation")
        .RequireAuthorization();

        return group;
    }
}

/// <summary>Cuerpo de un signalement. Quien reporta sale del token.</summary>
public record CreateReportBody(
    ReportTargetType TargetType,
    Guid TargetId,
    ReportReason Reason,
    string? Description,
    IReadOnlyList<string>? Evidence);
