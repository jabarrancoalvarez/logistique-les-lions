using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;

namespace LogistiqueLesLions.Application.Features.SavedSearches;

/// <summary>
/// Guarda los filtros actuales del Marketplace como búsqueda del usuario.
/// </summary>
/// <param name="Name">Título mostrado, p. ej. "Toyota Hilux".</param>
/// <param name="Filters">Filtros exactos utilizados.</param>
public record CreateSavedSearchCommand(
    Guid UserId,
    string Name,
    GetVehiclesQuery Filters,
    bool AlertEnabled
) : IRequest<Result<Guid>>;

/// <summary>Actualiza el nombre y los filtros de una búsqueda existente.</summary>
public record UpdateSavedSearchCommand(
    Guid UserId,
    Guid SearchId,
    string Name,
    GetVehiclesQuery Filters
) : IRequest<Result>;

/// <summary>Alerte nouveaux véhicules: ON/OFF.</summary>
public record SetSavedSearchAlertCommand(Guid UserId, Guid SearchId, bool Enabled) : IRequest<Result>;

public record DeleteSavedSearchCommand(Guid UserId, Guid SearchId) : IRequest<Result>;
