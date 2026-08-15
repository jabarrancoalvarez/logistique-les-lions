using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.UpcomingFeatures;

/// <summary>
/// «Prochainement» y su botón «Ça m'intéresse».
/// </summary>
/// <remarks>
/// El documento es claro sobre dónde vive: no merece ser una sección principal del menú
/// —«no debemos llenar el menú con algo que todavía no existe»—, así que cuelga del
/// perfil.
///
/// Y sobre para qué sirve: «esto nos permite decidir qué servicio premium merece
/// realmente desarrollarse». Por eso el interés se guarda por persona y no como un
/// contador anónimo.
/// </remarks>
public record GetUpcomingFeaturesQuery(Guid? UserId) : IRequest<Result<UpcomingFeatureListDto>>;

public record UpcomingFeatureListDto(IReadOnlyList<UpcomingFeatureDto> Items);

/// <param name="IsInterested">Si quien consulta ya lo ha marcado.</param>
public record UpcomingFeatureDto(
    Guid Id, string Code, string Name, string? Description,
    int InterestedCount, bool IsInterested);

public class GetUpcomingFeaturesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUpcomingFeaturesQuery, Result<UpcomingFeatureListDto>>
{
    public async Task<Result<UpcomingFeatureListDto>> Handle(
        GetUpcomingFeaturesQuery request, CancellationToken ct)
    {
        var items = await db.UpcomingFeatures
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Name)
            .Select(f => new UpcomingFeatureDto(
                f.Id, f.Code, f.Name, f.Description,
                f.Interests.Count,
                request.UserId != null && f.Interests.Any(i => i.UserId == request.UserId)))
            .ToListAsync(ct);

        return Result<UpcomingFeatureListDto>.Success(new UpcomingFeatureListDto(items));
    }
}

// ─── Ça m'intéresse ─────────────────────────────────────────────────────────

public record SetFeatureInterestCommand(Guid UserId, Guid FeatureId, bool Interested)
    : IRequest<Result<int>>;

/// <summary>Devuelve el número de interesados tras el cambio, para refrescar la tarjeta.</summary>
public class SetFeatureInterestCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SetFeatureInterestCommand, Result<int>>
{
    public async Task<Result<int>> Handle(SetFeatureInterestCommand request, CancellationToken ct)
    {
        var feature = await db.UpcomingFeatures
            .FirstOrDefaultAsync(f => f.Id == request.FeatureId && f.IsActive, ct);

        if (feature is null) return Result<int>.Failure("Feature.NotFound");

        var existing = await db.FeatureInterests
            .FirstOrDefaultAsync(i => i.FeatureId == request.FeatureId
                                   && i.UserId == request.UserId, ct);

        if (request.Interested && existing is null)
        {
            db.FeatureInterests.Add(new FeatureInterest
            {
                FeatureId = request.FeatureId,
                UserId    = request.UserId
            });
        }
        else if (!request.Interested && existing is not null)
        {
            // Soft delete, como todo aquí: la fila queda, pero deja de contar. Lo que
            // mide esta pantalla es la demanda de hoy, no la de quien cambió de idea.
            db.FeatureInterests.Remove(existing);
        }

        await db.SaveChangesAsync(ct);

        var count = await db.FeatureInterests
            .CountAsync(i => i.FeatureId == request.FeatureId, ct);

        return Result<int>.Success(count);
    }
}

// ─── Lectura administrativa ─────────────────────────────────────────────────

/// <summary>
/// «Interés en futuras funcionalidades», con la segmentación que pide el documento.
/// </summary>
public record GetFeatureInterestReportQuery(Guid? FeatureId = null)
    : IRequest<Result<FeatureInterestReportDto>>;

public record FeatureInterestReportDto(
    IReadOnlyList<FeatureInterestRowDto> Features,
    FeatureSegmentationDto? Segmentation
);

public record FeatureInterestRowDto(
    Guid Id, string Code, string Name, bool IsActive, int InterestedCount);

/// <param name="ByActivity">
/// Reparto por actividad: quien no ha publicado nada, quien tiene un anuncio, y quien
/// tiene varios. Es lo que distingue a un curioso de alguien que trabaja con esto.
/// </param>
public record FeatureSegmentationDto(
    Guid FeatureId,
    string FeatureName,
    int Total,
    int Particuliers,
    int Professionnels,
    IReadOnlyList<SegmentCountDto> ByCity,
    IReadOnlyList<SegmentCountDto> ByActivity
);

public record SegmentCountDto(string Label, int Count);

public class GetFeatureInterestReportQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFeatureInterestReportQuery, Result<FeatureInterestReportDto>>
{
    public async Task<Result<FeatureInterestReportDto>> Handle(
        GetFeatureInterestReportQuery request, CancellationToken ct)
    {
        var features = await db.UpcomingFeatures
            .AsNoTracking()
            .OrderByDescending(f => f.Interests.Count)
            .ThenBy(f => f.Name)
            .Select(f => new FeatureInterestRowDto(
                f.Id, f.Code, f.Name, f.IsActive, f.Interests.Count))
            .ToListAsync(ct);

        // Sin funcionalidad elegida se devuelve solo el ranking: la segmentación
        // responde a «quién quiere *esto*», no a «quién quiere algo».
        if (request.FeatureId is not { } featureId)
            return Result<FeatureInterestReportDto>.Success(
                new FeatureInterestReportDto(features, null));

        var feature = features.FirstOrDefault(f => f.Id == featureId);
        if (feature is null)
            return Result<FeatureInterestReportDto>.Failure("Feature.NotFound");

        var interested = await db.FeatureInterests
            .AsNoTracking()
            .Where(i => i.FeatureId == featureId && i.User != null)
            .Select(i => new { i.UserId, i.User!.AccountType, i.User.City })
            .ToListAsync(ct);

        var userIds = interested.Select(i => i.UserId).ToList();

        var listingsPerUser = await db.Vehicles
            .AsNoTracking()
            .Where(v => userIds.Contains(v.SellerId))
            .Select(v => v.SellerId)
            .ToListAsync(ct);

        var counts = listingsPerUser
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var byActivity = interested
            .GroupBy(i => counts.GetValueOrDefault(i.UserId) switch
            {
                0 => "Aucune annonce",
                1 => "1 annonce",
                <= 5 => "2 à 5 annonces",
                _ => "Plus de 5 annonces"
            })
            .Select(g => new SegmentCountDto(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

        var byCity = interested
            .Where(i => !string.IsNullOrWhiteSpace(i.City))
            .GroupBy(i => i.City!)
            .Select(g => new SegmentCountDto(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Label)
            .ToList();

        return Result<FeatureInterestReportDto>.Success(new FeatureInterestReportDto(
            features,
            new FeatureSegmentationDto(
                featureId, feature.Name, interested.Count,
                interested.Count(i => i.AccountType == AccountType.Particulier),
                interested.Count(i => i.AccountType == AccountType.Professionnel),
                byCity, byActivity)));
    }
}
