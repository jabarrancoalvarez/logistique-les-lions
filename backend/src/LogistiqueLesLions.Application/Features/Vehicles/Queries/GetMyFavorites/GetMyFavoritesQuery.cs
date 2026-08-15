using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetMyFavorites;

/// <summary>Mes recherches → Favoris.</summary>
public record GetMyFavoritesQuery(Guid UserId) : IRequest<Result<FavoritesDto>>;

/// <param name="AlertsAllEnabled">
/// Interruptor general: todos los favoritos reciben alertas de bajada de precio.
/// </param>
public record FavoritesDto(bool AlertsAllEnabled, IReadOnlyList<FavoriteItemDto> Items);

/// <param name="Vehicle">Datos actuales del anuncio; el favorito nunca guarda una copia.</param>
/// <param name="PriceWhenSaved">Precio en el momento de guardarlo.</param>
/// <param name="PriceDrop">Bajada acumulada desde entonces, o <c>null</c> si no ha bajado.</param>
/// <param name="AlertEnabled">Alerta específica de este favorito.</param>
public record FavoriteItemDto(
    VehicleListDto Vehicle,
    decimal PriceWhenSaved,
    decimal? PriceDrop,
    bool AlertEnabled,
    DateTimeOffset SavedAt
);
