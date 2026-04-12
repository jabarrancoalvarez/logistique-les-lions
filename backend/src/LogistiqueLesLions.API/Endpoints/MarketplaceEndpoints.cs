using LogistiqueLesLions.Application.Features.Marketplace.Commands.CreatePartner;
using LogistiqueLesLions.Application.Features.Marketplace.Queries.GetPartners;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

public static class MarketplaceEndpoints
{
    public static RouteGroupBuilder MapMarketplaceEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/v1/marketplace/partners?type=Gestor&country=ES
        group.MapGet("/partners", async (
            [FromQuery] PartnerType? type,
            [FromQuery] string? country,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPartnersQuery(type, country), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetPartners")
        .WithSummary("Listado público de partners de servicios del marketplace")
        .AllowAnonymous();

        // POST /api/v1/marketplace/partners
        group.MapPost("/partners", async (
            CreatePartnerCommand body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(body, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/marketplace/partners/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CreatePartner")
        .WithSummary("Alta de un partner de servicios (admin)")
        .RequireAuthorization();

        return group;
    }
}
