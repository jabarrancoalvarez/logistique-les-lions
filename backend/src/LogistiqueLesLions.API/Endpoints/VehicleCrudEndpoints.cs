using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.DeleteVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.ToggleFavorite;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.UploadVehicleImage;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleHistory;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LogistiqueLesLions.API.Endpoints;

public static class VehicleCrudEndpoints
{
    public static RouteGroupBuilder MapVehicleCrudEndpoints(this RouteGroupBuilder group)
    {
        // ─── GET /api/v1/vehicles ─────────────────────────────────────────────
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] Guid? makeId,
            [FromQuery] Guid? modelId,
            [FromQuery] int? yearFrom,
            [FromQuery] int? yearTo,
            [FromQuery] decimal? priceFrom,
            [FromQuery] decimal? priceTo,
            [FromQuery] string? countryOrigin,
            [FromQuery] VehicleCondition? condition,
            [FromQuery] FuelType? fuelType,
            [FromQuery] TransmissionType? transmission,
            [FromQuery] BodyType? bodyType,
            [FromQuery] bool? isExportReady,
            [FromQuery] bool? isFeatured,
            [FromQuery] Guid? sellerId,
            [FromQuery] VehicleStatus? status,
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDesc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await mediator.Send(new GetVehiclesQuery(
                search, makeId, modelId, yearFrom, yearTo,
                priceFrom, priceTo, countryOrigin,
                condition, fuelType, transmission, bodyType,
                isExportReady, isFeatured,
                sortBy ?? "createdAt", sortDesc, page, pageSize,
                sellerId, status
            ), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetVehicles")
        .WithSummary("Listado paginado de vehículos con filtros")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/{slug} ─────────────────────────────────────
        group.MapGet("/{slug}", async (
            string slug,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetVehicleBySlugQuery(slug), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetVehicleBySlug")
        .WithSummary("Detalle completo de un vehículo por su slug")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/{id}/history ──────────────────────────────
        group.MapGet("/{id:guid}/history", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetVehicleHistoryQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetVehicleHistory")
        .WithSummary("Historial de eventos de un vehículo")
        .AllowAnonymous();

        // ─── POST /api/v1/vehicles ────────────────────────────────────────────
        group.MapPost("/", async (
            CreateVehicleCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicles/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Errors);
        })
        .WithName("CreateVehicle")
        .WithSummary("Crear nuevo anuncio de vehículo")
        .RequireAuthorization();

        // ─── PUT /api/v1/vehicles/{id} ────────────────────────────────────────
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateVehicleCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (id != command.Id)
                return Results.BadRequest("El ID de la ruta no coincide con el cuerpo.");

            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateVehicle")
        .WithSummary("Actualizar datos de un vehículo")
        .RequireAuthorization();

        // ─── DELETE /api/v1/vehicles/{id} ─────────────────────────────────────
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromQuery] Guid requesterId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteVehicleCommand(id, requesterId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteVehicle")
        .WithSummary("Eliminar (soft-delete) un vehículo")
        .RequireAuthorization();

        // ─── POST /api/v1/vehicles/{id}/images ───────────────────────────────
        // Acepta multipart/form-data: file (IFormFile) + isPrimary (bool) + sortOrder (int) + altText (string?)
        group.MapPost("/{id:guid}/images", async (
            Guid id,
            IFormFile file,
            IMediator mediator,
            IStorageService storage,
            CancellationToken ct,
            [FromForm] bool isPrimary = false,
            [FromForm] int sortOrder = 0,
            [FromForm] string? altText = null) =>
        {
            if (file.Length == 0)
                return Results.BadRequest("El archivo está vacío.");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType))
                return Results.BadRequest("Formato no permitido. Usa JPEG, PNG o WebP.");

            if (file.Length > 10_485_760)
                return Results.BadRequest("El archivo supera el límite de 10 MB.");

            await using var stream = file.OpenReadStream();
            var (url, thumbnailUrl) = await storage.UploadAsync(
                stream, file.FileName, file.ContentType, $"vehicles/{id}", ct);

            var command = new UploadVehicleImageCommand(id, url, thumbnailUrl, isPrimary, sortOrder, altText);
            var result  = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicles/{id}/images/{result.Value}", new { id = result.Value, url, thumbnailUrl })
                : Results.BadRequest(result.Error);
        })
        .WithName("UploadVehicleImage")
        .WithSummary("Subir imagen a un vehículo (multipart/form-data)")
        .DisableAntiforgery()
        .RequireAuthorization();

        // ─── POST /api/v1/vehicles/{id}/favorite ─────────────────────────────
        group.MapPost("/{id:guid}/favorite", async (
            Guid id,
            [FromQuery] Guid userId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new ToggleFavoriteCommand(userId, id), ct);
            return result.IsSuccess
                ? Results.Ok(new { isSaved = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("ToggleFavorite")
        .WithSummary("Añadir/quitar vehículo de guardados")
        .RequireAuthorization();

        return group;
    }
}
