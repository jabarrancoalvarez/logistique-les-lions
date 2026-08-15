using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Parámetros de la estimación de valor de Mon Garage.
/// </summary>
/// <remarks>
/// Como los del indicador de precio, viven en base de datos y no en constantes del
/// código: son los umbrales que deciden si hay datos suficientes para atreverse a dar
/// una cifra, y el administrador debe poder ajustarlos sin tocar el código (parte P34).
///
/// Es una tabla de una sola fila, con identificador fijo.
/// </remarks>
public class VehicleValuationSettings : AuditableEntity
{
    public static readonly Guid SingletonId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Muestra mínima antes de generar una estimación.
    /// </summary>
    /// <remarks>
    /// Por debajo de este número <b>no se muestra ninguna cifra</b>: la especificación
    /// prohíbe inventarse una valoración.
    /// </remarks>
    public int MinComparables { get; set; } = 5;

    /// <summary>Antigüedad máxima de los anuncios usados como referencia.</summary>
    public int MaxListingAgeDays { get; set; } = 365;

    /// <summary>Años arriba y abajo dentro de los que un vehículo se considera comparable.</summary>
    public int YearBand { get; set; } = 2;

    /// <summary>Kilómetros arriba y abajo para considerar comparable el uso.</summary>
    public int MileageBandKm { get; set; } = 30_000;

    /// <summary>
    /// Ancho de la horquilla alrededor de la mediana. 0,05 = ±5 %, es decir
    /// <c>8.200.000 – 8.600.000 FCFA</c>.
    /// </summary>
    public decimal RangeSpread { get; set; } = 0.05m;

    /// <summary>
    /// Días entre dos instantáneas del historial de valor.
    /// </summary>
    /// <remarks>
    /// La evolución se construye guardando la estimación cada cierto tiempo. Con un
    /// intervalo corto el gráfico se llenaría de puntos que dicen lo mismo.
    /// </remarks>
    public int SnapshotIntervalDays { get; set; } = 30;
}
