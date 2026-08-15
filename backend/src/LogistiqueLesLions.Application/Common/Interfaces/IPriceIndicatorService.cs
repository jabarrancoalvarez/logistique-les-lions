using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Resultado del cálculo para un anuncio. <see cref="Indicator"/> es <c>null</c> cuando
/// no hay suficientes vehículos comparables: en ese caso no debe mostrarse nada.
/// </summary>
/// <param name="Indicator">Bonne affaire / Prix correct / Prix élevé, o <c>null</c>.</param>
/// <param name="ComparablesCount">Anuncios usados como referencia.</param>
/// <param name="ReferencePrice">Mediana de los comparables.</param>
/// <param name="LowerBound">Por debajo de este precio se considera buena oferta.</param>
/// <param name="UpperBound">Por encima de este precio se considera elevado.</param>
public record PriceIndicatorResult(
    PriceIndicator? Indicator,
    int ComparablesCount,
    decimal? ReferencePrice,
    decimal? LowerBound,
    decimal? UpperBound
)
{
    /// <summary>Sin datos suficientes: no se muestra indicador.</summary>
    public static PriceIndicatorResult NotEnoughData(int comparablesCount) =>
        new(null, comparablesCount, null, null, null);
}

/// <summary>
/// Calcula el indicador estadístico de precio comparando un anuncio con vehículos
/// similares publicados en Yoon u Auto. No utiliza IA.
/// </summary>
public interface IPriceIndicatorService
{
    /// <summary>Indicador de un anuncio concreto.</summary>
    Task<PriceIndicatorResult> CalculateAsync(Guid vehicleId, CancellationToken ct = default);

    /// <summary>
    /// Indicador de varios anuncios a la vez. Lo usa el listado del Marketplace para no
    /// lanzar una consulta por tarjeta.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PriceIndicatorResult>> CalculateManyAsync(
        IReadOnlyCollection<Guid> vehicleIds, CancellationToken ct = default);
}
