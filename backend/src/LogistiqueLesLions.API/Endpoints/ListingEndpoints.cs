using LogistiqueLesLions.Application.Features.Listings;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>
/// «Mes annonces»: los vehículos que el usuario está vendiendo o ha vendido.
/// </summary>
public static class ListingEndpoints
{
    public static RouteGroupBuilder MapListingEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        // ─── GET /api/v1/listings ────────────────────────────────────────────
        group.MapGet("/", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] VehicleStatus? status = null) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyListingsQuery(userId, status), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetMyListings")
        .WithSummary("Mes annonces, con estadísticas y calidad de cada anuncio");

        // ─── GET /api/v1/listings/{id}/quality ───────────────────────────────
        group.MapGet("/{id:guid}/quality", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetListingQualityQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "Vehicle.NotOwner"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetListingQuality")
        .WithSummary("Qualité de l'annonce: qué le falta al anuncio");

        // ─── POST /api/v1/listings/{id}/status ───────────────────────────────
        // Publier · Pausar · Reactivar · Réservé · Vendu · Archiver.
        group.MapPost("/{id:guid}/status", async (
            Guid id,
            [FromBody] ListingStatusBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new ChangeListingStatusCommand(userId, id, body.Status), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ChangeListingStatus")
        .WithSummary("Cambiar el estado del anuncio");

        // ─── PUT /api/v1/listings/{id}/price ─────────────────────────────────
        group.MapPut("/{id:guid}/price", async (
            Guid id,
            [FromBody] ListingPriceBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new UpdateListingPriceCommand(userId, id, body.Price), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateListingPrice")
        .WithSummary("Actualizar el precio sin abrir el formulario entero");

        // ─── PUT /api/v1/listings/{id}/mileage ───────────────────────────────
        group.MapPut("/{id:guid}/mileage", async (
            Guid id,
            [FromBody] ListingMileageBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new UpdateListingMileageCommand(userId, id, body.Mileage), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateListingMileage")
        .WithSummary("Actualizar el kilometraje del anuncio");

        // ─── POST /api/v1/listings/{id}/duplicate ────────────────────────────
        group.MapPost("/{id:guid}/duplicate", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new DuplicateListingCommand(userId, id), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/listings/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("DuplicateListing")
        .WithSummary("Duplicar un anuncio como borrador nuevo");

        // ─── PUT /api/v1/listings/{id}/images/order ──────────────────────────
        group.MapPut("/{id:guid}/images/order", async (
            Guid id,
            [FromBody] ListingImageOrderBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new ReorderListingImagesCommand(userId, id, body.ImageIds ?? []), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ReorderListingImages")
        .WithSummary("Reordenar las fotografías; la primera pasa a ser la principal");

        // ─── POST /api/v1/listings/{id}/feature ──────────────────────────────
        // «Mettre en avant»: destacar el anuncio (En vedette / À la une, 15 o 30 días).
        group.MapPost("/{id:guid}/feature", async (
            Guid id,
            [FromBody] ListingFeatureBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new FeatureListingCommand(userId, id, body.Tier, body.DurationDays), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("FeatureListing")
        .WithSummary("Destacar el anuncio (gratuito durante la fase de prueba)");

        // ─── DELETE /api/v1/listings/{id}/feature ────────────────────────────
        group.MapDelete("/{id:guid}/feature", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new UnfeatureListingCommand(userId, id), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UnfeatureListing")
        .WithSummary("Retirar la mise en avant del anuncio");

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}

public record ListingStatusBody(VehicleStatus Status);
public record ListingPriceBody(decimal Price);
public record ListingMileageBody(int Mileage);

/// <summary>Nivel de destacado (En vedette / À la une) y duración en días (15 o 30).</summary>
public record ListingFeatureBody(FeaturedTier Tier, int DurationDays);

/// <summary>Orden completo de las fotografías; la primera es la principal.</summary>
public record ListingImageOrderBody(IReadOnlyList<Guid>? ImageIds);
