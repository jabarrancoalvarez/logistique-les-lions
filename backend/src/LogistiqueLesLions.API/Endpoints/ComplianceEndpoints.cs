using LogistiqueLesLions.Application.Features.Compliance.Commands.InitiateProcess;
using LogistiqueLesLions.Application.Features.Compliance.Commands.UploadDocument;
using LogistiqueLesLions.Application.Features.Compliance.Queries.EstimateCost;
using LogistiqueLesLions.Application.Features.Compliance.Queries.GetDocumentTemplates;
using LogistiqueLesLions.Application.Features.Compliance.Queries.GetProcessStatus;
using LogistiqueLesLions.Application.Features.Compliance.Queries.GetRequirements;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LogistiqueLesLions.API.Endpoints;

public static class ComplianceEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static RouteGroupBuilder MapComplianceEndpoints(this RouteGroupBuilder group)
    {
        // ─── GET /api/v1/compliance/requirements?origin=ES&destination=DE ──
        group.MapGet("/requirements", async (
            [FromQuery] string origin,
            [FromQuery] string destination,
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
                return Results.BadRequest("Los parámetros 'origin' y 'destination' son obligatorios.");

            var cacheKey = $"compliance_req_{origin.ToUpper()}_{destination.ToUpper()}";
            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Results.Ok(JsonSerializer.Deserialize<CountryRequirementDto>(cached, JsonOpts));

            var result = await mediator.Send(
                new GetRequirementsQuery(origin.ToUpper(), destination.ToUpper()), ct);

            if (result.IsFailure)
                return Results.NotFound(result.Error);

            await cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(result.Value, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("GetComplianceRequirements")
        .WithSummary("Normativa de importación/exportación entre dos países")
        .AllowAnonymous();

        // ─── GET /api/v1/compliance/estimate?vehicleId=...&origin=ES&dest=DE
        group.MapGet("/estimate", async (
            [FromQuery] Guid vehicleId,
            [FromQuery] string origin,
            [FromQuery] string destination,
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            if (vehicleId == Guid.Empty || string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
                return Results.BadRequest("vehicleId, origin y destination son obligatorios.");

            var cacheKey = $"compliance_estimate_{vehicleId}_{origin.ToUpper()}_{destination.ToUpper()}";
            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Results.Ok(JsonSerializer.Deserialize<CostEstimateDto>(cached, JsonOpts));

            var result = await mediator.Send(
                new EstimateCostQuery(vehicleId, origin.ToUpper(), destination.ToUpper()), ct);

            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            await cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(result.Value, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) },
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("EstimateImportCost")
        .WithSummary("Calcula el coste estimado de importación/exportación para un vehículo")
        .AllowAnonymous();

        // ─── GET /api/v1/compliance/templates?country=ES&documentType=COC ──
        group.MapGet("/templates", async (
            [FromQuery] string country,
            [FromQuery] string? documentType,
            IMediator mediator,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(country))
                return Results.BadRequest("El parámetro 'country' es obligatorio.");

            var cacheKey = $"doc_templates_{country.ToUpper()}_{documentType ?? "all"}";
            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Results.Ok(JsonSerializer.Deserialize<IEnumerable<DocumentTemplateDto>>(cached, JsonOpts));

            var result = await mediator.Send(
                new GetDocumentTemplatesQuery(country.ToUpper(), documentType), ct);

            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            await cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(result.Value, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) },
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("GetDocumentTemplates")
        .WithSummary("Plantillas de documentos por país y tipo")
        .AllowAnonymous();

        // ─── GET /api/v1/compliance/processes/{id} ───────────────────────────
        group.MapGet("/processes/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProcessStatusQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetProcessStatus")
        .WithSummary("Estado de un proceso de tramitación")
        .RequireAuthorization();

        // ─── POST /api/v1/compliance/processes ──────────────────────────────
        group.MapPost("/processes", async (
            InitiateProcessCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/compliance/processes/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Errors);
        })
        .WithName("InitiateProcess")
        .WithSummary("Iniciar un proceso de importación/exportación con checklist de documentos")
        .RequireAuthorization();

        // ─── PUT /api/v1/compliance/processes/{processId}/documents/{docId} ─
        group.MapPut("/processes/{processId:guid}/documents/{docId:guid}", async (
            Guid processId,
            Guid docId,
            UploadDocumentCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (processId != command.ProcessId || docId != command.DocumentId)
                return Results.BadRequest("Los IDs de la ruta no coinciden con el cuerpo.");

            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("UploadProcessDocument")
        .WithSummary("Subir/actualizar un documento de un proceso de tramitación")
        .RequireAuthorization();

        return group;
    }
}
