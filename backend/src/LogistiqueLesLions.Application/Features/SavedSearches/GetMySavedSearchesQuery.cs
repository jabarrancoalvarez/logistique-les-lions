using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.SavedSearches;

/// <summary>Mes recherches → Recherches enregistrées.</summary>
public record GetMySavedSearchesQuery(Guid UserId) : IRequest<Result<List<SavedSearchDto>>>;

/// <param name="Filters">
/// Los filtros guardados. El frontend los usa tanto para el resumen visible
/// («2017–2022 · ≤150.000 km · Dakar») como para reabrir la búsqueda.
/// </param>
/// <param name="ResultsCount">"23 véhicules disponibles" en el momento de consultar.</param>
public record SavedSearchDto(
    Guid Id,
    string Name,
    GetVehiclesQuery Filters,
    bool AlertEnabled,
    int ResultsCount,
    DateTimeOffset CreatedAt
);

public class GetMySavedSearchesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetMySavedSearchesQuery, Result<List<SavedSearchDto>>>
{
    public async Task<Result<List<SavedSearchDto>>> Handle(
        GetMySavedSearchesQuery request, CancellationToken ct)
    {
        var searches = await context.SavedSearches
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.Id, s.Name, s.FiltersJson, s.AlertEnabled, s.CreatedAt })
            .ToListAsync(ct);

        var items = new List<SavedSearchDto>(searches.Count);

        foreach (var s in searches)
        {
            var filters = SavedSearchFilters.Deserialize(s.FiltersJson);

            // Se cuenta con los mismos criterios que aplica el Marketplace, para que el
            // número anunciado coincida con lo que el usuario verá al pulsar "Voir".
            var count = await VehicleQueryFilters
                .Apply(context, filters)
                .CountAsync(ct);

            items.Add(new SavedSearchDto(s.Id, s.Name, filters, s.AlertEnabled, count, s.CreatedAt));
        }

        return Result<List<SavedSearchDto>>.Success(items);
    }
}
