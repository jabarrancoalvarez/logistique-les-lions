using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.AskVehicleQuestion;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.GenerateVehicleDescription;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.DeleteVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.ToggleFavorite;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.UploadVehicleImage;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleFacets;
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

        // ─── GET /api/v1/vehicles/facets ─────────────────────────────────────
        group.MapGet("/facets", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] string? search = null,
            [FromQuery] int? yearFrom = null,
            [FromQuery] int? yearTo = null,
            [FromQuery] decimal? priceFrom = null,
            [FromQuery] decimal? priceTo = null,
            [FromQuery] string? countryOrigin = null,
            [FromQuery] string? fuelType = null,
            [FromQuery] string? condition = null) =>
        {
            var result = await mediator.Send(new GetVehicleFacetsQuery(
                search, yearFrom, yearTo, priceFrom, priceTo, countryOrigin, fuelType, condition), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetVehicleFacets")
        .WithSummary("Agregaciones para filtros tipo Amazon (counts por marca/combustible/estado/país)")
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

        // ─── POST /api/v1/vehicles/{id}/ai/description ───────────────────────
        group.MapPost("/{id:guid}/ai/description", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool overwrite = true) =>
        {
            var result = await mediator.Send(new GenerateVehicleDescriptionCommand(id, overwrite), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GenerateVehicleDescription")
        .WithSummary("Generar con IA descripción ES/EN del vehículo y persistirla")
        .RequireAuthorization("CanPublishVehicle");

        // ─── POST /api/v1/vehicles/ai/preview-description ────────────────────
        // Variante sin persistencia: el wizard de creación llama aquí con los datos
        // del formulario y recibe la descripción sin necesidad de tener vehicle Id.
        group.MapPost("/ai/preview-description", async (
            VehicleAiContext context,
            IAiContentService ai,
            CancellationToken ct) =>
        {
            var result = await ai.GenerateVehicleDescriptionAsync(context, ct);
            return Results.Ok(result);
        })
        .WithName("PreviewVehicleDescription")
        .WithSummary("Generar descripción IA sin persistir (para el wizard de creación)")
        .RequireAuthorization("CanPublishVehicle");

        // ─── POST /api/v1/vehicles/{id}/ai/ask ───────────────────────────────
        // Chat IA contextual: el cliente envía pregunta + historial; el handler
        // carga la ficha del vehículo y la pasa como system prompt a Claude.
        group.MapPost("/{id:guid}/ai/ask", async (
            Guid id,
            AskVehicleQuestionCommand body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = body with { VehicleId = id };
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("AskVehicleQuestion")
        .WithSummary("Chat IA contextual sobre un vehículo concreto")
        .AllowAnonymous();

        // ─── POST /api/v1/vehicles/ai/extract-document ───────────────────────
        // multipart/form-data: file (IFormFile) — imagen de ficha técnica/COC/permiso.
        // No persiste nada: devuelve los campos extraídos para autocompletar el formulario.
        group.MapPost("/ai/extract-document", async (
            IFormFile file,
            IAiContentService ai,
            CancellationToken ct) =>
        {
            if (file.Length == 0)
                return Results.BadRequest("El archivo está vacío.");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType))
                return Results.BadRequest("Formato no permitido. Usa JPEG, PNG o WebP.");

            if (file.Length > 10_485_760)
                return Results.BadRequest("El archivo supera el límite de 10 MB.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var extraction = await ai.ExtractVehicleDocumentAsync(ms.ToArray(), file.ContentType, ct);
            return Results.Ok(extraction);
        })
        .WithName("ExtractVehicleDocument")
        .WithSummary("OCR/extracción IA de campos de un documento vehicular")
        .DisableAntiforgery()
        .RequireAuthorization("CanPublishVehicle");

        return group;
    }
}
