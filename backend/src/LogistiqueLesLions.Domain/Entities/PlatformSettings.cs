using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Parámetros generales de la plataforma.
/// </summary>
/// <remarks>
/// La especificación es explícita sobre por qué existe esta tabla: «evitar que cualquier
/// pequeño cambio de negocio requiera modificar el código». Todo lo que aquí vive es un
/// número que el negocio puede querer mover un martes por la tarde.
///
/// Es una tabla de una sola fila, con identificador fijo, como
/// <see cref="PriceIndicatorSettings"/> y <see cref="VehicleValuationSettings"/>.
/// </remarks>
public class PlatformSettings : AuditableEntity
{
    public static readonly Guid SingletonId = Guid.Parse("30000000-0000-0000-0000-000000000003");

    /// <summary>Vehículos que caben a la vez en el comparador. El documento parte de 3.</summary>
    public int ComparatorMaxVehicles { get; set; } = 3;

    /// <summary>Puntos que genera una venta verificada.</summary>
    public int PointsPerVerifiedSale { get; set; } = 100;

    /// <summary>
    /// Días que un anuncio permanece activo antes de pedirle al vendedor que confirme
    /// que sigue en venta.
    /// </summary>
    public int ListingFreshnessDays { get; set; } = 60;

    /// <summary>Fotografías máximas por anuncio.</summary>
    public int MaxImagesPerListing { get; set; } = 20;

    /// <summary>
    /// Versión vigente de las condiciones de uso.
    /// </summary>
    /// <remarks>
    /// El documento pide poder administrar «textos legales/versiones». Aquí vive la
    /// versión; el texto sigue en las páginas legales.
    /// </remarks>
    public string LegalTermsVersion { get; set; } = "1.0";

    public DateTimeOffset? LegalTermsUpdatedAt { get; set; }
}

/// <summary>
/// Interruptor de una funcionalidad.
/// </summary>
/// <remarks>
/// Los flags son filas y no columnas de <see cref="PlatformSettings"/> porque nacen y
/// mueren: uno se enciende para una campaña y se retira dos meses después. Como columnas,
/// cada uno costaría una migración.
///
/// La <see cref="Key"/> es el contrato con el código: se compara con las constantes de
/// <c>FeatureFlagKeys</c>, nunca con la etiqueta que ve el administrador.
/// </remarks>
public class FeatureFlag : AuditableEntity
{
    public string Key { get; set; } = string.Empty;

    /// <summary>Etiqueta en francés, para la pantalla de configuración.</summary>
    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;
}

/// <summary>Claves de los interruptores que el código consulta.</summary>
public static class FeatureFlagKeys
{
    public const string PriceIndicator = "price_indicator";
    public const string VehicleValuation = "vehicle_valuation";
    public const string Comparator = "comparator";
    public const string VehicleRequests = "vehicle_requests";
    public const string UpcomingFeatures = "upcoming_features";
}
