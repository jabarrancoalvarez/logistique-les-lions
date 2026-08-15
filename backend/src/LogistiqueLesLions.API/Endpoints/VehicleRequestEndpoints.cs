using LogistiqueLesLions.Application.Features.VehicleRequests;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>Mes recherches → Mes demandes «Trouvez-moi une voiture».</summary>
public static class VehicleRequestEndpoints
{
    public static RouteGroupBuilder MapVehicleRequestEndpoints(this RouteGroupBuilder group)
    {
        // Gratuita, pero exige cuenta. El usuario sale siempre del token.
        group.RequireAuthorization();

        // ─── GET /api/v1/vehicle-requests ────────────────────────────────────
        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyVehicleRequestsQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetMyVehicleRequests")
        .WithSummary("Solicitudes del usuario, con su estado y propuestas");

        // ─── GET /api/v1/vehicle-requests/{id} ───────────────────────────────
        group.MapGet("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetVehicleRequestQuery(userId, id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetVehicleRequest")
        .WithSummary("Detalle de una solicitud, con su hilo y sus propuestas");

        // ─── POST /api/v1/vehicle-requests ───────────────────────────────────
        group.MapPost("/", async (
            [FromBody] CreateVehicleRequestBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(body.ToCommand(userId), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicle-requests/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateVehicleRequest")
        .WithSummary("Enviar una solicitud «Trouvez-moi une voiture»");

        // ─── POST /api/v1/vehicle-requests/{id}/messages ─────────────────────
        group.MapPost("/{id:guid}/messages", async (
            Guid id,
            [FromBody] VehicleRequestMessageBody body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(
                new AddVehicleRequestMessageCommand(userId, id, body.Body), ct);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("AddVehicleRequestMessage")
        .WithSummary("Responder dentro de la solicitud");

        // ─── POST /api/v1/vehicle-requests/{id}/proposals/seen ───────────────
        group.MapPost("/{id:guid}/proposals/seen", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            await mediator.Send(new MarkProposalsSeenCommand(userId, id), ct);
            return Results.NoContent();
        })
        .WithName("MarkVehicleRequestProposalsSeen")
        .WithSummary("Marcar como vistas las propuestas de la solicitud");

        // ─── POST /api/v1/vehicle-requests/{id}/cancel ───────────────────────
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new CancelVehicleRequestCommand(userId, id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("CancelVehicleRequest")
        .WithSummary("Annuler ma demande");

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}

/// <summary>Cuerpo del formulario «Trouvez-moi une voiture».</summary>
public record CreateVehicleRequestBody(
    Guid? MakeId,
    string MakeName,
    string? ModelName,
    string? Version,
    int? YearFrom,
    int? YearTo,
    int? MaxMileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    string? Color,
    string? ImportantEquipment,
    decimal? MaxBudget,
    VehicleRequestOrigin Origin,
    string? Notes)
{
    public CreateVehicleRequestCommand ToCommand(Guid userId) => new(
        userId, MakeId, MakeName, ModelName, Version,
        YearFrom, YearTo, MaxMileage,
        FuelType, Transmission, BodyType, Color, ImportantEquipment,
        MaxBudget, Origin, Notes);
}

public record VehicleRequestMessageBody(string Body);
