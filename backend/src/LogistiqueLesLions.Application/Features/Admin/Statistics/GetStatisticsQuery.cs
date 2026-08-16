using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.SavedSearches;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Statistics;

/// <summary>
/// «Statistiques» — la lectura de negocio de la plataforma.
/// </summary>
/// <remarks>
/// El bloque que más importa es el desajuste entre oferta y demanda: saber qué modelos
/// busca la gente y no encuentra es lo que permite hablar con importadores y
/// concesionarios con datos en la mano.
/// </remarks>
public record GetStatisticsQuery(int Days = 30) : IRequest<Result<StatisticsDto>>;

public record StatisticsDto(
    int PeriodDays,
    StatsUsersDto Users,
    StatsSupplyDto Supply,
    StatsDemandDto Demand,
    StatsFunnelDto Funnel
);

// ─── Usuarios ──────────────────────────────────────────────────────────────

public record StatsUsersDto(
    int Total,
    int NewInPeriod,
    /// <summary>Con actividad en el periodo: han entrado al menos una vez.</summary>
    int Active,
    int Particuliers,
    int Professionnels,
    IReadOnlyList<LabelCountDto> ByRegion,
    /// <summary>Altas por día, para dibujar la evolución.</summary>
    IReadOnlyList<DayCountDto> SignupsPerDay
);

public record LabelCountDto(string Label, int Count);
public record DayCountDto(DateOnly Day, int Count);

// ─── Oferta ────────────────────────────────────────────────────────────────

/// <param name="MedianPrice">
/// Mediana y no media: unos pocos anuncios muy caros desplazarían el promedio y darían
/// una idea falsa del mercado.
/// </param>
public record StatsSupplyDto(
    int PublishedInPeriod,
    int ActiveListings,
    decimal? AveragePrice,
    decimal? MedianPrice,
    int? MedianMileage,
    int? MedianYear,
    IReadOnlyList<LabelCountDto> TopMakes,
    IReadOnlyList<LabelCountDto> TopModels,
    IReadOnlyList<LabelCountDto> ByCity,
    IReadOnlyList<LabelCountDto> ByFuel,
    IReadOnlyList<LabelCountDto> ByCustomsStatus
);

// ─── Demanda ───────────────────────────────────────────────────────────────

/// <param name="Gaps">
/// Modelos que se buscan más de lo que se ofrecen. Es el dato con más valor de todo el
/// panel.
/// </param>
public record StatsDemandDto(
    int SavedSearches,
    int FavoritesTotal,
    int Requests,
    decimal? MedianSearchBudget,
    IReadOnlyList<LabelCountDto> TopSearchedMakes,
    IReadOnlyList<LabelCountDto> TopFavoritedModels,
    IReadOnlyList<LabelCountDto> TopUsedFilters,
    IReadOnlyList<SupplyGapDto> Gaps
);

/// <param name="SearchingUsers">Personas con una búsqueda guardada que apunta a ese modelo.</param>
/// <param name="Requests">Solicitudes «Trouvez-moi cette voiture» de ese modelo.</param>
/// <param name="AvailableListings">Anuncios activos que podrían satisfacerlas.</param>
public record SupplyGapDto(
    string Label,
    int SearchingUsers,
    int Requests,
    int AvailableListings
);

// ─── Conversión ────────────────────────────────────────────────────────────

/// <remarks>
/// Permite ver dónde se pierde al usuario: de mirar a guardar, de guardar a hablar, de
/// hablar a ofertar, y de ahí al contrato y a la venta verificada.
/// </remarks>
public record StatsFunnelDto(
    int Views,
    int Favorites,
    int Negotiations,
    int Offers,
    int AcceptedOffers,
    int Contracts,
    int VerifiedSales
);

public class GetStatisticsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStatisticsQuery, Result<StatisticsDto>>
{
    /// <summary>Cuántas filas se muestran en cada ranking.</summary>
    private const int TopSize = 8;

    public async Task<Result<StatisticsDto>> Handle(GetStatisticsQuery request, CancellationToken ct)
    {
        var days = Math.Clamp(request.Days, 1, 365);
        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var users = await UsersAsync(days, since, ct);
        var supply = await SupplyAsync(since, ct);
        var demand = await DemandAsync(ct);
        var funnel = await FunnelAsync(since, ct);

        return Result<StatisticsDto>.Success(
            new StatisticsDto(days, users, supply, demand, funnel));
    }

