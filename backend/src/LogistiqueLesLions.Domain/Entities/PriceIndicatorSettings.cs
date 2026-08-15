using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Parámetros del indicador estadístico de precio.
/// </summary>
/// <remarks>
/// Viven en base de datos y no en constantes del código porque la especificación lo
/// exige: «Así no tenemos esos porcentajes enterrados en el código». El administrador
/// los edita desde Administration → Configuration (parte P34).
///
/// Es una tabla de una sola fila, con identificador fijo.
/// </remarks>
public class PriceIndicatorSettings : AuditableEntity
{
    /// <summary>Identificador único de la fila de configuración.</summary>
    public static readonly Guid SingletonId = Guid.Parse("30000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Anuncios comparables mínimos para atreverse a mostrar un indicador.
    /// Por debajo de este número no se muestra nada.
    /// </summary>
    public int MinComparables { get; set; } = 5;

    /// <summary>Antigüedad máxima de los anuncios usados como referencia.</summary>
    public int MaxListingAgeDays { get; set; } = 180;

    /// <summary>Años arriba y abajo dentro de los que un vehículo se considera comparable.</summary>
    public int YearBand { get; set; } = 2;

    /// <summary>
    /// Margen por debajo de la mediana a partir del cual el precio es una buena oferta.
    /// 0,10 = 10 %.
    /// </summary>
    public decimal GoodDealMargin { get; set; } = 0.10m;

    /// <summary>Margen por encima de la mediana a partir del cual el precio es elevado.</summary>
    public decimal HighPriceMargin { get; set; } = 0.10m;
}
