using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Configuration;

/// <summary>
/// «Configuration générale»: los parámetros que el negocio puede querer mover.
/// </summary>
/// <remarks>
/// El documento explica para qué está esta pantalla: «evitar que cualquier pequeño
/// cambio de negocio requiera modificar el código». Todo cambio aquí deja fila en
/// <c>admin_actions</c> con el valor anterior y el nuevo: sin eso, dentro de seis meses
/// nadie sabrá por qué el indicador de precio dejó de aparecer.
/// </remarks>
public record GetSettingsQuery : IRequest<Result<SettingsDto>>;

public record SettingsDto(
    PlatformSettingsDto Platform,
    PriceIndicatorSettingsDto PriceIndicator,
    ValuationSettingsDto Valuation,
    IReadOnlyList<FeatureFlagDto> Flags
);

public record PlatformSettingsDto(
    int ComparatorMaxVehicles,
    int PointsPerVerifiedSale,
    int ListingFreshnessDays,
    int MaxImagesPerListing,
    string LegalTermsVersion,
    DateTimeOffset? LegalTermsUpdatedAt
);

public record PriceIndicatorSettingsDto(
    int MinComparables,
    int MaxListingAgeDays,
    int YearBand,
    decimal GoodDealMargin,
    decimal HighPriceMargin
);

public record ValuationSettingsDto(
    int MinComparables,
    int MaxListingAgeDays,
    int YearBand,
    int MileageBandKm,
    decimal RangeSpread,
    int SnapshotIntervalDays
);

public record FeatureFlagDto(
    Guid Id, string Key, string Label, string? Description, bool IsEnabled);

public class GetSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSettingsQuery, Result<SettingsDto>>
{
    public async Task<Result<SettingsDto>> Handle(GetSettingsQuery request, CancellationToken ct)
    {
        var platform = await db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? new PlatformSettings();
        var price = await db.PriceIndicatorSettings.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? new PriceIndicatorSettings();
        var valuation = await db.VehicleValuationSettings.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? new VehicleValuationSettings();

        var flags = await db.FeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Label)
            .Select(f => new FeatureFlagDto(f.Id, f.Key, f.Label, f.Description, f.IsEnabled))
            .ToListAsync(ct);

        return Result<SettingsDto>.Success(new SettingsDto(
            new PlatformSettingsDto(
                platform.ComparatorMaxVehicles, platform.PointsPerVerifiedSale,
                platform.ListingFreshnessDays, platform.MaxImagesPerListing,
                platform.LegalTermsVersion, platform.LegalTermsUpdatedAt),
            new PriceIndicatorSettingsDto(
                price.MinComparables, price.MaxListingAgeDays, price.YearBand,
                price.GoodDealMargin, price.HighPriceMargin),
            new ValuationSettingsDto(
                valuation.MinComparables, valuation.MaxListingAgeDays, valuation.YearBand,
                valuation.MileageBandKm, valuation.RangeSpread, valuation.SnapshotIntervalDays),
            flags));
    }
}

// ─── Modificación ───────────────────────────────────────────────────────────

public record UpdateSettingsCommand(
    Guid AdminId,
    PlatformSettingsDto Platform,
    PriceIndicatorSettingsDto PriceIndicator,
    ValuationSettingsDto Valuation
) : IRequest<Result>;

public class UpdateSettingsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateSettingsCommand, Result>
{
    public async Task<Result> Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        // Los rangos no son decoración: un comparador de 40 coches o un margen del 300 %
        // dejarían la aplicación inservible sin que nadie tocara una línea de código.
        if (request.Platform.ComparatorMaxVehicles is < 2 or > 6)
            return Result.Failure("Settings.ComparatorOutOfRange");
        if (request.Platform.PointsPerVerifiedSale is < 0 or > 10_000)
            return Result.Failure("Settings.PointsOutOfRange");
        if (request.Platform.MaxImagesPerListing is < 1 or > 50)
            return Result.Failure("Settings.MaxImagesOutOfRange");
        if (request.Platform.ListingFreshnessDays is < 7 or > 365)
            return Result.Failure("Settings.FreshnessOutOfRange");
        if (string.IsNullOrWhiteSpace(request.Platform.LegalTermsVersion))
            return Result.Failure("Settings.LegalVersionRequired");

        if (request.PriceIndicator.MinComparables < 1 || request.Valuation.MinComparables < 1)
            return Result.Failure("Settings.MinComparablesOutOfRange");
        if (request.PriceIndicator.GoodDealMargin is <= 0 or >= 1
            || request.PriceIndicator.HighPriceMargin is <= 0 or >= 1)
            return Result.Failure("Settings.MarginOutOfRange");
        if (request.Valuation.RangeSpread is <= 0 or >= 1)
            return Result.Failure("Settings.SpreadOutOfRange");
        if (request.Valuation.SnapshotIntervalDays < 1)
            return Result.Failure("Settings.SnapshotIntervalOutOfRange");

        var platform = await db.PlatformSettings.FirstOrDefaultAsync(ct);
        var price = await db.PriceIndicatorSettings.FirstOrDefaultAsync(ct);
        var valuation = await db.VehicleValuationSettings.FirstOrDefaultAsync(ct);

        if (platform is null || price is null || valuation is null)
            return Result.Failure("Settings.NotFound");

        var before = Describe(platform, price, valuation);

