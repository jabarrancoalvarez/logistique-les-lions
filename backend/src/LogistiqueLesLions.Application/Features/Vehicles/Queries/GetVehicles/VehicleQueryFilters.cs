using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

/// <summary>
/// Traducción de los filtros del Marketplace a una consulta.
/// </summary>
/// <remarks>
/// Vive aparte del handler porque el listado y el contador de resultados tienen que
/// aplicar <b>exactamente</b> los mismos criterios: si divergieran, el panel de filtros
/// anunciaría un número de resultados distinto del que luego se muestra.
/// </remarks>
public static class VehicleQueryFilters
{
    public static IQueryable<Vehicle> Apply(IApplicationDbContext context, GetVehiclesQuery r)
    {
        // Por defecto solo se ven los anuncios que la especificación considera
        // públicamente visibles. Los demás estados requieren permiso explícito, que la
        // API concede únicamente al propietario y al administrador.
        var query = r.IncludeNonPublic
            ? context.Vehicles.AsNoTracking().IgnoreQueryFilters().Where(v => v.DeletedAt == null)
            : context.Vehicles.AsNoTracking().Where(v =>
                (v.Status == VehicleStatus.Actif || v.Status == VehicleStatus.Reserve)
                // Un anuncio ocultado por moderación desaparece del Marketplace aunque
                // su estado siga siendo «Actif»: la medida no la levanta quien publica.
                && v.AdminHiddenAt == null);

        // ─── Barra de búsqueda ─────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var term = r.Search.Trim().ToLower();
            query = query.Where(v =>
                v.Title.ToLower().Contains(term) ||
                v.Make.Name.ToLower().Contains(term) ||
                (v.Model != null && v.Model.Name.ToLower().Contains(term)) ||
                (v.Version != null && v.Version.ToLower().Contains(term)) ||
                v.PublicReference.ToLower() == term);
        }

        // ─── Marca y modelo ────────────────────────────────────────────────
        if (r.MakeId.HasValue)  query = query.Where(v => v.MakeId == r.MakeId.Value);
        if (r.ModelId.HasValue) query = query.Where(v => v.ModelId == r.ModelId.Value);

        // ─── Precio, año y kilometraje ─────────────────────────────────────
        if (r.PriceFrom.HasValue)   query = query.Where(v => v.Price >= r.PriceFrom.Value);
        if (r.PriceTo.HasValue)     query = query.Where(v => v.Price <= r.PriceTo.Value);
        if (r.YearFrom.HasValue)    query = query.Where(v => v.Year >= r.YearFrom.Value);
        if (r.YearTo.HasValue)      query = query.Where(v => v.Year <= r.YearTo.Value);
        if (r.MileageFrom.HasValue) query = query.Where(v => v.Mileage >= r.MileageFrom.Value);
        if (r.MileageTo.HasValue)   query = query.Where(v => v.Mileage <= r.MileageTo.Value);

        // ─── Ubicación y aduana ────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(r.Region)) query = query.Where(v => v.Region == r.Region);
        if (!string.IsNullOrWhiteSpace(r.City))   query = query.Where(v => v.City == r.City);

        // ─── Mecánica ──────────────────────────────────────────────────────
        if (r.FuelType.HasValue)     query = query.Where(v => v.FuelType == r.FuelType.Value);
        if (r.Transmission.HasValue) query = query.Where(v => v.Transmission == r.Transmission.Value);
        if (r.BodyType.HasValue)     query = query.Where(v => v.BodyType == r.BodyType.Value);
        if (r.Drivetrain.HasValue)   query = query.Where(v => v.Drivetrain == r.Drivetrain.Value);
        if (r.Condition.HasValue)    query = query.Where(v => v.Condition == r.Condition.Value);

        if (r.PowerFrom.HasValue)        query = query.Where(v => v.PowerCv >= r.PowerFrom.Value);
        if (r.PowerTo.HasValue)          query = query.Where(v => v.PowerCv <= r.PowerTo.Value);
        if (r.DisplacementFrom.HasValue) query = query.Where(v => v.EngineDisplacementCc >= r.DisplacementFrom.Value);
        if (r.DisplacementTo.HasValue)   query = query.Where(v => v.EngineDisplacementCc <= r.DisplacementTo.Value);

