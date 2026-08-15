using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>
/// Checklist privada de inspección de una negociación.
/// </summary>
/// <remarks>
/// Devuelve siempre los once puntos de la especificación, valorados o no, para que el
/// frontend no tenga que conocer el catálogo.
/// </remarks>
public record GetMyInspectionQuery(Guid UserId, Guid NegotiationId)
    : IRequest<Result<InspectionDto>>;

public record InspectionDto(
    Guid? Id,
    DateTimeOffset? VisitedAt,
    int? ObservedMileage,
    string? Notes,
    IReadOnlyList<InspectionItemDto> Items,
    DateTimeOffset? UpdatedAt
);

public record InspectionItemDto(InspectionItemType Type, InspectionResult? Result, string? Notes);

/// <summary>Guarda la checklist completa. Sustituye la valoración anterior.</summary>
public record SaveInspectionCommand(
    Guid UserId,
    Guid NegotiationId,
    DateTimeOffset? VisitedAt,
    int? ObservedMileage,
    string? Notes,
    IReadOnlyList<InspectionItemDto> Items
) : IRequest<Result>;

public class GetMyInspectionQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMyInspectionQuery, Result<InspectionDto>>
{
    public async Task<Result<InspectionDto>> Handle(GetMyInspectionQuery request, CancellationToken ct)
    {
        var negotiation = await db.Negotiations
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.NegotiationId, ct);

        if (negotiation is null) return Result<InspectionDto>.Failure("Negotiation.NotFound");
        if (!negotiation.Involves(request.UserId))
            return Result<InspectionDto>.Failure("Negotiation.AccessDenied");

        // ⚠️ Se filtra por UserId además de por negociación: la checklist de la otra
        // parte no debe ser accesible bajo ningún concepto.
        var inspection = await db.VehicleInspections
            .AsNoTracking()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.NegotiationId == request.NegotiationId
                                   && i.UserId == request.UserId, ct);

        var saved = inspection?.Items.ToDictionary(x => x.Type)
                    ?? new Dictionary<InspectionItemType, VehicleInspectionItem>();

        // Siempre los once puntos, en el orden del documento.
        var items = Enum.GetValues<InspectionItemType>()
            .Select(type => saved.TryGetValue(type, out var item)
                ? new InspectionItemDto(type, item.Result, item.Notes)
                : new InspectionItemDto(type, null, null))
            .ToList();

        return Result<InspectionDto>.Success(new InspectionDto(
            inspection?.Id,
            inspection?.VisitedAt,
            inspection?.ObservedMileage,
            inspection?.Notes,
            items,
            inspection?.UpdatedAt));
    }
}

public class SaveInspectionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SaveInspectionCommand, Result>
{
    public async Task<Result> Handle(SaveInspectionCommand request, CancellationToken ct)
    {
        var negotiation = await db.Negotiations
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.NegotiationId, ct);

        if (negotiation is null) return Result.Failure("Negotiation.NotFound");
        if (!negotiation.Involves(request.UserId)) return Result.Failure("Negotiation.AccessDenied");

        var inspection = await db.VehicleInspections
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.NegotiationId == request.NegotiationId
                                   && i.UserId == request.UserId, ct);

        if (inspection is null)
        {
            inspection = new VehicleInspection
            {
                NegotiationId = request.NegotiationId,
                UserId        = request.UserId
            };
            db.VehicleInspections.Add(inspection);
        }

        inspection.VisitedAt       = request.VisitedAt;
        inspection.ObservedMileage = request.ObservedMileage;
        inspection.Notes           = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        var existing = inspection.Items.ToDictionary(x => x.Type);

        foreach (var incoming in request.Items)
        {
            if (existing.TryGetValue(incoming.Type, out var item))
            {
                item.Result = incoming.Result;
                item.Notes  = Clean(incoming.Notes);
            }
            else if (incoming.Result is not null || !string.IsNullOrWhiteSpace(incoming.Notes))
            {
                // Solo se persisten los puntos con contenido: un formulario en blanco no
                // debe generar once filas vacías.
                inspection.Items.Add(new VehicleInspectionItem
                {
                    InspectionId = inspection.Id,
                    Type         = incoming.Type,
                    Result       = incoming.Result,
                    Notes        = Clean(incoming.Notes)
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
