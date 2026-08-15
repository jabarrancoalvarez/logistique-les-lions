using LogistiqueLesLions.Application.Features.SavedSearches;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>Mes recherches → Recherches enregistrées.</summary>
public static class SavedSearchEndpoints
{
    public static RouteGroupBuilder MapSavedSearchEndpoints(this RouteGroupBuilder group)
    {
        // Todas exigen cuenta: el usuario sale del token, nunca del cuerpo.
        group.RequireAuthorization();

        // ─── GET /api/v1/saved-searches ──────────────────────────────────────
        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetMySavedSearchesQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetMySavedSearches")
        .WithSummary("Búsquedas guardadas del usuario, con su número de resultados");

        // ─── POST /api/v1/saved-searches ─────────────────────────────────────
        group.MapPost("/", async (
            [FromBody] SavedSearchRequest body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var command = new CreateSavedSearchCommand(
                userId, body.Name, body.Filters ?? new GetVehiclesQuery(), body.AlertEnabled);

            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/saved-searches/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateSavedSearch")
        .WithSummary("Guardar los filtros actuales como búsqueda");

        // ─── PUT /api/v1/saved-searches/{id} ─────────────────────────────────
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] SavedSearchRequest body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var command = new UpdateSavedSearchCommand(
                userId, id, body.Name, body.Filters ?? new GetVehiclesQuery());

            var result = await mediator.Send(command, ct);
            return Respond(result);
        })
        .WithName("UpdateSavedSearch")
        .WithSummary("Modificar una búsqueda guardada");

        // ─── PUT /api/v1/saved-searches/{id}/alert ───────────────────────────
        group.MapPut("/{id:guid}/alert", async (
            Guid id,
            [FromBody] SetAlertRequest body,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new SetSavedSearchAlertCommand(userId, id, body.Enabled), ct);
            return Respond(result);
        })
        .WithName("SetSavedSearchAlert")
        .WithSummary("Alerte nouveaux véhicules: ON/OFF");

        // ─── DELETE /api/v1/saved-searches/{id} ──────────────────────────────
        group.MapDelete("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new DeleteSavedSearchCommand(userId, id), ct);
            return Respond(result);
        })
        .WithName("DeleteSavedSearch")
        .WithSummary("Eliminar una búsqueda guardada");

        return group;
    }

    /// <summary>
    /// Una búsqueda que no existe y una ajena se responden igual: revelar la diferencia
    /// permitiría sondear los identificadores de otros usuarios.
    /// </summary>
    private static IResult Respond(Application.Common.Models.Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : result.Error == "SavedSearch.NotFound"
                ? Results.NotFound(result.Error)
                : Results.BadRequest(result.Error);

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}

/// <param name="Filters">Filtros exactos, con el mismo formato que el Marketplace.</param>
public record SavedSearchRequest(string Name, GetVehiclesQuery? Filters, bool AlertEnabled = true);
