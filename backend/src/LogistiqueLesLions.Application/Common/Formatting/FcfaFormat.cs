using System.Globalization;

namespace LogistiqueLesLions.Application.Common.Formatting;

/// <summary>
/// Formato de las cifras de Yoon u Auto: <c>8.900.000</c>.
/// </summary>
/// <remarks>
/// El separador de millares es el punto, como fija el documento para los importes en
/// FCFA. Se declara aquí en lugar de pedirle a .NET una cultura que lo haga —se usaba
/// <c>de-DE</c> por sus convenciones— porque <b>la imagen de producción es Alpine</b>,
/// que corre en modo <i>globalization-invariant</i>: allí no existe ninguna cultura
/// salvo la invariante y pedir cualquier otra lanza una excepción.
///
/// Eso tumbaba en producción cuatro cosas a la vez: hacer una oferta, las alertas de
/// bajada de precio, los recordatorios de Mon Garage y el PDF del contrato. Todas
/// devolvían 500 sin más pista que «de-de is an invalid culture identifier».
///
/// Definirlo explícitamente es además más honesto: el formato lo manda la
/// especificación, no las costumbres de un país.
/// </remarks>
public static class FcfaFormat
{
    public static readonly NumberFormatInfo Numbers = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = [3]
    };

    /// <summary>«8.900.000», sin la moneda.</summary>
    public static string Amount(decimal value) => value.ToString("N0", Numbers);

    /// <summary>«8.900.000 FCFA».</summary>
    public static string WithCurrency(decimal value) => $"{Amount(value)} FCFA";

    /// <summary>«120.000 km».</summary>
    public static string Kilometres(int value) => $"{value.ToString("N0", Numbers)} km";
}
