using System.Text.RegularExpressions;

namespace LogistiqueLesLions.Application.Common;

/// <summary>
/// Normalización y validación de números de teléfono senegaleses.
/// El teléfono es el identificador principal de la cuenta, por lo que debe almacenarse
/// siempre en un formato canónico único (E.164) para que el índice UNIQUE sea fiable:
/// "77 123 45 67", "+221 77 123 45 67" y "00221771234567" son el mismo número.
/// </summary>
public static partial class SenegalPhone
{
    private const string CountryCode = "+221";

    /// <summary>E.164 senegalés: +221 seguido de 9 dígitos.</summary>
    [GeneratedRegex(@"^\+221[0-9]{9}$")]
    private static partial Regex E164();

    /// <summary>Todo lo que no sea dígito o el "+" inicial.</summary>
    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex Noise();

    /// <summary>
    /// Devuelve el número en formato +221XXXXXXXXX, o <c>null</c> si no es un número
    /// senegalés válido.
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var digits = Noise().Replace(input.Trim(), string.Empty);

        // Prefijo internacional en cualquiera de sus formas.
        if (digits.StartsWith("00221", StringComparison.Ordinal))
            digits = CountryCode + digits[5..];
        else if (digits.StartsWith("221", StringComparison.Ordinal) && digits.Length == 12)
            digits = CountryCode + digits[3..];
        else if (!digits.StartsWith('+'))
            digits = CountryCode + digits.TrimStart('0');

        return E164().IsMatch(digits) ? digits : null;
    }

    /// <summary><c>true</c> si el valor corresponde a un teléfono senegalés válido.</summary>
    public static bool IsValid(string? input) => Normalize(input) is not null;

    /// <summary>Formato legible para la interfaz: +221 77 123 45 67.</summary>
    public static string Format(string? normalized)
    {
        if (normalized is null || !E164().IsMatch(normalized)) return normalized ?? string.Empty;
        var n = normalized[4..];
        return $"{CountryCode} {n[..2]} {n[2..5]} {n[5..7]} {n[7..]}";
    }
}