    private async Task<StatsUsersDto> UsersAsync(
        int days, DateTimeOffset since, CancellationToken ct)
    {
        var users = db.UserProfiles.AsNoTracking();

        // Se agrupa en la base: son 14 regiones, pero el recuento lo hace el motor en vez
        // de traerse una fila por usuario.
        var regiones = await users
            .Where(u => u.Region != null)
            .GroupBy(u => u.Region!)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .ToListAsync(ct);

        var byRegion = regiones.Select(r => new LabelCountDto(r.Label, r.Count)).ToList();

        // Las altas por día siguen agrupándose en memoria: recortar un DateTimeOffset a
        // fecha depende del proveedor, y aquí la consulta ya está acotada al periodo, así
        // que lo que viaja son las altas de esos días y no la tabla entera.
        var signups = await users
            .Where(u => u.CreatedAt >= since)
            .Select(u => u.CreatedAt)
            .ToListAsync(ct);

        var perDay = signups
            .GroupBy(d => DateOnly.FromDateTime(d.UtcDateTime))
            .Select(g => new DayCountDto(g.Key, g.Count()))
            .OrderBy(x => x.Day)
            .ToList();

        return new StatsUsersDto(
            await users.CountAsync(ct),
            signups.Count,
            // «Activo» es haber entrado en el periodo: es lo único que la aplicación
            // registra hoy sobre actividad.
            await users.CountAsync(u => u.LastLoginAt >= since, ct),
            await users.CountAsync(u => u.AccountType == AccountType.Particulier, ct),
            await users.CountAsync(u => u.AccountType == AccountType.Professionnel, ct),
            byRegion,
            perDay);
    }

    private async Task<StatsSupplyDto> SupplyAsync(DateTimeOffset since, CancellationToken ct)
    {
        var active = db.Vehicles
            .AsNoTracking()
            .Where(v => v.Status == VehicleStatus.Actif || v.Status == VehicleStatus.Reserve);

        var precios     = active.Where(v => v.Price > 0).Select(v => v.Price);
        var kilometrajes = active.Where(v => v.Mileage > 0).Select(v => (decimal)v.Mileage!.Value);
        var anios       = active.Select(v => (decimal)v.Year);

        var hayPrecios = await precios.AnyAsync(ct);

        return new StatsSupplyDto(
            await db.Vehicles.AsNoTracking().CountAsync(v => v.PublishedAt >= since, ct),
            await active.CountAsync(ct),
            hayPrecios ? Math.Round(await precios.AverageAsync(ct), 0) : null,
            await MedianAsync(precios, ct),
            (int?)await MedianAsync(kilometrajes, ct),
            (int?)await MedianAsync(anios, ct),
            await TopAsync(active.Select(v => v.Make.Name), ct),
            await TopAsync(active.Where(v => v.Model != null)
                .Select(v => v.Make.Name + " " + v.Model!.Name), ct),
            await TopAsync(active.Where(v => v.City != null).Select(v => v.City!), ct),
            // Los enums se agrupan por su valor y se rotulan después: así el GROUP BY no
            // depende de cómo esté guardado el enum en la columna.
            await TopEnumAsync(active.Where(v => v.FuelType != null)
                .Select(v => v.FuelType!.Value), ct),
            await TopEnumAsync(active.Where(v => v.CustomsStatus != null)
                .Select(v => v.CustomsStatus!.Value), ct));
    }

    private async Task<StatsDemandDto> DemandAsync(CancellationToken ct)
    {
        // Los filtros guardados viven como JSON: se leen en memoria porque interpretarlos
        // en SQL ataría la consulta al proveedor.
        var savedSearches = await db.SavedSearches
            .AsNoTracking()
            .Select(s => new { s.UserId, s.FiltersJson })
            .ToListAsync(ct);

        var parsed = savedSearches
            .Select(s => new { s.UserId, Filters = SavedSearchFilters.Deserialize(s.FiltersJson) })
            .ToList();

        var makes = await db.VehicleMakes.AsNoTracking()
            .Select(m => new { m.Id, m.Name }).ToListAsync(ct);
        var models = await db.VehicleModels.AsNoTracking()
            .Select(m => new { m.Id, m.Name, m.MakeId }).ToListAsync(ct);

        var makeNames = makes.ToDictionary(m => m.Id, m => m.Name);
        var modelLabels = models.ToDictionary(
            m => m.Id,
            m => $"{makeNames.GetValueOrDefault(m.MakeId, "—")} {m.Name}");

        var searchedMakes = Top(parsed
            .Where(p => p.Filters.MakeId is not null)
            .Select(p => makeNames.GetValueOrDefault(p.Filters.MakeId!.Value, "—")));

        var usedFilters = Top(parsed.SelectMany(p => FilterNames(p.Filters)));

        var budgets = parsed
            .Where(p => p.Filters.PriceTo is > 0)
            .Select(p => p.Filters.PriceTo!.Value)
            .ToList();

        var favoritedModels = await TopAsync(db.SavedVehicles
            .AsNoTracking()
            .Join(db.Vehicles.AsNoTracking(), s => s.VehicleId, v => v.Id, (s, v) => v)
            .Where(v => v.ModelId != null)
            .Select(v => v.Make.Name + " " + v.Model!.Name), ct);

        var gaps = await GapsAsync(parsed.Select(p => (p.UserId, p.Filters.ModelId)), modelLabels, ct);

        return new StatsDemandDto(
            savedSearches.Count,
            await db.SavedVehicles.AsNoTracking().CountAsync(ct),
            await db.VehicleRequests.AsNoTracking().CountAsync(ct),
            Median(budgets),
            searchedMakes,
            favoritedModels,
            usedFilters,
            gaps);
    }

