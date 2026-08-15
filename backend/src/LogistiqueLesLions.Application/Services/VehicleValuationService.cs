using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Services;

/// <inheritdoc />
public class VehicleValuationService(IApplicationDbContext context) : IVehicleValuationService
{
    /// <summary>Datos mínimos de un anuncio para servir de comparable.</summary>
    private sealed record Comparable(
        Guid ModelId,
        int Year,
        int? Mileage,
        FuelType? FuelType,
        TransmissionType? Transmission,
        string? Region,
        decimal Price);

    public async Task<VehicleValuationResult> EstimateAsync(
        Guid garageVehicleId, CancellationToken ct = default)
    {
        var vehicle = await context.GarageVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == garageVehicleId, ct);

        if (vehicle is null) return VehicleValuationResult.NotEnoughData(0);

        // Sin modelo no hay nada con qué comparar: «Toyota 2019» abarca coches que no
        // tienen nada que ver entre sí.
        if (vehicle.ModelId is not { } modelId) return VehicleValuationResult.NotEnoughData(0);

        var settings = await LoadSettingsAsync(ct);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-settings.MaxListingAgeDays);

        // La ficha del garaje no guarda ubicación: el vehículo está donde vive su dueño,
        // y ahí es donde se vendería.
        var region = await context.UserProfiles
            .AsNoTracking()
            .Where(u => u.Id == vehicle.UserId)
            .Select(u => u.Region)
            .FirstOrDefaultAsync(ct);

        var pool = await context.Vehicles
            .AsNoTracking()
            .Where(v => v.MakeId == vehicle.MakeId
                     && v.ModelId == modelId
                     // Cuenta lo que el mercado pide hoy y lo que ya se ha vendido:
                     // ambas cosas dicen cuánto vale el coche.
                     && (v.Status == VehicleStatus.Actif
                         || v.Status == VehicleStatus.Reserve
                         || v.Status == VehicleStatus.Vendu)
                     && (v.PublishedAt ?? v.CreatedAt) >= cutoff
                     && v.Year >= vehicle.Year - settings.YearBand
                     && v.Year <= vehicle.Year + settings.YearBand)
            .Select(v => new Comparable(
                v.ModelId!.Value, v.Year, v.Mileage, v.FuelType, v.Transmission, v.Region, v.Price))
            .ToListAsync(ct);

        return Evaluate(vehicle, region, pool, settings);
    }

    /// <summary>
    /// Busca la muestra más parecida posible, soltando criterios hasta reunir el mínimo.
    /// </summary>
    /// <remarks>
    /// Se empieza por lo más específico —mismo uso, misma mecánica, misma región— y se va
    /// aflojando. Nunca se baja de marca, modelo y franja de años: por debajo de eso los
    /// precios dejan de ser comparables. Si ni así hay muestra suficiente, no se muestra
    /// ninguna cifra.
    /// </remarks>
    private static VehicleValuationResult Evaluate(
        GarageVehicle vehicle,
        string? region,
        IReadOnlyList<Comparable> pool,
        VehicleValuationSettings settings)
    {
        var tiers = new[]
        {
            ValuationCriteria.MakeModelYear | ValuationCriteria.Mileage
                | ValuationCriteria.FuelAndTransmission | ValuationCriteria.Region,
            ValuationCriteria.MakeModelYear | ValuationCriteria.Mileage
                | ValuationCriteria.FuelAndTransmission,
            ValuationCriteria.MakeModelYear | ValuationCriteria.Mileage,
            ValuationCriteria.MakeModelYear
        };

        foreach (var criteria in tiers)
        {
            var prices = pool
                .Where(c => Matches(c, vehicle, region, criteria, settings))
                .Select(c => c.Price)
                .ToList();

            if (prices.Count < settings.MinComparables) continue;

            var median = Median(prices);
            var low  = Math.Round(median * (1 - settings.RangeSpread), 0);
            var high = Math.Round(median * (1 + settings.RangeSpread), 0);

            return new VehicleValuationResult(true, median, low, high, prices.Count, criteria);
        }

        // Ni con los criterios más amplios hay muestra: no se inventa nada.
        return VehicleValuationResult.NotEnoughData(pool.Count);
    }

    private static bool Matches(
        Comparable c, GarageVehicle vehicle, string? region, ValuationCriteria criteria,
        VehicleValuationSettings settings)
    {
        if (criteria.HasFlag(ValuationCriteria.Region)
            && !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(c.Region)
            && !string.Equals(region, c.Region, StringComparison.OrdinalIgnoreCase))
            return false;

        // Un criterio solo se aplica cuando los dos vehículos tienen el dato: exigir que
        // coincida un campo que el anuncio no rellena vaciaría la muestra.
        if (criteria.HasFlag(ValuationCriteria.Mileage)
            && vehicle.Mileage is { } mileage && c.Mileage is { } other
            && Math.Abs(other - mileage) > settings.MileageBandKm)
            return false;

        if (criteria.HasFlag(ValuationCriteria.FuelAndTransmission))
        {
            if (vehicle.FuelType is { } fuel && c.FuelType is { } otherFuel && fuel != otherFuel)
                return false;

            if (vehicle.Transmission is { } gearbox && c.Transmission is { } otherGearbox
                && gearbox != otherGearbox)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Mediana, no media: un anuncio con un precio disparatado desplazaría la media y
    /// daría una horquilla que no se parece al mercado.
    /// </summary>
    private static decimal Median(List<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2m;
    }

    /// <summary>
    /// Parámetros configurables. Si la fila no existe todavía se usan los valores por
    /// defecto de la entidad en lugar de fallar.
    /// </summary>
    private async Task<VehicleValuationSettings> LoadSettingsAsync(CancellationToken ct) =>
        await context.VehicleValuationSettings.AsNoTracking().FirstOrDefaultAsync(ct)
        ?? new VehicleValuationSettings();
}
