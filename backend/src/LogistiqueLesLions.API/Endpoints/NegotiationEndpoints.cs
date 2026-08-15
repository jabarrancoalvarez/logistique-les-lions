using LogistiqueLesLions.API.Documents;
using LogistiqueLesLions.Application.Features.Negotiations;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using System.Security.Claims;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>Mes négociations.</summary>
public static class NegotiationEndpoints
{
    public static RouteGroupBuilder MapNegotiationEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        // ─── GET /api/v1/negotiations ────────────────────────────────────────
        group.MapGet("/", async (
            [FromQuery] NegotiationStatus? status,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyNegotiationsQuery(userId, status), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetMyNegotiations")
        .WithSummary("Negociaciones del usuario: En cours · En attente · Terminées");

        // ─── GET /api/v1/negotiations/{id} ───────────────────────────────────
        group.MapGet("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetNegotiationQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "Negotiation.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetNegotiation")
        .WithSummary("Detalle de una negociación con su cronología");

        // ─── GET /api/v1/negotiations/{id}/inspection ────────────────────────
        // Checklist privada: solo la del propio usuario, nunca la de la otra parte.
        group.MapGet("/{id:guid}/inspection", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyInspectionQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "Negotiation.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetMyInspection")
        .WithSummary("Checklist privada de inspección del usuario");

        // ─── PUT /api/v1/negotiations/{id}/inspection ────────────────────────
        group.MapPut("/{id:guid}/inspection", async (
            Guid id,
            [FromBody] SaveInspectionBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var command = new SaveInspectionCommand(
                userId, id, body.VisitedAt, body.ObservedMileage, body.Notes,
                body.Items ?? []);

            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("SaveInspection")
        .WithSummary("Guardar la checklist privada de inspección");

        // ─── POST /api/v1/negotiations/offers ────────────────────────────────
        // «Faire une offre» desde la ficha del vehículo.
        group.MapPost("/offers", async (
            [FromBody] MakeOfferBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new MakeOfferCommand(userId, body.VehicleId, body.Amount, body.Message), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("MakeOffer")
        .WithSummary("Enviar una oferta sobre un anuncio");

        // ─── POST /api/v1/negotiations/{id}/offers ───────────────────────────
        group.MapPost("/{id:guid}/offers", async (
            Guid id,
            [FromBody] CounterOfferBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new CounterOfferCommand(userId, id, body.Amount, body.Message), ct);

            return result.IsSuccess
                ? Results.Ok(new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CounterOffer")
        .WithSummary("Contraoferta dentro de una negociación");

        // ─── POST /api/v1/negotiations/offers/{id}/accept ────────────────────
        group.MapPost("/offers/{id:guid}/accept", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new AcceptOfferCommand(userId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("AcceptOffer")
        .WithSummary("Accepter une offre");

        // ─── POST /api/v1/negotiations/offers/{id}/reject ────────────────────
        group.MapPost("/offers/{id:guid}/reject", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new RejectOfferCommand(userId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RejectOffer")
        .WithSummary("Refuser une offre");

        // ─── GET /api/v1/negotiations/{id}/contract ──────────────────────────
        // Devuelve el contrato si existe y, si no, los datos precargados del formulario.
        group.MapGet("/{id:guid}/contract", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetContractQuery(userId, id), ct);

            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error == "Negotiation.AccessDenied"
                ? Results.Forbid()
                : Results.NotFound(result.Error);
        })
        .WithName("GetContract")
        .WithSummary("Pestaña «Contrat» de una negociación");

        // ─── POST /api/v1/negotiations/{id}/contract ─────────────────────────
        group.MapPost("/{id:guid}/contract", async (
            Guid id,
            [FromBody] ContractBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var command = new CreateContractCommand(
                userId, id, body.AgreedPrice, body.RegistrationPlate,
                body.SellerLegalName, body.SellerIdDocument, body.SellerAddress,
                body.BuyerLegalName, body.BuyerIdDocument, body.BuyerAddress);

            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateContract")
        .WithSummary("Créer le contrat de vente");

        // ─── PUT /api/v1/negotiations/contracts/{id} ─────────────────────────
        group.MapPut("/contracts/{id:guid}", async (
            Guid id,
            [FromBody] ContractBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var command = new UpdateContractCommand(
                userId, id, body.AgreedPrice, body.RegistrationPlate,
                body.SellerLegalName, body.SellerIdDocument, body.SellerAddress,
                body.BuyerLegalName, body.BuyerIdDocument, body.BuyerAddress);

            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateContract")
        .WithSummary("Corregir un contrato en borrador");

        // ─── POST /api/v1/negotiations/contracts/{id}/send ───────────────────
        group.MapPost("/contracts/{id:guid}/send", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new SendContractCommand(userId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("SendContract")
        .WithSummary("Enviar el contrato a la otra parte");

        // ─── POST /api/v1/negotiations/contracts/{id}/validate ───────────────
        group.MapPost("/contracts/{id:guid}/validate", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new ValidateContractCommand(userId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("ValidateContract")
        .WithSummary("Valider le contrat — vente vérifiée");

        // ─── POST /api/v1/negotiations/contracts/{id}/request-changes ────────
        group.MapPost("/contracts/{id:guid}/request-changes", async (
            Guid id,
            [FromBody] RequestContractChangesBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new RequestContractChangesCommand(userId, id, body.Notes ?? string.Empty), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RequestContractChanges")
        .WithSummary("Demander une modification du contrat");

        // ─── POST /api/v1/negotiations/contracts/{id}/cancel ─────────────────
        group.MapPost("/contracts/{id:guid}/cancel", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new CancelContractCommand(userId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("CancelContract")
        .WithSummary("Anular un contrato no validado");

        // ─── GET /api/v1/negotiations/contracts/{id}.pdf ─────────────────────
        // El contrato definitivo, con el QR de verificación en el pie.
        group.MapGet("/contracts/{id:guid}.pdf", async (
            Guid id,
            ClaimsPrincipal user,
            IMediator mediator,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetContractDocumentQuery(userId, id), ct);

            if (!result.IsSuccess)
                return result.Error == "Negotiation.AccessDenied"
                    ? Results.Forbid()
                    : Results.NotFound(result.Error);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var contract = result.Value!;
            var frontend = (configuration["Frontend:Url"] ?? string.Empty).TrimEnd('/');
            var verificationUrl = $"{frontend}/verification/{contract.VerificationCode}";

            var pdf = new ContractDocument(contract, verificationUrl).GeneratePdf();

            return Results.File(pdf, "application/pdf",
                fileDownloadName: $"contrat-{contract.PublicReference}.pdf");
        })
        .WithName("DownloadContractPdf")
        .WithSummary("Descargar el contrato de venta en PDF");

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}

/// <param name="Amount">Opcional: sin importe, el formulario solo inicia la conversación.</param>
public record MakeOfferBody(Guid VehicleId, decimal? Amount, string? Message);

public record CounterOfferBody(decimal Amount, string? Message);

/// <summary>Crear y corregir comparten formulario, y por tanto cuerpo.</summary>
public record ContractBody(
    decimal AgreedPrice,
    string? RegistrationPlate,
    string SellerLegalName,
    string? SellerIdDocument,
    string? SellerAddress,
    string BuyerLegalName,
    string? BuyerIdDocument,
    string? BuyerAddress);

public record RequestContractChangesBody(string? Notes);

public record SaveInspectionBody(
    DateTimeOffset? VisitedAt,
    int? ObservedMileage,
    string? Notes,
    IReadOnlyList<InspectionItemDto>? Items);