        platform.ComparatorMaxVehicles = request.Platform.ComparatorMaxVehicles;
        platform.PointsPerVerifiedSale = request.Platform.PointsPerVerifiedSale;
        platform.ListingFreshnessDays  = request.Platform.ListingFreshnessDays;
        platform.MaxImagesPerListing   = request.Platform.MaxImagesPerListing;

        // La fecha de las condiciones solo se mueve cuando cambia la versión: es lo que
        // permite saber desde cuándo rige el texto que la gente aceptó.
        if (platform.LegalTermsVersion != request.Platform.LegalTermsVersion.Trim())
        {
            platform.LegalTermsVersion   = request.Platform.LegalTermsVersion.Trim();
            platform.LegalTermsUpdatedAt = DateTimeOffset.UtcNow;
        }

        price.MinComparables    = request.PriceIndicator.MinComparables;
        price.MaxListingAgeDays = request.PriceIndicator.MaxListingAgeDays;
        price.YearBand          = request.PriceIndicator.YearBand;
        price.GoodDealMargin    = request.PriceIndicator.GoodDealMargin;
        price.HighPriceMargin   = request.PriceIndicator.HighPriceMargin;

        valuation.MinComparables       = request.Valuation.MinComparables;
        valuation.MaxListingAgeDays    = request.Valuation.MaxListingAgeDays;
        valuation.YearBand             = request.Valuation.YearBand;
        valuation.MileageBandKm        = request.Valuation.MileageBandKm;
        valuation.RangeSpread          = request.Valuation.RangeSpread;
        valuation.SnapshotIntervalDays = request.Valuation.SnapshotIntervalDays;

        var after = Describe(platform, price, valuation);

        if (before != after)
        {
            db.AdminActions.Add(new AdminAction
            {
                AdminId    = request.AdminId,
                TargetType = AdminTargetType.Settings,
                TargetId   = PlatformSettings.SingletonId,
                Type       = AdminActionType.SettingsChanged,
                OldValue   = before,
                NewValue   = after
            });
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Retrato de la configuración, para guardarlo como valor anterior y nuevo.
    /// </summary>
    /// <remarks>
    /// Una línea legible y no un JSON: quien lea el journal quiere entender qué cambió,
    /// no deserializar nada.
    /// </remarks>
    private static string Describe(
        PlatformSettings p, PriceIndicatorSettings i, VehicleValuationSettings v) =>
        $"comparateur={p.ComparatorMaxVehicles}, points={p.PointsPerVerifiedSale}, " +
        $"fraîcheur={p.ListingFreshnessDays}j, photos={p.MaxImagesPerListing}, " +
        $"CGU={p.LegalTermsVersion} · " +
        $"indicateur(min={i.MinComparables}, âge={i.MaxListingAgeDays}j, ±{i.YearBand}a, " +
        $"bonne={i.GoodDealMargin:0.###}, élevé={i.HighPriceMargin:0.###}) · " +
        $"estimation(min={v.MinComparables}, âge={v.MaxListingAgeDays}j, ±{v.YearBand}a, " +
        $"±{v.MileageBandKm}km, fourchette={v.RangeSpread:0.###}, " +
        $"instantané={v.SnapshotIntervalDays}j)";
}

// ─── Interruptores ──────────────────────────────────────────────────────────

public record ToggleFeatureFlagCommand(Guid AdminId, Guid FlagId, bool IsEnabled)
    : IRequest<Result>;

public class ToggleFeatureFlagCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ToggleFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(ToggleFeatureFlagCommand request, CancellationToken ct)
    {
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Id == request.FlagId, ct);
        if (flag is null) return Result.Failure("FeatureFlag.NotFound");

        if (flag.IsEnabled == request.IsEnabled) return Result.Success();

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Settings,
            TargetId   = flag.Id,
            Type       = AdminActionType.FeatureFlagToggled,
            Reason     = flag.Label,
            OldValue   = flag.IsEnabled ? "activé" : "désactivé",
            NewValue   = request.IsEnabled ? "activé" : "désactivé"
        });

        flag.IsEnabled = request.IsEnabled;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Lectura pública ────────────────────────────────────────────────────────

/// <summary>
/// Los parámetros que el frontend necesita conocer, sin ser administrador.
/// </summary>
/// <remarks>
/// El comparador tiene que saber cuántos vehículos admite, y las pantallas tienen que
/// saber qué está encendido. ❌ Aquí no sale nada que no sea público: los márgenes del
/// indicador de precio y los puntos por venta se quedan en el backoffice.
/// </remarks>
public record GetPublicSettingsQuery : IRequest<Result<PublicSettingsDto>>;

public record PublicSettingsDto(
    int ComparatorMaxVehicles,
    int MaxImagesPerListing,
    string LegalTermsVersion,
    IReadOnlyDictionary<string, bool> Features
);

public class GetPublicSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPublicSettingsQuery, Result<PublicSettingsDto>>
{
    public async Task<Result<PublicSettingsDto>> Handle(
        GetPublicSettingsQuery request, CancellationToken ct)
    {
        var platform = await db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? new PlatformSettings();

        var flags = await db.FeatureFlags
            .AsNoTracking()
            .Select(f => new { f.Key, f.IsEnabled })
            .ToListAsync(ct);

        return Result<PublicSettingsDto>.Success(new PublicSettingsDto(
            platform.ComparatorMaxVehicles,
            platform.MaxImagesPerListing,
            platform.LegalTermsVersion,
            flags.ToDictionary(f => f.Key, f => f.IsEnabled)));
    }
}
