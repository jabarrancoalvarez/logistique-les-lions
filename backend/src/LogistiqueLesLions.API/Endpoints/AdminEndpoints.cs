using LogistiqueLesLions.Application.Features.Admin.Commands.ApproveVehicle;
using LogistiqueLesLions.Application.Features.Admin.Queries.GetAdminStats;
using LogistiqueLesLions.Application.Features.Admin.Queries.GetVehiclesAdmin;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization("AdminOnly");

        // GET /api/v1/admin/stats
        group.MapGet("/stats", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminStatsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithSummary("Estadísticas generales de la plataforma");

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
        group.MapPost("/vehicles/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveVehicleCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result);
        })
        .WithSummary("Aprobar un vehículo (pasar a estado Active)");

        return group;
    }
}
