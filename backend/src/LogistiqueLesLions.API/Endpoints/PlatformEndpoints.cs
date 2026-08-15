using System.Security.Claims;
using LogistiqueLesLions.Application.Features.Admin.Configuration;
using LogistiqueLesLions.Application.Features.UpcomingFeatures;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>
/// Lo que la aplicación necesita saber de sí misma: parámetros públicos y
/// «Prochainement».
/// </summary>
public static class PlatformEndpoints
{
    public static RouteGroupBuilder MapPlatformEndpoints(this RouteGroupBuilder group)
    {
        // ─── GET /api/v1/platform/settings ───────────────────────────────────
        // Sin autenticar: el comparador y los límites del formulario los necesita
        // cualquiera. Aquí no sale ningún parámetro interno.
        group.MapGet("/settings", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPublicSettingsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .AllowAnonymous()
        .WithName("GetPublicSettings")
        .WithSummary("Parámetros públicos: comparador, fotos por anuncio y funcionalidades activas");

        // ─── GET /api/v1/platform/upcoming ───────────────────────────────────
        // Visible sin cuenta; el «Ça m'intéresse» ya marcado solo se conoce si la hay.
        group.MapGet("/upcoming", async (
            ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var userId = TryGetUserId(user, out var id) ? id : (Guid?)null;

            var result = await sender.Send(new GetUpcomingFeaturesQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .AllowAnonymous()
        .WithName("GetUpcomingFeatures")
        .WithSummary("Fonctionnalités à venir «Prochainement»");

        // ─── POST /api/v1/platform/upcoming/{id}/interest ────────────────────
        group.MapPost("/upcoming/{id:guid}/interest", async (
            Guid id,
            [FromBody] FeatureInterestBody body,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            // El interés se cuenta por persona: sin cuenta no hay a quién contar.
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await sender.Send(
                new SetFeatureInterestCommand(userId, id, body.Interested), ct);

            return result.IsSuccess
                ? Results.Ok(new { interestedCount = result.Value })
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithName("SetFeatureInterest")
        .WithSummary("Declarar o retirar el interés por una funcionalidad futura");

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");

        return Guid.TryParse(claim, out userId);
    }
}

public record FeatureInterestBody(bool Interested);