        if (r.DoorsFrom.HasValue) query = query.Where(v => v.Doors >= r.DoorsFrom.Value);
        if (r.DoorsTo.HasValue)   query = query.Where(v => v.Doors <= r.DoorsTo.Value);
        if (r.SeatsFrom.HasValue) query = query.Where(v => v.Seats >= r.SeatsFrom.Value);
        if (r.SeatsTo.HasValue)   query = query.Where(v => v.Seats <= r.SeatsTo.Value);

        if (!string.IsNullOrWhiteSpace(r.Color))
        {
            var color = r.Color.Trim().ToLower();
            query = query.Where(v => v.Color != null && v.Color.ToLower() == color);
        }

        // ─── Equipamiento: el anuncio debe declararlos TODOS ───────────────
        if (r.EquipmentIds is { Count: > 0 })
        {
            // Se encadena un Any por equipamiento en lugar de un único Contains sobre la
            // lista: con un solo Any la condición sería "alguno de ellos", no "todos".
            foreach (var equipmentId in r.EquipmentIds.Distinct())
            {
                var id = equipmentId;
                query = query.Where(v => v.Equipments.Any(e => e.EquipmentId == id));
            }
        }

        // ─── Quién publica ─────────────────────────────────────────────────
        if (r.SellerAccountType.HasValue)
            query = query.Where(v => v.Seller != null && v.Seller.AccountType == r.SellerAccountType.Value);

        // ─── Heredado de los módulos de exportación ────────────────────────
        if (!string.IsNullOrWhiteSpace(r.CountryOrigin))
            query = query.Where(v => v.CountryOrigin == r.CountryOrigin);
        if (r.IsExportReady.HasValue)
            query = query.Where(v => v.IsExportReady == r.IsExportReady.Value);

        // ─── Uso interno ───────────────────────────────────────────────────
        if (r.IsFeatured.HasValue)
        {
            // «Destacado» ya no es una bandera: es tener un nivel vigente (no caducado).
            var now = DateTimeOffset.UtcNow;
            query = r.IsFeatured.Value
                ? query.Where(v => v.FeaturedTier != FeaturedTier.Aucune && v.FeaturedUntil > now)
                : query.Where(v => v.FeaturedTier == FeaturedTier.Aucune || v.FeaturedUntil <= now);
        }
        if (r.SellerId.HasValue)   query = query.Where(v => v.SellerId == r.SellerId.Value);
        if (r.Status.HasValue)     query = query.Where(v => v.Status == r.Status.Value);

        return query;
    }

    /// <summary>Las cinco ordenaciones de la especificación.</summary>
    /// <remarks>
    /// Los destacados solo se fijan arriba en el <b>orden por defecto</b> (por fecha).
    /// Si el usuario ordena por precio, año, kilometraje o vistas se respeta su elección
    /// y los destacados solo se distinguen por su distintivo, sin reordenar.
    /// </remarks>
    public static IQueryable<Vehicle> ApplySorting(IQueryable<Vehicle> query, GetVehiclesQuery r) =>
        r.SortBy.ToLowerInvariant() switch
        {
            "price"   => r.SortDesc ? query.OrderByDescending(v => v.Price)      : query.OrderBy(v => v.Price),
            "year"    => r.SortDesc ? query.OrderByDescending(v => v.Year)       : query.OrderBy(v => v.Year),
            "mileage" => r.SortDesc ? query.OrderByDescending(v => v.Mileage)    : query.OrderBy(v => v.Mileage),
            "views"   => r.SortDesc ? query.OrderByDescending(v => v.ViewsCount) : query.OrderBy(v => v.ViewsCount),
            _         => ApplyDefaultSort(query, r),
        };

    /// <summary>
    /// Orden por defecto: primero «À la une», luego «En vedette», luego el resto; y dentro
    /// de cada grupo, por fecha de publicación. Es el único orden que fija los destacados.
    /// </summary>
    private static IQueryable<Vehicle> ApplyDefaultSort(IQueryable<Vehicle> query, GetVehiclesQuery r)
    {
        var now = DateTimeOffset.UtcNow;

        var ranked = query.OrderByDescending(v =>
            v.FeaturedUntil > now
                ? (v.FeaturedTier == FeaturedTier.ALaUne
                    ? 2
                    : (v.FeaturedTier == FeaturedTier.EnVedette ? 1 : 0))
                : 0);

        return r.SortDesc
            ? ranked.ThenByDescending(v => v.CreatedAt)
            : ranked.ThenBy(v => v.CreatedAt);
    }
}
