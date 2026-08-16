using System.Globalization;
using FluentAssertions;
using LogistiqueLesLions.Application.Common.Formatting;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Common;

/// <summary>
/// Formato de las cifras en FCFA.
/// </summary>
/// <remarks>
/// Existe por un fallo de producción: el código pedía la cultura <c>de-DE</c> para
/// conseguir el punto como separador de millares, y la imagen de producción es Alpine,
/// que corre en modo <i>globalization-invariant</i>. Allí no hay más cultura que la
/// invariante, así que hacer una oferta, alertar de una bajada de precio, avisar de un
/// recordatorio o generar el PDF de un contrato devolvían 500.
///
/// Estas pruebas fijan el formato y, sobre todo, que no dependa de ninguna cultura
/// instalada ni de la del hilo.
/// </remarks>
public class FcfaFormatTests
{
    [Theory]
    [InlineData(8_900_000, "8.900.000")]
    [InlineData(13_500_000, "13.500.000")]
    [InlineData(950_000, "950.000")]
    [InlineData(0, "0")]
    public void ElSeparadorDeMillaresEsElPunto(decimal amount, string expected)
    {
        FcfaFormat.Amount(amount).Should().Be(expected);
    }

    [Fact]
    public void ElImporteConMonedaLlevaFcfaDetras()
    {
        FcfaFormat.WithCurrency(8_900_000).Should().Be("8.900.000 FCFA");
    }

    [Fact]
    public void ElKilometrajeSeFormateaIgual()
    {
        FcfaFormat.Kilometres(120_000).Should().Be("120.000 km");
    }

    [Fact]
    public void ElFormatoNoDebeDependerDeLaCulturaDelHilo()
    {
        // En una máquina inglesa la coma sería el separador; el importe debe salir
        // igual en cualquier sitio.
        var original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var conInvariante = FcfaFormat.Amount(8_900_000);

            CultureInfo.CurrentCulture = original;

            conInvariante.Should().Be("8.900.000");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
