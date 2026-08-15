namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Genera las referencias públicas legibles que la especificación funcional muestra al
/// usuario: <c>Réf. Yoon: #YU12345</c>.
/// </summary>
/// <remarks>
/// Se resuelve con una secuencia de PostgreSQL en lugar de contar filas para que dos
/// altas simultáneas no puedan obtener el mismo número.
/// </remarks>
public interface IPublicReferenceGenerator
{
    /// <summary>Siguiente referencia de anuncio, con el formato <c>YU00001</c>.</summary>
    Task<string> NextVehicleReferenceAsync(CancellationToken ct = default);

    /// <summary>
    /// Siguiente referencia de solicitud «Trouvez-moi une voiture», con el formato
    /// <c>YD00248</c>. Usa su propia secuencia para que la numeración no se mezcle
    /// con la de los anuncios.
    /// </summary>
    Task<string> NextRequestReferenceAsync(CancellationToken ct = default);

    /// <summary>
    /// Siguiente referencia de contrato, con el formato <c>YC00125</c>. Identifica
    /// también la venta verificada asociada.
    /// </summary>
    Task<string> NextContractReferenceAsync(CancellationToken ct = default);

    /// <summary>
    /// Siguiente referencia de reporte, con el formato <c>SG00042</c>. Es lo que el
    /// usuario y el administrador citan al hablar de un signalement.
    /// </summary>
    Task<string> NextReportReferenceAsync(CancellationToken ct = default);
}
