using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

/// <summary>
/// Indicador de precio: estadístico, sin IA, y con la mediana como referencia.
/// </summary>
/// <remarks>
/// No tenía ninguna prueba. Se escriben al acotar la consulta que trae los comparables,
/// porque ese acotado es un superconjunto del filtro que decide de verdad y hay que poder
/// demostrar que no deja fuera nada que contara: distinto modelo, año en el borde de la
/// franja y anuncios sin modelo, que se comparan entre sí.
/// </remarks>
public class PriceIndicatorTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PriceIndicatorService _service;

    private readonly Guid _toyota = Guid.NewGuid();
    private readonly Guid _renault = Guid.NewGuid();
    private readonly Guid _corolla = Guid.NewGuid();
    private readonly Guid _rav4 = Guid.NewGuid();
    private readonly Guid _vendedor = Guid.NewGuid();

    public PriceIndicatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        _context = new ApplicationDbContext(
            options,
            new Infrastructure.Persistence.Interceptors.AuditInterceptor(currentUser.Object),
            new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
                currentUser.Object, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        _context.VehicleMakes.AddRange(
            new VehicleMake { Id = _toyota, Name = "Toyota", Country = "JP" },
            new VehicleMake { Id = _renault, Name = "Renault", Country = "FR" });
        _context.VehicleModels.AddRange(
            new VehicleModel { Id = _corolla, MakeId = _toyota, Name = "Corolla" },
            new VehicleModel { Id = _rav4, MakeId = _toyota, Name = "RAV4" });
        _context.UserProfiles.Add(new UserProfile
        {
            Id = _vendedor, DisplayName = "Auto Dakar",
            Phone = "+221770000001", PasswordHash = "x"
        });
        // MinComparables por defecto es 5: con menos, el indicador no se muestra.
        _context.PriceIndicatorSettings.Add(new PriceIndicatorSettings());
        _context.SaveChanges();

        _service = new PriceIndicatorService(_context);
    }

    private Guid Anuncio(Guid makeId, Guid? modelId, int year, decimal price,
                        VehicleStatus status = VehicleStatus.Actif)
    {
        var id = Guid.NewGuid();
        _context.Vehicles.Add(new Vehicle
        {
            Id = id,
            PublicReference = "YU" + Guid.NewGuid().ToString("N")[..5],
            Slug = "anuncio-" + id.ToString("N")[..8],
            Title = "Anuncio de prueba",
            MakeId = makeId, ModelId = modelId, Year = year, Price = price,
            SellerId = _vendedor, Status = status,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        });
        _context.SaveChanges();
        return id;
    }

    /// <summary>Cinco Corolla de 2018 a 6.000.000, que es la mediana.</summary>
    private void CincoCorollaDe2018(decimal precio = 6_000_000m)
    {
        for (var i = 0; i < 5; i++) Anuncio(_toyota, _corolla, 2018, precio);
    }

    [Fact]
    public async Task DeberiaCalificarDeBuenaOportunidadElQueBajaDeLaMediana()
    {
        CincoCorollaDe2018();
        var barato = Anuncio(_toyota, _corolla, 2018, 4_000_000m);

        var r = await _service.CalculateAsync(barato);

        r.Indicator.Should().Be(PriceIndicator.BonneAffaire);
        r.ReferencePrice.Should().Be(6_000_000m);
        r.ComparablesCount.Should().Be(5);
    }

    [Fact]
    public async Task NoDeberiaContarseASiMismoComoComparable()
    {
        // Cuatro comparables más el propio anuncio: sin excluirse serían cinco y saldría
        // indicador; excluyéndose se queda en cuatro y no debe salir.
        for (var i = 0; i < 4; i++) Anuncio(_toyota, _corolla, 2018, 6_000_000m);
        var objetivo = Anuncio(_toyota, _corolla, 2018, 6_000_000m);

        var r = await _service.CalculateAsync(objetivo);

        r.ComparablesCount.Should().Be(4);
        r.Indicator.Should().BeNull("con menos comparables de los exigidos no se muestra nada");
    }

    [Fact]
    public async Task NoDeberiaMezclarModelosDistintosDeLaMismaMarca()
    {
        CincoCorollaDe2018();                       // Corolla, no deben contar
        var rav4 = Anuncio(_toyota, _rav4, 2018, 12_000_000m);

        var r = await _service.CalculateAsync(rav4);

        r.ComparablesCount.Should().Be(0);
        r.Indicator.Should().BeNull();
    }

    [Fact]
    public async Task DeberiaAceptarLosAniosDelBordeDeLaFranjaYRechazarElSiguiente()
    {
        // Franja por defecto: ±2 años. Con objetivo de 2018, entran 2016 y 2020.
        Anuncio(_toyota, _corolla, 2016, 6_000_000m);
        Anuncio(_toyota, _corolla, 2017, 6_000_000m);
        Anuncio(_toyota, _corolla, 2019, 6_000_000m);
        Anuncio(_toyota, _corolla, 2020, 6_000_000m);
        Anuncio(_toyota, _corolla, 2018, 6_000_000m);
        Anuncio(_toyota, _corolla, 2015, 1_000_000m);   // fuera por un año
        Anuncio(_toyota, _corolla, 2021, 1_000_000m);   // fuera por un año

        var objetivo = Anuncio(_toyota, _corolla, 2018, 6_000_000m);

        var r = await _service.CalculateAsync(objetivo);

        r.ComparablesCount.Should().Be(5, "los de 2015 y 2021 quedan fuera de la franja");
        r.ReferencePrice.Should().Be(6_000_000m, "los de fuera no arrastran la mediana");
    }

    [Fact]
    public async Task DeberiaCompararEntreSiLosAnunciosSinModelo()
    {
        for (var i = 0; i < 5; i++) Anuncio(_toyota, null, 2018, 6_000_000m);
        var objetivo = Anuncio(_toyota, null, 2018, 4_000_000m);

        var r = await _service.CalculateAsync(objetivo);

        r.ComparablesCount.Should().Be(5, "un anuncio sin modelo se compara con los que tampoco lo tienen");
        r.Indicator.Should().Be(PriceIndicator.BonneAffaire);
    }

    [Fact]
    public async Task NoDeberiaComparaUnAnuncioSinModeloConLosQueSiLoTienen()
    {
        CincoCorollaDe2018();
        var sinModelo = Anuncio(_toyota, null, 2018, 6_000_000m);

        var r = await _service.CalculateAsync(sinModelo);

        r.ComparablesCount.Should().Be(0);
    }

    [Fact]
    public async Task NoDeberiaContarLosAnunciosQueYaNoEstanALaVenta()
    {
        for (var i = 0; i < 5; i++)
            Anuncio(_toyota, _corolla, 2018, 6_000_000m, VehicleStatus.Vendu);
        var objetivo = Anuncio(_toyota, _corolla, 2018, 4_000_000m);

        var r = await _service.CalculateAsync(objetivo);

        r.ComparablesCount.Should().Be(0, "un anuncio vendido ya no es oferta de mercado");
    }

    [Fact]
    public async Task DeberiaResolverVariasTarjetasDeMarcasDistintasEnUnaSolaLlamada()
    {
        CincoCorollaDe2018();
        var corolla = Anuncio(_toyota, _corolla, 2018, 4_000_000m);

        for (var i = 0; i < 5; i++) Anuncio(_renault, null, 2019, 3_000_000m);
        var renault = Anuncio(_renault, null, 2019, 3_000_000m);

        var r = await _service.CalculateManyAsync([corolla, renault]);

        r.Should().HaveCount(2);
        r[corolla].Indicator.Should().Be(PriceIndicator.BonneAffaire);
        r[corolla].ReferencePrice.Should().Be(6_000_000m);
        r[renault].Indicator.Should().Be(PriceIndicator.PrixCorrect);
        r[renault].ReferencePrice.Should().Be(3_000_000m);
    }

    public void Dispose() => _context.Dispose();
}
