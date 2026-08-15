using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.AskVehicleQuestion;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.GenerateVehicleDescription;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.DeleteVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.ToggleFavorite;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.RegisterVehicleView;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.SetFavoriteAlert;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.UploadVehicleImage;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetSimilarVehicles;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.CompareVehicles;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.CountVehicles;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFilterOptions;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetMyFavorites;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleModels;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleFacets;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleHistory;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace LogistiqueLesLions.API.Endpoints;

public static class VehicleCrudEndpoints
{
    public static RouteGroupBuilder MapVehicleCrudEndpoints(this RouteGroupBuilder group)
    {
        // ─── GET /api/v1/vehicles ─────────────────────────────────────────────
        group.MapGet("/", async (
            [AsParameters] VehicleSearchRequest req,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(req.ToQuery(CanSeeNonPublic(user, req.SellerId)), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetVehicles")
        .WithSummary("Listado paginado de vehículos con filtros")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/count ──────────────────────────────────────
        // Cuántos resultados producen unos filtros, sin traer los anuncios.
        group.MapGet("/count", async (
            [AsParameters] VehicleSearchRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new CountVehiclesQuery(req.ToQuery()), ct);
            return result.IsSuccess
                ? Results.Ok(new { count = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CountVehicles")
        .WithSummary("Número de resultados para unos filtros dados")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/compare ────────────────────────────────────
        // Los datos se consultan siempre en vivo: el comparador solo guarda los ids.
        group.MapGet("/compare", async (
            [FromQuery] Guid[] ids, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CompareVehiclesQuery(ids ?? []), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("CompareVehicles")
        .WithSummary("Datos actuales de los vehículos del comparador")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/filter-options ─────────────────────────────
        group.MapGet("/filter-options", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFilterOptionsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetFilterOptions")
        .WithSummary("Catálogo de equipamiento y colores disponibles para los filtros")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/makes/{makeId}/models ──────────────────────
        group.MapGet("/makes/{makeId:guid}/models", async (
            Guid makeId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetVehicleModelsQuery(makeId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetVehicleModels")
        .WithSummary("Modelos de una marca, para el desplegable dependiente")
        .AllowAnonymous();

        // ─── GET /api/v1/vehicles/{id}/similar ───────────────────────────────
        group.MapGet("/{id:guid}/similar", async (
            Guid id, IMediator mediator, CancellationToken ct, [FromQuery] int take = 8) =>
        {
            var result = await mediator.Send(new GetSimilarVehiclesQuery(id, take), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetSimilarVehicles")
        .WithSummary("Véhicules similaires del final de la ficha")
        .AllowAnonymous();

        // ─── POST /api/v1/vehicles/{id}/view ─────────────────────────────────
        group.MapPost("/{id:guid}/view", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new RegisterVehicleViewCommand(id), ct);
            // El contador es informativo: nunca debe hacer fallar la carga de la ficha.
            return Results.NoContent();
        })
        .WithName("RegisterVehicleView")
        .WithSummary("Registra una visualización del anuncio")
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
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // El vendedor sale siempre del token: nunca se acepta del cuerpo, o cualquiera
            // podría publicar anuncios en nombre de otro usuario.
            if (!TryGetUserId(user, out var sellerId))
                return Results.Unauthorized();

            var result = await mediator.Send(command with { SellerId = sellerId }, ct);
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
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (id != command.Id)
                return Results.BadRequest("L'identifiant de l'URL ne correspond pas au corps de la requête.");

            if (!TryGetUserId(user, out var requesterId))
                return Results.Unauthorized();

            // El handler comprueba que quien edita sea el propietario del anuncio.
            var result = await mediator.Send(command with { RequesterId = requesterId }, ct);

            if (result.IsSuccess) return Results.NoContent();
            return result.Error == "Vehicle.NotOwner"
                ? Results.Forbid()
                : Results.BadRequest(result.Error);
        })
        .WithName("UpdateVehicle")
        .WithSummary("Actualizar datos de un vehículo")
        .RequireAuthorization();

        // ─── DELETE /api/v1/vehicles/{id} ─────────────────────────────────────
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value;
            if (!Guid.TryParse(sub, out var requesterId))
                return Results.Unauthorized();

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
        // El usuario sale siempre del token: aceptarlo por query string permitiría
        // manipular los favoritos de cualquier otra cuenta.
        group.MapPost("/{id:guid}/favorite", async (
            Guid id,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new ToggleFavoriteCommand(userId, id), ct);
            return result.IsSuccess
                ? Results.Ok(new { isSaved = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("ToggleFavorite")
        .WithSummary("Añadir/quitar vehículo de Favoris")
        .RequireAuthorization();

        // ─── GET /api/v1/vehicles/favorites ──────────────────────────────────
        group.MapGet("/favorites", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyFavoritesQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetMyFavorites")
        .WithSummary("Mes recherches → Favoris")
        .RequireAuthorization();

        // ─── PUT /api/v1/vehicles/{id}/favorite/alert ────────────────────────
        group.MapPut("/{id:guid}/favorite/alert", async (
            Guid id,
            [FromBody] SetAlertRequest body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new SetFavoriteAlertCommand(userId, id, body.Enabled), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("SetFavoriteAlert")
        .WithSummary("Alerta de bajada de precio de un favorito concreto")
        .RequireAuthorization();

        // ─── PUT /api/v1/vehicles/favorites/alerts ───────────────────────────
        group.MapPut("/favorites/alerts", async (
            [FromBody] SetAlertRequest body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new SetAllFavoriteAlertsCommand(userId, body.Enabled), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("SetAllFavoriteAlerts")
        .WithSummary("Interruptor general de alertas de Favoris")
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

    /// <summary>Identificador del usuario autenticado a partir del JWT.</summary>
    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }

    /// <summary>
    /// Los borradores, pausados y archivados solo los ve su propietario o un
    /// administrador. Nunca se deduce del <c>sellerId</c> recibido, o bastaría con
    /// pasar el identificador de otro usuario para ver sus anuncios sin publicar.
    /// </summary>
    private static bool CanSeeNonPublic(ClaimsPrincipal user, Guid? requestedSellerId)
    {
        if (user.IsInRole("Admin")) return true;
        if (requestedSellerId is null) return false;
        return TryGetUserId(user, out var userId) && userId == requestedSellerId.Value;
    }
}

/// <summary>Cuerpo de los dos endpoints de alertas de Favoris.</summary>
public record SetAlertRequest(bool Enabled);