    /// <summary>
    /// Qué modelos se buscan más de lo que se ofrecen.
    /// </summary>
    /// <remarks>
    /// Se cruza lo que la gente guarda y pide con lo que hay publicado. Solo se listan
    /// los modelos con demanda real: uno sin nadie buscándolo no es un hueco de mercado,
    /// es un modelo que no interesa.
    /// </remarks>
    private async Task<List<SupplyGapDto>> GapsAsync(
        IEnumerable<(Guid UserId, Guid? ModelId)> searches,
        IReadOnlyDictionary<Guid, string> modelLabels,
        CancellationToken ct)
    {
        var searchers = searches
            .Where(s => s.ModelId is not null)
            .GroupBy(s => s.ModelId!.Value)
            // Una persona con tres búsquedas del mismo modelo sigue siendo una persona.
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());

        // Las solicitudes guardan el modelo como texto libre: se cruzan por nombre. Se
        // agrupa por marca y modelo en la base, que devuelve una fila por combinación
        // distinta en vez de una por solicitud.
        var requests = await db.VehicleRequests
            .AsNoTracking()
            .Where(r => r.ModelName != null)
            .GroupBy(r => new { MakeName = r.Make != null ? r.Make.Name : r.MakeName, r.ModelName })
            .Select(g => new { g.Key.MakeName, g.Key.ModelName, Count = g.Count() })
            .ToListAsync(ct);

