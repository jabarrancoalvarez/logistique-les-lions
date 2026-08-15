using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Garage;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>Mon Garage — espacio privado del usuario para los vehículos que posee.</summary>
public static class GarageEndpoints
{
    public static RouteGroupBuilder MapGarageEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        // ─── GET /api/v1/garage ──────────────────────────────────────────────
        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyGarageQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetMyGarage")
        .WithSummary("Mon Garage: resumen y tarjetas de los vehículos del usuario");

        // ─── GET /api/v1/garage/{id} ─────────────────────────────────────────
        group.MapGet("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetGarageVehicleQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "GarageVehicle.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetGarageVehicle")
        .WithSummary("Ficha de un vehículo de Mon Garage");

        // ─── POST /api/v1/garage ─────────────────────────────────────────────
        group.MapPost("/", async (
            [FromBody] GarageVehicleBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new CreateGarageVehicleCommand(userId, body.ToInput(), body.SourceContractId), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/garage/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateGarageVehicle")
        .WithSummary("+ Ajouter un véhicule");

        // ─── PUT /api/v1/garage/{id} ─────────────────────────────────────────
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] GarageVehicleBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new UpdateGarageVehicleCommand(userId, id, body.ToInput()), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateGarageVehicle")
        .WithSummary("Mettre à jour la fiche du véhicule");

        // ─── DELETE /api/v1/garage/{id} ──────────────────────────────────────
        group.MapDelete("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new DeleteGarageVehicleCommand(userId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteGarageVehicle")
        .WithSummary("Retirar un vehículo de Mon Garage");

        // ─── GET /api/v1/garage/from-contract/{contractId} ───────────────────
        // «Ajouter ce véhicule à Mon Garage»: la ficha ya rellena tras la venta.
        group.MapGet("/from-contract/{contractId:guid}", async (
            Guid contractId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new GetGaragePrefillFromContractQuery(userId, contractId), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "Contract.NotBuyer"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetGaragePrefillFromContract")
        .WithSummary("Datos precargados para incorporar un vehículo comprado en Yoon u Auto");

        // ─── POST /api/v1/garage/{id}/images ─────────────────────────────────
        group.MapPost("/{id:guid}/images", async (
            Guid id,
            IFormFile file,
            ClaimsPrincipal user,
            IMediator mediator,
            IStorageService storage,
            CancellationToken ct,
            [FromForm] bool isPrimary = false,
            [FromForm] int sortOrder = 0) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            if (file.Length == 0) return Results.BadRequest("El archivo está vacío.");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType))
                return Results.BadRequest("Formato no permitido. Usa JPEG, PNG o WebP.");

            if (file.Length > 10_485_760)
                return Results.BadRequest("El archivo supera el límite de 10 MB.");

            await using var stream = file.OpenReadStream();
            var (url, thumbnailUrl) = await storage.UploadAsync(
                stream, file.FileName, file.ContentType, $"garage/{id}", ct);

            var result = await mediator.Send(
                new AddGarageVehicleImageCommand(userId, id, url, thumbnailUrl, isPrimary, sortOrder), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/garage/{id}/images/{result.Value}",
                    new { id = result.Value, url, thumbnailUrl })
                : Results.BadRequest(result.Error);
        })
        .WithName("UploadGarageVehicleImage")
        .WithSummary("Subir una fotografía del vehículo (multipart/form-data)")
        .DisableAntiforgery();

        // ─── DELETE /api/v1/garage/images/{imageId} ──────────────────────────
        group.MapDelete("/images/{imageId:guid}", async (
            Guid imageId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new DeleteGarageVehicleImageCommand(userId, imageId), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteGarageVehicleImage")
        .WithSummary("Borrar una fotografía del vehículo");

        // ─── GET /api/v1/garage/{id}/documents ───────────────────────────────
        group.MapGet("/{id:guid}/documents", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetGarageDocumentsQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "GarageVehicle.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetGarageDocuments")
        .WithSummary("Historial documental de un vehículo, en orden cronológico");

        // ─── POST /api/v1/garage/{id}/documents ──────────────────────────────
        // El archivo va al almacenamiento privado: la documentación no se sirve estática.
        group.MapPost("/{id:guid}/documents", async (
            Guid id,
            IFormFile file,
            ClaimsPrincipal user,
            IMediator mediator,
            IStorageService storage,
            CancellationToken ct,
            [FromForm] GarageDocumentType type = GarageDocumentType.Autre,
            [FromForm] string? name = null,
            [FromForm] DateTimeOffset? documentDate = null,
            [FromForm] string? notes = null) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            if (file.Length == 0) return Results.BadRequest("El archivo está vacío.");

            var allowed = new[] { "application/pdf", "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType))
                return Results.BadRequest("Formato no permitido. Usa PDF, JPEG, PNG o WebP.");

            if (file.Length > 10_485_760)
                return Results.BadRequest("El archivo supera el límite de 10 MB.");

            await using var stream = file.OpenReadStream();
            var key = await storage.UploadPrivateAsync(
                stream, file.FileName, file.ContentType, $"garage/{id}/documents", ct);

            var command = new AddGarageDocumentCommand(
                userId, id, type,
                string.IsNullOrWhiteSpace(name) ? file.FileName : name,
                documentDate, key, file.FileName, file.ContentType, file.Length, notes);

            var result = await mediator.Send(command, ct);

            // Si el alta no cuaja, el archivo no debe quedarse suelto en el disco.
            if (!result.IsSuccess)
            {
                await storage.DeletePrivateAsync(key, ct);
                return Results.BadRequest(result.Error);
            }

            return Results.Created($"/api/v1/garage/documents/{result.Value}",
                new { id = result.Value });
        })
        .WithName("UploadGarageDocument")
        .WithSummary("Subir un documento del vehículo (multipart/form-data)")
        .DisableAntiforgery();

        // ─── PUT /api/v1/garage/documents/{documentId} ───────────────────────
        group.MapPut("/documents/{documentId:guid}", async (
            Guid documentId,
            [FromBody] GarageDocumentBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new UpdateGarageDocumentCommand(
                userId, documentId, body.Type, body.Name ?? string.Empty,
                body.DocumentDate, body.Notes), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateGarageDocument")
        .WithSummary("Corregir la clasificación de un documento");

        // ─── DELETE /api/v1/garage/documents/{documentId} ────────────────────
        group.MapDelete("/documents/{documentId:guid}", async (
            Guid documentId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new DeleteGarageDocumentCommand(userId, documentId), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteGarageDocument")
        .WithSummary("Borrar un documento del vehículo");

        // ─── GET /api/v1/garage/documents/{documentId}/file ──────────────────
        // Única vía de acceso al archivo: comprueba de quién es antes de servirlo.
        group.MapGet("/documents/{documentId:guid}/file", async (
            Guid documentId,
            ClaimsPrincipal user,
            IMediator mediator,
            IStorageService storage,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetGarageDocumentFileQuery(userId, documentId), ct);

            if (!result.IsSuccess)
                return result.Error == "GarageVehicle.AccessDenied"
                    ? Results.Forbid()
                    : Results.NotFound(result.Error);

            var file = result.Value!;
            var stream = await storage.OpenPrivateAsync(file.StorageKey, ct);
            if (stream is null) return Results.NotFound("GarageDocument.FileMissing");

            return Results.File(stream, file.ContentType, fileDownloadName: file.FileName);
        })
        .WithName("DownloadGarageDocument")
        .WithSummary("Descargar el archivo de un documento");

        // ─── GET /api/v1/garage/{id}/maintenance ─────────────────────────────
        group.MapGet("/{id:guid}/maintenance", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMaintenanceHistoryQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "GarageVehicle.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetMaintenanceHistory")
        .WithSummary("Entretien: historial de mantenimiento agrupado por año");

        // ─── POST /api/v1/garage/{id}/maintenance ────────────────────────────
        group.MapPost("/{id:guid}/maintenance", async (
            Guid id,
            [FromBody] MaintenanceBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new AddMaintenanceRecordCommand(userId, id, body.ToInput()), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/garage/maintenance/{result.Value}",
                    new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("AddMaintenanceRecord")
        .WithSummary("Registrar una intervención");

        // ─── PUT /api/v1/garage/maintenance/{recordId} ───────────────────────
        group.MapPut("/maintenance/{recordId:guid}", async (
            Guid recordId,
            [FromBody] MaintenanceBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new UpdateMaintenanceRecordCommand(userId, recordId, body.ToInput()), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateMaintenanceRecord")
        .WithSummary("Corregir una intervención");

        // ─── DELETE /api/v1/garage/maintenance/{recordId} ────────────────────
        group.MapDelete("/maintenance/{recordId:guid}", async (
            Guid recordId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new DeleteMaintenanceRecordCommand(userId, recordId), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteMaintenanceRecord")
        .WithSummary("Borrar una intervención");

        // ─── POST /api/v1/garage/maintenance/{recordId}/images ───────────────
        // Estas fotos no se hacen públicas nunca: van al almacenamiento privado.
        group.MapPost("/maintenance/{recordId:guid}/images", async (
            Guid recordId,
            IFormFile file,
            ClaimsPrincipal user,
            IMediator mediator,
            IStorageService storage,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            if (file.Length == 0) return Results.BadRequest("El archivo está vacío.");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType))
                return Results.BadRequest("Formato no permitido. Usa JPEG, PNG o WebP.");

            if (file.Length > 10_485_760)
                return Results.BadRequest("El archivo supera el límite de 10 MB.");

            await using var stream = file.OpenReadStream();
            var key = await storage.UploadPrivateAsync(
                stream, file.FileName, file.ContentType, $"garage/maintenance/{recordId}", ct);

            var result = await mediator.Send(new AddMaintenanceImageCommand(
                userId, recordId, key, file.FileName, file.ContentType, file.Length), ct);

            if (!result.IsSuccess)
            {
                await storage.DeletePrivateAsync(key, ct);
                return Results.BadRequest(result.Error);
            }

            return Results.Created($"/api/v1/garage/maintenance/images/{result.Value}",
                new { id = result.Value });
        })
        .WithName("UploadMaintenanceImage")
        .WithSummary("Subir una fotografía de la intervención (multipart/form-data)")
        .DisableAntiforgery();

        // ─── GET /api/v1/garage/maintenance/images/{imageId} ─────────────────
        group.MapGet("/maintenance/images/{imageId:guid}", async (
            Guid imageId,
            ClaimsPrincipal user,
            IMediator mediator,
            IStorageService storage,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMaintenanceImageFileQuery(userId, imageId), ct);

            if (!result.IsSuccess)
                return result.Error == "GarageVehicle.AccessDenied"
                    ? Results.Forbid()
                    : Results.NotFound(result.Error);

            var file = result.Value!;
            var stream = await storage.OpenPrivateAsync(file.StorageKey, ct);
            if (stream is null) return Results.NotFound("Maintenance.ImageMissing");

            return Results.File(stream, file.ContentType, fileDownloadName: file.FileName);
        })
        .WithName("GetMaintenanceImage")
        .WithSummary("Fotografía de una intervención");

        // ─── DELETE /api/v1/garage/maintenance/images/{imageId} ──────────────
        group.MapDelete("/maintenance/images/{imageId:guid}", async (
            Guid imageId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new DeleteMaintenanceImageCommand(userId, imageId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteMaintenanceImage")
        .WithSummary("Borrar una fotografía de la intervención");

        // ─── GET /api/v1/garage/{id}/valuation ───────────────────────────────
        // Estimación estadística, sin IA. Sin muestra suficiente no devuelve cifra.
        group.MapGet("/{id:guid}/valuation", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetVehicleValuationQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "GarageVehicle.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetVehicleValuation")
        .WithSummary("Valeur estimée y évolution de la valeur de un vehículo");

        // ─── POST /api/v1/garage/{id}/sell ───────────────────────────────────
        // «Vendre ce véhicule». Crea un BORRADOR: no se publica nada automáticamente.
        group.MapPost("/{id:guid}/sell", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new CreateListingFromGarageCommand(userId, id), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicles/{result.Value!.Slug}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateListingFromGarage")
        .WithSummary("Crear un borrador de anuncio a partir de un vehículo de Mon Garage");

        // ─── GET /api/v1/garage/listings/{vehicleId}/transparency ────────────
        group.MapGet("/listings/{vehicleId:guid}/transparency", async (
            Guid vehicleId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new GetTransparencySettingsQuery(userId, vehicleId), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "GarageVehicle.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetTransparencySettings")
        .WithSummary("Qué parte del historial se comparte en el anuncio");

        // ─── PUT /api/v1/garage/listings/{vehicleId}/transparency ────────────
        group.MapPut("/listings/{vehicleId:guid}/transparency", async (
            Guid vehicleId,
            [FromBody] TransparencyBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new SaveTransparencySettingsCommand(
                userId, vehicleId,
                body.ShowMaintenanceHistory, body.ShowMaintenanceDetails,
                body.ShowMileageEvolution, body.Records ?? []), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("SaveTransparencySettings")
        .WithSummary("Elegir expresamente qué historial se enseña en el anuncio");

        // ─── GET /api/v1/garage/{id}/completeness ────────────────────────────
        // ⚠️ Indicador del historial digital. NO es un diagnóstico mecánico.
        group.MapGet("/{id:guid}/completeness", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetVehicleCompletenessQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "GarageVehicle.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetVehicleCompleteness")
        .WithSummary("Complétude du dossier: lo completo y actualizado que está el historial");

        // ─── GET /api/v1/garage/reminders ────────────────────────────────────
        // «1 rappel à venir» del resumen: lo abierto de todos los vehículos.
        group.MapGet("/reminders", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] int limit = 5) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetUpcomingRemindersQuery(userId, limit), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetUpcomingReminders")
        .WithSummary("Rappels pendientes del usuario, del más urgente al menos");

        // ─── GET /api/v1/garage/{id}/reminders ───────────────────────────────
        group.MapGet("/{id:guid}/reminders", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetVehicleRemindersQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "GarageVehicle.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetVehicleReminders")
        .WithSummary("Rappels de un vehículo");

        // ─── POST /api/v1/garage/{id}/reminders ──────────────────────────────
        group.MapPost("/{id:guid}/reminders", async (
            Guid id,
            [FromBody] ReminderBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new AddReminderCommand(userId, id, body.ToInput()), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/garage/reminders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("AddReminder")
        .WithSummary("Crear un rappel por fecha, por kilometraje o por ambos");

        // ─── PUT /api/v1/garage/reminders/{reminderId} ───────────────────────
        group.MapPut("/reminders/{reminderId:guid}", async (
            Guid reminderId,
            [FromBody] ReminderBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new UpdateReminderCommand(userId, reminderId, body.ToInput()), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateReminder")
        .WithSummary("Corregir o aplazar un rappel");

        // ─── POST /api/v1/garage/reminders/{reminderId}/status ───────────────
        group.MapPost("/reminders/{reminderId:guid}/status", async (
            Guid reminderId,
            [FromBody] ReminderStatusBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new SetReminderStatusCommand(userId, reminderId, body.Status), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("SetReminderStatus")
        .WithSummary("Marcar un rappel como Terminé, Annulé o de nuevo À venir");

        // ─── DELETE /api/v1/garage/reminders/{reminderId} ────────────────────
        group.MapDelete("/reminders/{reminderId:guid}", async (
            Guid reminderId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new DeleteReminderCommand(userId, reminderId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteReminder")
        .WithSummary("Borrar un rappel");

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}

/// <summary>
/// Cuerpo del alta y de la corrección de la ficha.
/// </summary>
/// <remarks>
/// El usuario nunca llega por aquí: sale del token. <c>SourceContractId</c> sí se acepta,
/// pero el handler comprueba que quien lo envía es realmente quien compró.
/// </remarks>
public record GarageVehicleBody(
    Guid MakeId,
    Guid? ModelId,
    string? Version,
    int Year,
    int? Mileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    int? PowerCv,
    int? EngineDisplacementCc,
    string? Color,
    string? RegistrationPlate,
    string? Vin,
    DateTimeOffset? PurchaseDate,
    decimal? PurchasePrice,
    Guid? SourceContractId)
{
    public GarageVehicleInput ToInput() => new(
        MakeId, ModelId, Version, Year, Mileage, FuelType, Transmission, BodyType,
        PowerCv, EngineDisplacementCc, Color, RegistrationPlate, Vin,
        PurchaseDate, PurchasePrice);
}

/// <summary>
/// Corrección de la clasificación de un documento. El archivo no se sustituye: para
/// cambiarlo se sube otro y se borra el anterior.
/// </summary>
public record GarageDocumentBody(
    GarageDocumentType Type,
    string? Name,
    DateTimeOffset? DocumentDate,
    string? Notes);

/// <summary>
/// Selección de «Transparence du véhicule». Lo que no venga marcado deja de compartirse.
/// </summary>
public record TransparencyBody(
    bool ShowMaintenanceHistory,
    bool ShowMaintenanceDetails,
    bool ShowMileageEvolution,
    IReadOnlyList<SharedRecordInput>? Records);

/// <summary>Alta y corrección de un rappel comparten formulario.</summary>
public record ReminderBody(
    ReminderType Type,
    string? Label,
    DateTimeOffset? DueDate,
    int? DueMileage,
    string? Notes)
{
    public ReminderInput ToInput() => new(Type, Label ?? string.Empty, DueDate, DueMileage, Notes);
}

public record ReminderStatusBody(ReminderStatus Status);

/// <summary>Alta y corrección de una intervención comparten formulario.</summary>
public record MaintenanceBody(
    MaintenanceType Type,
    DateTimeOffset PerformedAt,
    int? Mileage,
    string? Description,
    decimal? Cost,
    string? Workshop,
    string? Notes,
    Guid? DocumentId)
{
    public MaintenanceInput ToInput() => new(
        Type, PerformedAt, Mileage, Description ?? string.Empty,
        Cost, Workshop, Notes, DocumentId);
}
