using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Services;

/// <summary>
/// Indicador estadístico de precio. La regla más importante que se verifica aquí es la
/// negativa: sin suficientes comparables no debe devolverse ningún indicador.
/// </summary>
public class PriceIndicatorServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PriceIndicatorService _service;

    private readonly Guid _toyota = Guid.NewGuid();
    private readonly Guid _rav4 = Guid.NewGuid();
    private readonly Guid _seller = Guid.NewGuid();

    public PriceIndicatorServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        _context = new ApplicationDbContext(
            options,
            new Infrastructure.Persistence.Interceptors.AuditInterceptor(mockCurrentUser.Object),
            new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
                mockCurrentUser.Object, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        _context.VehicleMakes.Add(new VehicleMake { Id = _toyota, Name = "Toyota", Country = "JP" });
        _context.VehicleModels.Add(new VehicleModel { Id = _rav4, MakeId = _toyota, Name = "RAV4" });
        _context.PriceIndicatorSettings.Add(new PriceIndicatorSettings
        {
            Id = PriceIndicatorSettings.SingletonId,
            MinComparables = 5,
            MaxListingAgeDays = 180,
            YearBand = 2,
            GoodDealMargin = 0.10m,
            HighPriceMargin = 0.10m
        });
        _context.SaveChanges();

        _service = new PriceIndicatorService(_context);
    }

    private Guid AddVehicle(decimal price, int year = 2019, Guid? modelId = null,
                            VehicleStatus status = VehicleStatus.Actif, int ageDays = 10)
    {
        var id = Guid.NewGuid();
        _context.Vehicles.Add(new Vehicle
        {
            Id = id,
            PublicReference = $"YU{Random.Shared.Next(10000, 99999)}",
            Slug = id.ToString("N"),
            Title = "Toyota RAV4",
            MakeId = _toyota,
            ModelId = modelId ?? _rav4,
            Year = year,
            Price = price,
            SellerId = _seller,
            Status = status,
            // La antigüedad se mide desde PublishedAt: CreatedAt lo fija el
            // AuditInterceptor y no puede establecerse desde el test.
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-ageDays)
        });
        _context.SaveChanges();
        return id;
    }

    /// <summary>Rellena la muestra con precios en torno a una mediana de 10.000.000.</summary>
    private void AddComparablePool()
    {
        foreach (var price in new[] { 8_000_000m, 9_000_000m, 10_000_000m, 11_000_000m, 12_000_000m })
            AddVehicle(price);
    }

    [Fact]
    public async Task SinSuficientesComparables_NoDebeDevolverIndicador()
    {
        var target = AddVehicle(9_000_000m);
        AddVehicle(10_000_000m);
        AddVehicle(11_000_000m);

        var result = await _service.CalculateAsync(target);

        result.Indicator.Should().BeNull();
        result.ReferencePrice.Should().BeNull();
        result.ComparablesCount.Should().Be(2);
        // Se informa de cuántos había, para poder explicar por qué no hay indicador.
    }

    [Fact]
    public async Task PorDebajoDelMargen_DebeSerBonneAffaire()
    {
        AddComparablePool();
        // Mediana de la muestra = 10.000.000; el umbral inferior queda en 9.000.000.
        var target = AddVehicle(7_500_000m);

        var result = await _service.CalculateAsync(target);

        result.Indicator.Should().Be(PriceIndicator.BonneAffaire);
        result.ReferencePrice.Should().Be(10_000_000m);
    }

    [Fact]
    public async Task DentroDelMargen_DebeSerPrixCorrect()
    {
        AddComparablePool();
        var target = AddVehicle(10_200_000m);

        var result = await _service.CalculateAsync(target);

        result.Indicator.Should().Be(PriceIndicator.PrixCorrect);
    }

    [Fact]
    public async Task PorEncimaDelMargen_DebeSerPrixEleve()
    {
        AddComparablePool();
        var target = AddVehicle(14_000_000m);

        var result = await _service.CalculateAsync(target);

        result.Indicator.Should().Be(PriceIndicator.PrixEleve);
    }

    [Fact]
    public async Task NoDebeContarseASiMismoComoComparable()
    {
        AddComparablePool();
        var target = AddVehicle(10_000_000m);

        var result = await _service.CalculateAsync(target);

        // Hay 6 anuncios en total, pero solo 5 son comparables del objetivo.
        result.ComparablesCount.Should().Be(5);
    }

    [Fact]
    public async Task NoDebeUsarAnunciosNoPublicados()
    {
        AddComparablePool();
        // Cinco borradores más: no deben contar, así que la muestra sigue siendo válida
        // pero la mediana no debe desplazarse hacia esos precios disparatados.
        for (var i = 0; i < 5; i++)
            AddVehicle(50_000_000m, status: VehicleStatus.Brouillon);

        var target = AddVehicle(10_000_000m);
        var result = await _service.CalculateAsync(target);

        result.ComparablesCount.Should().Be(5);
        result.ReferencePrice.Should().Be(10_000_000m);
    }

    [Fact]
    public async Task NoDebeUsarAnunciosDemasiadoAntiguos()
    {
        foreach (var price in new[] { 8_000_000m, 9_000_000m, 10_000_000m, 11_000_000m, 12_000_000m })
            AddVehicle(price, ageDays: 400);

        var target = AddVehicle(9_000_000m);
        var result = await _service.CalculateAsync(target);

        result.Indicator.Should().BeNull();
    }

    [Fact]
    public async Task NoDebeCompararConOtroModelo()
    {
        var otherModel = Guid.NewGuid();
        _context.VehicleModels.Add(new VehicleModel { Id = otherModel, MakeId = _toyota, Name = "Hilux" });
        _context.SaveChanges();

        for (var i = 0; i < 6; i++)
            AddVehicle(20_000_000m, modelId: otherModel);

        var target = AddVehicle(9_000_000m);
        var result = await _service.CalculateAsync(target);

        result.Indicator.Should().BeNull();
    }

    [Fact]
    public async Task NoDebeCompararConAnyosDemasiadoLejanos()
    {
        foreach (var price in new[] { 8_000_000m, 9_000_000m, 10_000_000m, 11_000_000m, 12_000_000m })
            AddVehicle(price, year: 2010);

        var target = AddVehicle(9_000_000m, year: 2019);
        var result = await _service.CalculateAsync(target);

        result.Indicator.Should().BeNull();
    }

    [Fact]
    public async Task LaMedianaNoDebeDesplazarsePorUnPrecioAtipico()
    {
        AddComparablePool();
        // Un anuncio con un precio disparatado movería la media, pero no la mediana.
        AddVehicle(500_000_000m);

        var target = AddVehicle(10_000_000m);
        var result = await _service.CalculateAsync(target);

        result.ReferencePrice.Should().Be(10_500_000m);
        result.Indicator.Should().Be(PriceIndicator.PrixCorrect);
    }

    [Fact]
    public async Task CalculateMany_DebeResolverVariosDeUnaVez()
    {
        AddComparablePool();
        var cheap = AddVehicle(7_000_000m);
        var expensive = AddVehicle(15_000_000m);

        var results = await _service.CalculateManyAsync([cheap, expensive]);

        results[cheap].Indicator.Should().Be(PriceIndicator.BonneAffaire);
        results[expensive].Indicator.Should().Be(PriceIndicator.PrixEleve);
    }

    public void Dispose() => _context.Dispose();
}
