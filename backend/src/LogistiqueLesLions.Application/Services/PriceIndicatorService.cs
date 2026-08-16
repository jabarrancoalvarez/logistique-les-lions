using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Services;

/// <inheritdoc />
public class PriceIndicatorService(IApplicationDbContext context) : IPriceIndicatorService
{
    /// <summary>Datos mínimos de un anuncio para servir de comparable.</summary>
    private sealed record Comparable(Guid Id, Guid MakeId, Guid? ModelId, int Year, decimal Price);

    public async Task<PriceIndicatorResult> CalculateAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var results = await CalculateManyAsync([vehicleId], ct);
        return results.TryGetValue(vehicleId, out var result)
            ? result
            : PriceIndicatorResult.NotEnoughData(0);
    }

    public async Task<IReadOnlyDictionary<Guid, PriceIndicatorResult>> CalculateManyAsync(
        IReadOnlyCollection<Guid> vehicleIds, CancellationToken ct = default)
    {
        if (vehicleIds.Count == 0)
            return new Dictionary<Guid, PriceIndicatorResult>();

        var settings = await LoadSettingsAsync(ct);

        var targets = await context.Vehicles
            .AsNoTracking()
            .Where(v => vehicleIds.Contains(v.Id))
            .Select(v => new { v.Id, v.MakeId, v.ModelId, v.Year, v.Price })
            .ToListAsync(ct);

        if (targets.Count == 0)
            return new Dictionary<Guid, PriceIndicatorResult>();

        // Un único viaje a base de datos para todas las tarjetas de la página: se traen
        // los comparables y se reparten en memoria. Ese viaje único evita un N+1 por
        // tarjeta y se mantiene.
        //
        // ⚠️ Lo que se acota es el cubo. Antes se pedían **todos** los anuncios activos de
        // cada marca de la página, y luego se descartaban aquí por modelo y por año: una
        // página con ocho marcas se traía todos los Toyota, todos los Renault y todos los
        // Peugeot del catálogo, en cada carga del listado público. Ahora esas dos
        // condiciones viajan a la consulta.
        //
        // El filtro de abajo sigue siendo el que manda: esto es un superconjunto suyo, no
        // un criterio distinto. Un comparable solo cuenta si comparte marca y modelo con
        // algún objetivo y cae dentro de su franja de años, así que nada de lo que sirve
        // queda fuera.
        var makeIds  = targets.Select(t => t.MakeId).Distinct().ToList();
        var modelIds = targets.Where(t => t.ModelId is not null)
                              .Select(t => t.ModelId!.Value).Distinct().ToList();

        // Los anuncios sin modelo se comparan entre sí, así que hay que dejarlos entrar.
        var hayObjetivoSinModelo = targets.Any(t => t.ModelId is null);

        // Franja que cubre a todos los objetivos de la página a la vez.
        var anioMin = targets.Min(t => t.Year) - settings.YearBand;
        var anioMax = targets.Max(t => t.Year) + settings.YearBand;

        var cutoff = DateTimeOffset.UtcNow.AddDays(-settings.MaxListingAgeDays);

        var pool = await context.Vehicles
            .AsNoTracking()
            .Where(v => makeIds.Contains(v.MakeId)
                        && (v.Status == VehicleStatus.Actif || v.Status == VehicleStatus.Reserve)
                        // La antigüedad se mide desde la publicación, no desde la
                        // creación: un borrador antiguo publicado ayer es un anuncio nuevo.
                        && (v.PublishedAt ?? v.CreatedAt) >= cutoff
                        && v.Year >= anioMin && v.Year <= anioMax
                        && ((v.ModelId != null && modelIds.Contains(v.ModelId.Value))
                            || (hayObjetivoSinModelo && v.ModelId == null)))
            .Select(v => new Comparable(v.Id, v.MakeId, v.ModelId, v.Year, v.Price))
            .ToListAsync(ct);

        var results = new Dictionary<Guid, PriceIndicatorResult>(targets.Count);

        foreach (var target in targets)
        {
            // El propio anuncio queda fuera de su referencia: su precio no debe influir
            // en la mediana con la que se le compara.
            var comparables = pool
                .Where(c => c.Id != target.Id
                            && c.MakeId == target.MakeId
                            && c.ModelId == target.ModelId
                            && Math.Abs(c.Year - target.Year) <= settings.YearBand)
                .Select(c => c.Price)
                .ToList();

            results[target.Id] = Evaluate(target.Price, comparables, settings);
        }

        return results;
    }

    /// <summary>
    /// Compara el precio con la mediana de los comparables.
    /// </summary>
    /// <remarks>
    /// Se usa la mediana y no la media porque un solo anuncio con un precio disparatado
    /// desplazaría la media y etiquetaría como «bonne affaire» todo lo demás.
    /// </remarks>
    private static PriceIndicatorResult Evaluate(
        decimal price, List<decimal> comparables, PriceIndicatorSettings settings)
    {
        if (comparables.Count < settings.MinComparables)
            return PriceIndicatorResult.NotEnoughData(comparables.Count);

        var median = Median(comparables);
        var lower = median * (1 - settings.GoodDealMargin);
        var upper = median * (1 + settings.HighPriceMargin);

        var indicator = price <= lower ? PriceIndicator.BonneAffaire
                      : price >= upper ? PriceIndicator.PrixEleve
                      : PriceIndicator.PrixCorrect;

        return new PriceIndicatorResult(indicator, comparables.Count, median, lower, upper);
    }

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
    private async Task<PriceIndicatorSettings> LoadSettingsAsync(CancellationToken ct) =>
        await context.PriceIndicatorSettings.AsNoTracking().FirstOrDefaultAsync(ct)
        ?? new PriceIndicatorSettings();
}
