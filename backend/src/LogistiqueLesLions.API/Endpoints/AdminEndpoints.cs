using LogistiqueLesLions.Application.Features.Admin.Commands.ApproveVehicle;
using LogistiqueLesLions.Application.Features.Admin.Queries.GetAdminStats;
using LogistiqueLesLions.Application.Features.Admin.Queries.GetDashboardKpis;
using LogistiqueLesLions.Application.Features.Admin.Queries.GetVehiclesAdmin;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence.Seeding;
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

        // GET /api/v1/admin/dashboard/kpis
        group.MapGet("/dashboard/kpis", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDashboardKpisQuery(), ct);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithSummary("KPIs agregados para el dashboard (procesos por estado, lead time, incidencias)");

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
        // Solo Admin puede aprobar (acción de mutación crítica)
        group.MapPost("/vehicles/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveVehicleCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result);
        })
        .RequireAuthorization("AdminOnly")
        .WithSummary("Aprobar un vehículo (pasar a estado Active)");

        // POST /api/v1/admin/seed
        // Ejecuta el DatabaseSeeder bajo demanda. Idempotente: solo inserta lo que
        // aún no existe. Útil para poblar la demo sin tener que reiniciar el
        // servicio con Seed:Enabled=true.
        group.MapPost("/seed", async (DatabaseSeeder seeder, CancellationToken ct) =>
        {
            await seeder.SeedAsync(ct);
            return Results.Ok(new { ok = true, message = "Seed ejecutado" });
        })
        .RequireAuthorization("AdminOnly")
        .WithSummary("Ejecuta el seeder de datos demo (idempotente)");

        return group;
    }
}
