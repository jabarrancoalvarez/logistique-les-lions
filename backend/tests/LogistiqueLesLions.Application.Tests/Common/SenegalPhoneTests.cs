using FluentAssertions;
using LogistiqueLesLions.Application.Common;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Common;

/// <summary>
/// El teléfono es el identificador único de la cuenta: la normalización tiene que
/// llevar todas las formas de escribir un mismo número al mismo valor canónico, o el
/// índice UNIQUE dejaría entrar duplicados.
/// </summary>
public class SenegalPhoneTests
{
    [Theory]
    [InlineData("+221771234567")]
    [InlineData("+221 77 123 45 67")]
    [InlineData("00221771234567")]
    [InlineData("221771234567")]
    [InlineData("771234567")]
    [InlineData("77 123 45 67")]
    [InlineData("77-123-45-67")]
    [InlineData("77.123.45.67")]
    [InlineData("0771234567")]
    public void Normalize_DeberiaDevolverElMismoNumeroCanonico(string input)
    {
        SenegalPhone.Normalize(input).Should().Be("+221771234567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("77123456")]        // 8 dígitos: demasiado corto
    [InlineData("7712345678")]      // 10 dígitos: demasiado largo
    [InlineData("+34612345678")]    // otro país
    [InlineData("téléphone")]
    public void Normalize_DeberiaDevolverNull_SiNoEsUnNumeroSenegalesValido(string? input)
    {
        SenegalPhone.Normalize(input).Should().BeNull();
    }

    [Fact]
    public void IsValid_DeberiaReflejarElResultadoDeNormalize()
    {
        SenegalPhone.IsValid("77 123 45 67").Should().BeTrue();
        SenegalPhone.IsValid("+34612345678").Should().BeFalse();
    }

    [Fact]
    public void Format_DeberiaDevolverElNumeroLegible()
    {
        SenegalPhone.Format("+221771234567").Should().Be("+221 77 123 45 67");
    }

    [Fact]
    public void Format_DeberiaDevolverLaEntradaSinCambios_SiNoEstaNormalizada()
    {
        SenegalPhone.Format("77 123 45 67").Should().Be("77 123 45 67");
    }
}
