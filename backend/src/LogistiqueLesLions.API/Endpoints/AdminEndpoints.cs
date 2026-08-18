using QuestPDF.Fluent;
using LogistiqueLesLions.API.Documents;
using LogistiqueLesLions.Application.Features.Negotiations;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Commands.ApproveVehicle;
using LogistiqueLesLions.Application.Features.Admin.Dashboard;
using LogistiqueLesLions.Application.Features.Admin.Communications;
using LogistiqueLesLions.Application.Features.Admin.Statistics;
using LogistiqueLesLions.Application.Features.Admin.Configuration;
using LogistiqueLesLions.Application.Features.UpcomingFeatures;
using LogistiqueLesLions.Application.Features.Admin.Contracts;
using LogistiqueLesLions.Application.Features.Moderation;
using LogistiqueLesLions.Application.Features.Admin.Listings;
using LogistiqueLesLions.Application.Features.Admin.Negotiations;
using LogistiqueLesLions.Application.Features.Admin.Requests;
using LogistiqueLesLions.Application.Features.Admin.Users;
using System.Security.Claims;
using LogistiqueLesLions.Application.Features.Admin.Queries.GetVehiclesAdmin;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        // El panel admin lo pueden ver Admin + Moderator (policy CanViewAdminPanel).
        // Para acciones de mutación se mantienen restricciones más finas a nivel endpoint.
        group.RequireAuthorization("CanViewAdminPanel");

        // ─── GET /api/v1/admin/dashboard ─────────────────────────────────────
        // «Tableau de bord»: qué está ocurriendo hoy en Yoon u Auto.
        group.MapGet("/dashboard", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminDashboardQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetAdminDashboard")
        .WithSummary("Tableau de bord: usuarios, marketplace, actividad, demanda y Mon Garage");

        // ─── GET /api/v1/admin/users ─────────────────────────────────────────
        group.MapGet("/users", async (
            [AsParameters] AdminUserSearchRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetAdminUsers")
        .WithSummary("Listado de usuarios con búsqueda y filtros");

        // ─── GET /api/v1/admin/users/{id} ────────────────────────────────────
        group.MapGet("/users/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminUserQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetAdminUser")
        .WithSummary("Ficha administrativa: perfil, actividad, acciones y notas internas");

        // ─── POST /api/v1/admin/users/{id}/status ────────────────────────────
        // Activar, suspender temporalmente o bloquear. Siempre deja rastro.
        group.MapPost("/users/{id:guid}/status", async (
            Guid id,
            [FromBody] AccountStatusBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new ChangeAccountStatusCommand(
                adminId, id, body.Status, body.Reason, body.SuspendedUntil), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ChangeAccountStatus")
        .WithSummary("Cambiar el estado de una cuenta dejando constancia del motivo");

        // ─── POST /api/v1/admin/notes ────────────────────────────────────────
        group.MapPost("/notes", async (
            [FromBody] AdminNoteBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new AddAdminNoteCommand(
                adminId, body.TargetType, body.TargetId, body.Body ?? string.Empty), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/notes/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("AddAdminNote")
        .WithSummary("Añadir una nota interna sobre un usuario o un anuncio");

        // ─── DELETE /api/v1/admin/notes/{id} ─────────────────────────────────
        group.MapDelete("/notes/{id:guid}", async (
            Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new DeleteAdminNoteCommand(adminId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteAdminNote")
        .WithSummary("Retirar una nota interna propia");

        // ─── GET /api/v1/admin/listings ──────────────────────────────────────
        group.MapGet("/listings", async (
            [AsParameters] AdminListingSearchRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetAdminListings")
        .WithSummary("Listado de anuncios con filtros administrativos");

        // ─── GET /api/v1/admin/listings/{id} ─────────────────────────────────
        group.MapGet("/listings/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminListingQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetAdminListing")
        .WithSummary("Ficha administrativa del anuncio: métricas, precios, acciones y notas");

        // ─── POST /api/v1/admin/listings/{id}/action ─────────────────────────
        // Ocultar, reactivar, marcar para revisión, archivar o eliminar.
        // ⚠️ La información comercial no se toca desde aquí.
        group.MapPost("/listings/{id:guid}/action", async (
            Guid id,
            [FromBody] AdminListingActionBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new ApplyAdminListingActionCommand(
                adminId, id, body.Action, body.Reason), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ApplyAdminListingAction")
        .WithSummary("Aplicar una medida sobre el anuncio dejando constancia del motivo");

        // ─── POST /api/v1/admin/listings/{id}/correction ─────────────────────
        group.MapPost("/listings/{id:guid}/correction", async (
            Guid id,
            [FromBody] ListingCorrectionBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new RequestListingCorrectionCommand(
                adminId, id, body.Message ?? string.Empty), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RequestListingCorrection")
        .WithSummary("Pedir a quien publica que corrija su anuncio");

        // ─── GET /api/v1/admin/requests ──────────────────────────────────────
        group.MapGet("/requests", async (
            [AsParameters] AdminRequestSearchRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetAdminRequests")
        .WithSummary("Demandes de véhicules: solicitudes «Trouvez-moi cette voiture»");

        // ─── GET /api/v1/admin/requests/{id} ─────────────────────────────────
        group.MapGet("/requests/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminRequestQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetAdminRequest")
        .WithSummary("Ficha de la solicitud: criterios, propuestas, mensajes e historial");

        // ─── POST /api/v1/admin/requests/{id}/assign ─────────────────────────
        group.MapPost("/requests/{id:guid}/assign", async (
            Guid id,
            [FromBody] AssignRequestBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new AssignRequestCommand(adminId, id, body.Assign), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("AssignRequest")
        .WithSummary("Hacerse cargo de una solicitud o soltarla");

        // ─── POST /api/v1/admin/requests/{id}/status ─────────────────────────
        group.MapPost("/requests/{id:guid}/status", async (
            Guid id,
            [FromBody] RequestStatusBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(
                new ChangeRequestStatusCommand(adminId, id, body.Status, body.Reason), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ChangeRequestStatus")
        .WithSummary("Cambiar el estado de la solicitud");

        // ─── POST /api/v1/admin/requests/{id}/proposals/internal ─────────────
        group.MapPost("/requests/{id:guid}/proposals/internal", async (
            Guid id,
            [FromBody] InternalProposalBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(
                new AddInternalProposalCommand(adminId, id, body.VehicleId, body.Comments), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/requests/{id}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("AddInternalProposal")
        .WithSummary("Anexar un anuncio de Yoon u Auto a la solicitud");

        // ─── POST /api/v1/admin/requests/{id}/proposals/external ─────────────
        group.MapPost("/requests/{id:guid}/proposals/external", async (
            Guid id,
            [FromBody] ExternalProposalBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(
                new AddExternalProposalCommand(adminId, id, body.ToInput()), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/requests/{id}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("AddExternalProposal")
        .WithSummary("Añadir un vehículo encontrado fuera de Yoon u Auto");

        // ─── DELETE /api/v1/admin/requests/proposals/{proposalId} ────────────
        group.MapDelete("/requests/proposals/{proposalId:guid}", async (
            Guid proposalId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new RemoveProposalCommand(adminId, proposalId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RemoveProposal")
        .WithSummary("Retirar una propuesta");

        // ─── POST /api/v1/admin/requests/{id}/reply ──────────────────────────
        group.MapPost("/requests/{id:guid}/reply", async (
            Guid id,
            [FromBody] RequestReplyBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(
                new ReplyToRequestCommand(adminId, id, body.Body ?? string.Empty), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ReplyToRequest")
        .WithSummary("Responder al usuario dentro de su solicitud");

        // ─── GET /api/v1/admin/negotiations ──────────────────────────────────
        // Datos estructurales. El contenido de los mensajes NO viene aquí.
        group.MapGet("/negotiations", async (
            [AsParameters] AdminNegotiationSearchRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetAdminNegotiations")
        .WithSummary("Negociaciones: quién, sobre qué y en qué punto — sin el contenido");

        // ─── GET /api/v1/admin/negotiations/{id} ─────────────────────────────
        group.MapGet("/negotiations/{id:guid}", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminNegotiationQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetAdminNegotiation")
        .WithSummary("Ficha estructural: ofertas, cronología y accesos al contenido");

        // ─── POST /api/v1/admin/negotiations/{id}/content ────────────────────
        // ⚠️ Única vía de leer una conversación privada. Exige motivo y queda registrada.
        group.MapPost("/negotiations/{id:guid}/content", async (
            Guid id,
            [FromBody] ContentAccessBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new AccessNegotiationContentCommand(
                adminId, id, body.Reason, body.Details), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("AccessNegotiationContent")
        .WithSummary("Leer una conversación privada con motivo justificado (queda registrado)");

        // ─── GET /api/v1/admin/contracts ─────────────────────────────────────
        group.MapGet("/contracts", async (
            [AsParameters] AdminContractSearchRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetAdminContracts")
        .WithSummary("Contratos y ventas verificadas");

        // ─── GET /api/v1/admin/contracts/{id} ────────────────────────────────
        group.MapGet("/contracts/{id:guid}", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminContractQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetAdminContract")
        .WithSummary("Ficha del contrato: partes, vehículo, cronología, QR y trazabilidad");

        // ─── POST /api/v1/admin/contracts/{id}/invalidate ────────────────────
        // Lo único que el administrador puede hacerle a un contrato.
        // ❌ No existe forma de validarlo: eso pertenece a las partes.
        group.MapPost("/contracts/{id:guid}/invalidate", async (
            Guid id,
            [FromBody] InvalidateContractBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(
                new InvalidateContractCommand(adminId, id, body.Reason ?? string.Empty), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("InvalidateContract")
        .WithSummary("Invalidar administrativamente un contrato (situaciones excepcionales)");

        // ─── POST /api/v1/admin/contracts/{id}/document ──────────────────────
        // El PDF lleva pièces d'identité, direcciones y teléfonos: se entrega solo con
        // motivo escrito, y la descarga queda registrada en la misma operación, igual
        // que leer una conversación privada.
        group.MapPost("/contracts/{id:guid}/document", async (
            Guid id,
            [FromBody] ContractDocumentAccessBody body,
            ClaimsPrincipal user,
            ISender sender,
            IApplicationDbContext db,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var motivo = body.Reason?.Trim();
            if (string.IsNullOrEmpty(motivo)) return Results.BadRequest("Admin.ReasonRequired");

            var result = await sender.Send(
                new GetContractDocumentQuery(adminId, id, IsAdmin: true), ct);

            if (!result.IsSuccess) return Results.BadRequest(result.Error);

            db.AdminActions.Add(new AdminAction
            {
                AdminId    = adminId,
                TargetType = AdminTargetType.Contract,
                TargetId   = id,
                Type       = AdminActionType.ContractDocumentAccessed,
                Reason     = motivo
            });
            await db.SaveChangesAsync(ct);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var contract = result.Value!;
            var frontend = (configuration["Frontend:Url"] ?? string.Empty).TrimEnd('/');
            var pdf = new ContractDocument(
                contract, $"{frontend}/verification/{contract.VerificationCode}").GeneratePdf();

            return Results.File(pdf, "application/pdf",
                fileDownloadName: $"contrat-{contract.PublicReference}.pdf");
        })
        .RequireAuthorization("AdminOnly")
        .WithName("AdminContractDocument")
        .WithSummary("Descargar el PDF de un contrato, con motivo y dejando constancia");

        // ─── GET /api/v1/admin/reports ───────────────────────────────────────
        group.MapGet("/reports", async (
            [AsParameters] AdminReportSearchRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetReports")
        .WithSummary("Modération: bandeja de signalements");

        // ─── GET /api/v1/admin/reports/{id} ──────────────────────────────────
        group.MapGet("/reports/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetReportQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetReport")
        .WithSummary("Ficha del signalement: pruebas, historial y notas");

        // ─── POST /api/v1/admin/reports/{id}/status ──────────────────────────
        group.MapPost("/reports/{id:guid}/status", async (
            Guid id,
            [FromBody] ReportStatusBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new ChangeReportStatusCommand(
                adminId, id, body.Status, body.Resolution), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ChangeReportStatus")
        .WithSummary("Cambiar el estado del signalement; cerrarlo exige explicar la decisión");

        // ─── POST /api/v1/admin/reports/{id}/warn ────────────────────────────
        group.MapPost("/reports/{id:guid}/warn", async (
            Guid id,
            [FromBody] ReportMessageBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new WarnReportedUserCommand(
                adminId, id, body.Message ?? string.Empty), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("WarnReportedUser")
        .WithSummary("Advertir al usuario señalado");

        // ─── POST /api/v1/admin/reports/{id}/request-info ────────────────────
        group.MapPost("/reports/{id:guid}/request-info", async (
            Guid id,
            [FromBody] ReportMessageBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new RequestReportInfoCommand(
                adminId, id, body.Message ?? string.Empty), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RequestReportInfo")
        .WithSummary("Solicitar más información a quien reporta");

        // ─── GET /api/v1/admin/communications ────────────────────────────────
        group.MapGet("/communications", async (
            ISender sender,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await sender.Send(new GetCommunicationsQuery(page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetCommunications")
        .WithSummary("Histórico de comunicaciones: qué se envió, cuándo y a cuántos");

        // ─── POST /api/v1/admin/communications ───────────────────────────────
        group.MapPost("/communications", async (
            [FromBody] CommunicationBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new SendCommunicationCommand(
                adminId, body.Type, body.Audience, body.TargetUserId, body.Region,
                body.Title ?? string.Empty, body.Body ?? string.Empty, body.SendByEmail), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SendCommunication")
        .WithSummary("Enviar una comunicación de plataforma");

        // ─── Points de fidélité ──────────────────────────────────────────────
        group.MapGet("/users/{id:guid}/points", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserPointsQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetUserPoints")
        .WithSummary("Saldo y libro de movimientos de puntos de un usuario");

        group.MapPost("/users/{id:guid}/points", async (
            Guid id,
            [FromBody] PointsAdjustmentBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new AdjustUserPointsCommand(
                adminId, id, body.Points, body.Reason ?? string.Empty), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("AdjustUserPoints")
        .WithSummary("Ajustar manualmente los puntos de un usuario (exige motivo)");

        // ─── Configuration ───────────────────────────────────────────────────
        group.MapGet("/settings", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSettingsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetSettings")
        .WithSummary("Parámetros generales, del indicador de precio y de la estimación");

        group.MapPut("/settings", async (
            [FromBody] SettingsBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new UpdateSettingsCommand(
                adminId, body.Platform, body.PriceIndicator, body.Valuation), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdateSettings")
        .WithSummary("Modificar los parámetros (queda registrado con valor anterior y nuevo)");

        group.MapPost("/settings/flags/{id:guid}", async (
            Guid id,
            [FromBody] FeatureFlagBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new ToggleFeatureFlagCommand(adminId, id, body.IsEnabled), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("ToggleFeatureFlag")
        .WithSummary("Encender o apagar una funcionalidad");

        // ─── Catálogos ───────────────────────────────────────────────────────
        group.MapGet("/catalogs", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCatalogsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetCatalogs")
        .WithSummary("Marcas, modelos, equipamiento y funcionalidades futuras");

        group.MapPost("/catalogs/makes", async (
            [FromBody] CatalogMakeBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new SaveCatalogMakeCommand(
                adminId, body.Id, body.Name ?? string.Empty, body.Country, body.IsPopular), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SaveCatalogMake")
        .WithSummary("Crear o renombrar una marca");

        group.MapPost("/catalogs/models", async (
            [FromBody] CatalogModelBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new SaveCatalogModelCommand(
                adminId, body.Id, body.MakeId, body.Name ?? string.Empty, body.Category), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SaveCatalogModel")
        .WithSummary("Crear o renombrar un modelo");

        group.MapPost("/catalogs/equipments", async (
            [FromBody] CatalogEquipmentBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new SaveCatalogEquipmentCommand(
                adminId, body.Id, body.Code ?? string.Empty, body.Name ?? string.Empty,
                body.DisplayOrder, body.IsActive), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SaveCatalogEquipment")
        .WithSummary("Crear, renombrar o retirar un equipamiento");

        group.MapPost("/catalogs/features", async (
            [FromBody] CatalogFeatureBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var adminId)) return Results.Unauthorized();

            var result = await sender.Send(new SaveUpcomingFeatureCommand(
                adminId, body.Id, body.Code ?? string.Empty, body.Name ?? string.Empty,
                body.Description, body.DisplayOrder, body.IsActive), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SaveUpcomingFeature")
        .WithSummary("Crear, editar o retirar una funcionalidad de «Prochainement»");

        // ─── Intérêt pour les fonctionnalités à venir ────────────────────────
        group.MapGet("/feature-interest", async (
            ISender sender,
            CancellationToken ct,
            [FromQuery] Guid? featureId = null) =>
        {
            var result = await sender.Send(new GetFeatureInterestReportQuery(featureId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetFeatureInterestReport")
        .WithSummary("Quién quiere qué, segmentado por perfil, ciudad y actividad");

        // ─── Journal d'activité ──────────────────────────────────────────────
        group.MapGet("/activity", async (
            ISender sender,
            CancellationToken ct,
            [FromQuery] Guid? adminId = null,
            [FromQuery] AdminTargetType? targetType = null,
            [FromQuery] AdminActionType? type = null,
            [FromQuery] DateTimeOffset? from = null,
            [FromQuery] DateTimeOffset? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30) =>
        {
            var result = await sender.Send(new GetActivityLogQuery(
                adminId, targetType, type, from, to, page, pageSize), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetActivityLog")
        .WithSummary("Registro de actividad administrativa (solo lectura)");

        // ─── GET /api/v1/admin/statistics ────────────────────────────────────
        group.MapGet("/statistics", async (
            ISender sender,
            CancellationToken ct,
            [FromQuery] int days = 30) =>
        {
            var result = await sender.Send(new GetStatisticsQuery(days), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetStatistics")
        .WithSummary("Estadísticas: usuarios, oferta, demanda, desajuste y conversión");

        // ❌ Retirados «/dashboard/kpis» y «/stats»: contaban procesos de tramitación e
        // incidencias, que ya no existen. Las cifras de Yoon u Auto están en
        // «/admin/dashboard» y «/admin/statistics».

        // GET /api/v1/admin/vehicles
        group.MapGet("/vehicles", async (
            ISender sender,
            CancellationToken ct,
            [FromQuery] VehicleStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await sender.Send(new GetVehiclesAdminQuery(status, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithSummary("Listar todos los vehículos (admin)");

        // POST /api/v1/admin/vehicles/{id}/approve
        // Solo Admin puede aprobar (acción de mutación crítica)
        group.MapPost("/vehicles/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveVehicleCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result);
        })
        .RequireAuthorization("AdminOnly")
        .WithSummary("Aprobar un vehículo (pasar a estado Active)");

        // ❌ Retirado «/seed»: ejecutaba el sembrador del producto anterior, que poblaba
        // países, procesos de tramitación y partners. El catálogo senegalés lo mantiene
        // YoonUAutoReseeder al arrancar.

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}

/// <summary>Filtros del listado de usuarios.</summary>
public class AdminUserSearchRequest
{
    /// <summary>Nombre, teléfono o correo.</summary>
    [FromQuery] public string? Search { get; set; }
    [FromQuery] public string? City { get; set; }
    [FromQuery] public AccountType? AccountType { get; set; }
    [FromQuery] public bool? PhoneVerified { get; set; }
    [FromQuery] public AccountStatus? Status { get; set; }
    // Anulables a propósito: en una clase [AsParameters], un int no anulable rompe el
    // binding cuando el parámetro no viene en la query string, y el endpoint responde
    // 400 con el cuerpo vacío. El valor por defecto se aplica en ToQuery().
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }

    public GetAdminUsersQuery ToQuery() =>
        new(Search, City, AccountType, PhoneVerified, Status, Page ?? 1, PageSize ?? 20);
}

public record AccountStatusBody(
    AccountStatus Status,
    string? Reason,
    DateTimeOffset? SuspendedUntil);

public record AdminNoteBody(AdminTargetType TargetType, Guid TargetId, string? Body);

/// <summary>Filtros del listado de anuncios del backoffice.</summary>
public class AdminListingSearchRequest
{
    /// <summary>Referencia Yoon o título.</summary>
    [FromQuery] public string? Search { get; set; }
    [FromQuery] public Guid? MakeId { get; set; }
    [FromQuery] public Guid? ModelId { get; set; }
    [FromQuery] public Guid? SellerId { get; set; }
    [FromQuery] public string? City { get; set; }
    [FromQuery] public VehicleStatus? Status { get; set; }
    [FromQuery] public AccountType? SellerAccountType { get; set; }
    [FromQuery] public decimal? PriceFrom { get; set; }
    [FromQuery] public decimal? PriceTo { get; set; }
    [FromQuery] public DateTimeOffset? CreatedFrom { get; set; }
    [FromQuery] public DateTimeOffset? CreatedTo { get; set; }
    [FromQuery] public bool? Hidden { get; set; }
    [FromQuery] public bool? Flagged { get; set; }
    /// <summary>Con signalements abiertos.</summary>
    [FromQuery] public bool? Reported { get; set; }
    // Anulables a propósito: en una clase [AsParameters], un int no anulable rompe el
    // binding cuando el parámetro no viene en la query string, y el endpoint responde
    // 400 con el cuerpo vacío. El valor por defecto se aplica en ToQuery().
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }

    public GetAdminListingsQuery ToQuery() => new(
        Search, MakeId, ModelId, SellerId, City, Status, SellerAccountType,
        PriceFrom, PriceTo, CreatedFrom, CreatedTo, Hidden, Flagged, Reported, Page ?? 1, PageSize ?? 20);
}

public record AdminListingActionBody(AdminListingAction Action, string? Reason);

public record ListingCorrectionBody(string? Message);

/// <summary>Filtros del listado de solicitudes.</summary>
public class AdminRequestSearchRequest
{
    /// <summary>Referencia, marca o modelo.</summary>
    [FromQuery] public string? Search { get; set; }
    [FromQuery] public VehicleRequestStatus? Status { get; set; }
    [FromQuery] public VehicleRequestOrigin? Origin { get; set; }
    [FromQuery] public Guid? AssignedAdminId { get; set; }
    /// <summary>Solo las que nadie ha tomado: la cola de trabajo del equipo.</summary>
    [FromQuery] public bool? Unassigned { get; set; }
    // Anulables a propósito: en una clase [AsParameters], un int no anulable rompe el
    // binding cuando el parámetro no viene en la query string, y el endpoint responde
    // 400 con el cuerpo vacío. El valor por defecto se aplica en ToQuery().
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }

    public GetAdminRequestsQuery ToQuery() =>
        new(Search, Status, Origin, AssignedAdminId, Unassigned, Page ?? 1, PageSize ?? 20);
}

/// <summary>Filtros del listado de negociaciones.</summary>
public class AdminNegotiationSearchRequest
{
    [FromQuery] public string? Search { get; set; }
    [FromQuery] public NegotiationStatus? Status { get; set; }
    [FromQuery] public Guid? VehicleId { get; set; }
    [FromQuery] public Guid? UserId { get; set; }
    [FromQuery] public bool? WithContract { get; set; }
    // Anulables a propósito: en una clase [AsParameters], un int no anulable rompe el
    // binding cuando el parámetro no viene en la query string, y el endpoint responde
    // 400 con el cuerpo vacío. El valor por defecto se aplica en ToQuery().
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }

    public GetAdminNegotiationsQuery ToQuery() =>
        new(Search, Status, VehicleId, UserId, WithContract, Page ?? 1, PageSize ?? 20);
}

/// <summary>Filtros del listado de contratos.</summary>
public class AdminContractSearchRequest
{
    [FromQuery] public string? Search { get; set; }
    [FromQuery] public ContractStatus? Status { get; set; }
    [FromQuery] public Guid? UserId { get; set; }
    [FromQuery] public bool? VerifiedSalesOnly { get; set; }
    // Anulables a propósito: en una clase [AsParameters], un int no anulable rompe el
    // binding cuando el parámetro no viene en la query string, y el endpoint responde
    // 400 con el cuerpo vacío. El valor por defecto se aplica en ToQuery().
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }

    public GetAdminContractsQuery ToQuery() =>
        new(Search, Status, UserId, VerifiedSalesOnly, Page ?? 1, PageSize ?? 20);
}

/// <param name="Details">Por qué esta conversación concreta. Obligatorio.</param>
public record ContentAccessBody(ContentAccessReason Reason, string? Details);

public record InvalidateContractBody(string? Reason);

/// <summary>Filtros de la bandeja de signalements.</summary>
public class AdminReportSearchRequest
{
    [FromQuery] public string? Search { get; set; }
    [FromQuery] public ReportStatus? Status { get; set; }
    [FromQuery] public ReportReason? Reason { get; set; }
    [FromQuery] public ReportTargetType? TargetType { get; set; }
    // Anulables a propósito: en una clase [AsParameters], un int no anulable rompe el
    // binding cuando el parámetro no viene en la query string, y el endpoint responde
    // 400 con el cuerpo vacío. El valor por defecto se aplica en ToQuery().
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }

    public GetReportsQuery ToQuery() => new(Search, Status, Reason, TargetType, Page ?? 1, PageSize ?? 20);
}

public record ReportStatusBody(ReportStatus Status, string? Resolution);
public record ReportMessageBody(string? Message);

/// <summary>Comunicación de plataforma. El remitente sale del token.</summary>
public record CommunicationBody(
    CommunicationType Type,
    CommunicationAudience Audience,
    Guid? TargetUserId,
    string? Region,
    string? Title,
    string? Body,
    bool SendByEmail);

public record AssignRequestBody(bool Assign);
public record RequestStatusBody(VehicleRequestStatus Status, string? Reason);
public record InternalProposalBody(Guid VehicleId, string? Comments);
public record RequestReplyBody(string? Body);

/// <summary>Vehículo encontrado fuera de Yoon u Auto.</summary>
public record ExternalProposalBody(
    string? MakeModel,
    string? Version,
    int? Year,
    int? Mileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    decimal? EstimatedPrice,
    decimal? AdditionalCosts,
    string? CountryOfOrigin,
    IReadOnlyList<string>? PhotoUrls,
    string? ExternalUrl,
    string? Comments)
{
    public ExternalProposalInput ToInput() => new(
        MakeModel ?? string.Empty, Version, Year, Mileage, FuelType, Transmission,
        EstimatedPrice, AdditionalCosts, CountryOfOrigin, PhotoUrls, ExternalUrl, Comments);
}

// ─── Cuerpos de las peticiones de Configuration (P34) ───────────────────────

public record PointsAdjustmentBody(int Points, string? Reason);

public record SettingsBody(
    PlatformSettingsDto Platform,
    PriceIndicatorSettingsDto PriceIndicator,
    ValuationSettingsDto Valuation);

public record FeatureFlagBody(bool IsEnabled);

/// <summary><c>Id</c> nulo crea; con valor, edita.</summary>
public record CatalogMakeBody(Guid? Id, string? Name, string? Country, bool IsPopular);

public record CatalogModelBody(Guid? Id, Guid MakeId, string? Name, string? Category);

public record CatalogEquipmentBody(
    Guid? Id, string? Code, string? Name, int DisplayOrder, bool IsActive);

public record CatalogFeatureBody(
    Guid? Id, string? Code, string? Name, string? Description, int DisplayOrder, bool IsActive);

/// <summary>Por qué se descarga el PDF de un contrato desde el backoffice.</summary>
public record ContractDocumentAccessBody(string? Reason);
