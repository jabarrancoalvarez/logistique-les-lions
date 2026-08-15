namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Situación aduanera del vehículo. Por su importancia en Senegal constituye un bloque
/// propio de la ficha y un filtro destacado del Marketplace.
/// </summary>
/// <remarks>
/// ⚠️ El estado lo declara el usuario que publica. Yoon u Auto <b>no</b> debe presentarlo
/// como información verificada por la plataforma mientras no exista un procedimiento de
/// verificación documental.
/// </remarks>
public enum CustomsStatus
{
    /// <summary>Dédouané — derechos de aduana pagados.</summary>
    Dedouane = 1,
    /// <summary>Non dédouané — pendiente de despacho aduanero.</summary>
    NonDedouane = 2,
    /// <summary>Passavant — régimen temporal de circulación.</summary>
    Passavant = 3
}