        // El rótulo se compone fuera: concatenar dentro del GROUP BY ata la consulta al
        // proveedor, y aquí ya son pocas filas.
        var requestsByLabel = requests
            .GroupBy(r => $"{r.MakeName} {r.ModelName}".Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count), StringComparer.OrdinalIgnoreCase);

        var supply = await db.Vehicles
            .AsNoTracking()
            .Where(v => v.Status == VehicleStatus.Actif && v.ModelId != null)
            .GroupBy(v => new { Make = v.Make.Name, Model = v.Model!.Name })
            .Select(g => new { g.Key.Make, g.Key.Model, Count = g.Count() })
            .ToListAsync(ct);

        var supplyByLabel = supply
            .GroupBy(v => $"{v.Make} {v.Model}".Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count), StringComparer.OrdinalIgnoreCase);

        var labels = searchers.Keys
            .Select(id => modelLabels.GetValueOrDefault(id))
            .Where(l => l is not null)
            .Select(l => l!)
            .Concat(requestsByLabel.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var gaps = new List<SupplyGapDto>();

        foreach (var label in labels)
        {
            var modelId = modelLabels
                .FirstOrDefault(kv => string.Equals(kv.Value, label, StringComparison.OrdinalIgnoreCase))
                .Key;

            var searching = modelId != Guid.Empty ? searchers.GetValueOrDefault(modelId) : 0;
            var requested = requestsByLabel.GetValueOrDefault(label);
            var available = supplyByLabel.GetValueOrDefault(label);

            if (searching + requested == 0) continue;

            gaps.Add(new SupplyGapDto(label, searching, requested, available));
        }

        // Lo que más se busca y menos hay, primero.
        return gaps
            .OrderByDescending(g => g.SearchingUsers + g.Requests - g.AvailableListings)
            .ThenByDescending(g => g.SearchingUsers + g.Requests)
            .Take(TopSize)
            .ToList();
    }

    private async Task<StatsFunnelDto> FunnelAsync(DateTimeOffset since, CancellationToken ct)
    {
        return new StatsFunnelDto(
            // Las visitas son acumuladas: el anuncio no guarda cuándo se vio cada una.
            await db.Vehicles.AsNoTracking().SumAsync(v => v.ViewsCount, ct),
            await db.SavedVehicles.AsNoTracking().CountAsync(ct),
            await db.Negotiations.AsNoTracking().CountAsync(n => n.CreatedAt >= since, ct),
            await db.Offers.AsNoTracking().CountAsync(o => o.CreatedAt >= since, ct),
            await db.Offers.AsNoTracking()
                .CountAsync(o => o.Status == OfferStatus.Acceptee && o.CreatedAt >= since, ct),
            await db.Contracts.AsNoTracking().CountAsync(c => c.CreatedAt >= since, ct),
            await db.Contracts.AsNoTracking()
                .CountAsync(c => c.Status == ContractStatus.Valide && c.CreatedAt >= since, ct));
    }

    // ─── Ayudas ────────────────────────────────────────────────────────────

    private static List<LabelCountDto> Top(IEnumerable<string> values) =>
        values
            .GroupBy(v => v)
            .Select(g => new LabelCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .Take(TopSize)
            .ToList();

    /// <summary>Ranking resuelto por el motor: vuelven ocho filas, no la tabla.</summary>
    /// <remarks>
    /// ⚠️ Se ordena sobre un tipo anónimo y el DTO se construye al final, ya en memoria.
    /// Proyectar a <see cref="LabelCountDto"/> antes de ordenar rompe la traducción:
    /// EF no sabe ordenar por una propiedad de un tipo que él mismo acaba de proyectar.
    /// </remarks>
    private static async Task<List<LabelCountDto>> TopAsync(
        IQueryable<string> values, CancellationToken ct)
    {
        var filas = await values
            .GroupBy(v => v)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .Take(TopSize)
            .ToListAsync(ct);

        return filas.Select(f => new LabelCountDto(f.Label, f.Count)).ToList();
    }

    /// <summary>
    /// Igual, para columnas de enum: se agrupa por el valor y se rotula al salir.
    /// </summary>
    private static async Task<List<LabelCountDto>> TopEnumAsync<TEnum>(
        IQueryable<TEnum> values, CancellationToken ct) where TEnum : struct, Enum
    {
        var filas = await values
            .GroupBy(v => v)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(TopSize)
            .ToListAsync(ct);

        return filas
            .Select(f => new LabelCountDto(f.Key.ToString()!, f.Count))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .ToList();
    }

    /// <summary>
    /// Mediana calculada en la base: se cuenta y se piden solo las filas centrales.
    /// </summary>
    /// <remarks>
    /// Son dos consultas y como mucho dos filas, en vez de traerse todos los valores para
    /// ordenarlos aquí. Se evita a propósito <c>percentile_cont</c>, que resolvería en una
    /// sola consulta pero solo existe en PostgreSQL y dejaría estas estadísticas sin
    /// poder probarse.
    ///
    /// Devuelve <c>null</c> si no hay datos: no se inventa un valor central donde no hay
    /// valores.
    /// </remarks>
    private static async Task<decimal?> MedianAsync(IQueryable<decimal> values, CancellationToken ct)
    {
        var total = await values.CountAsync(ct);
        if (total == 0) return null;

        var ordenados = values.OrderBy(v => v);

        if (total % 2 == 1)
            return await ordenados.Skip(total / 2).FirstAsync(ct);

        var centrales = await ordenados.Skip(total / 2 - 1).Take(2).ToListAsync(ct);
        return Math.Round((centrales[0] + centrales[1]) / 2m, 0);
    }

    /// <summary>Qué filtros usó una búsqueda guardada, por su nombre.</summary>
    private static IEnumerable<string> FilterNames(
        Features.Vehicles.Queries.GetVehicles.GetVehiclesQuery f)
    {
        if (!string.IsNullOrWhiteSpace(f.Search)) yield return "Recherche libre";
        if (f.MakeId is not null) yield return "Marque";
        if (f.ModelId is not null) yield return "Modèle";
        if (f.PriceFrom is not null || f.PriceTo is not null) yield return "Prix";
        if (f.YearFrom is not null || f.YearTo is not null) yield return "Année";
        if (f.MileageFrom is not null || f.MileageTo is not null) yield return "Kilométrage";
        if (!string.IsNullOrWhiteSpace(f.Region)) yield return "Région";
        if (!string.IsNullOrWhiteSpace(f.City)) yield return "Ville";
        if (f.CustomsStatus is not null) yield return "Statut douanier";
        if (f.FuelType is not null) yield return "Carburant";
        if (f.Transmission is not null) yield return "Boîte";
        if (f.BodyType is not null) yield return "Carrosserie";
        if (f.Drivetrain is not null) yield return "Transmission";
        if (f.PowerFrom is not null || f.PowerTo is not null) yield return "Puissance";
        if (f.EquipmentIds is { Count: > 0 }) yield return "Équipements";
    }

    /// <summary>
    /// Mediana. <c>null</c> si no hay datos: no se inventa un valor central donde no
    /// hay valores.
    /// </summary>
    private static decimal? Median(List<decimal> values)
    {
        if (values.Count == 0) return null;

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;

        return sorted.Count % 2 == 1
            ? sorted[mid]
            : Math.Round((sorted[mid - 1] + sorted[mid]) / 2m, 0);
    }
}
