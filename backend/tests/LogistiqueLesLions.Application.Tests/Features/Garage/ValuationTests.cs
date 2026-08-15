using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Garage;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Garage;

/// <summary>
/// Valeur estimée y évolution de la valeur.
/// </summary>
/// <remarks>
/// La regla que gobierna esta parte es que <b>nunca se inventa una valoración</b>: sin
/// muestra suficiente no se devuelve ninguna cifra. Y todo es estadística: ni IA, ni
/// predicciones a futuro.
/// </remarks>
public class ValuationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly VehicleValuationService _service;
    private readonly GetVehicleValuationQueryHandler _query;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _modelId = Guid.NewGuid();
    private readonly Guid _otherModelId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public ValuationTests()
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

        _context.VehicleMakes.Add(new VehicleMake { Id = _makeId, Name = "Toyota", Country = "JP" });
        _context.VehicleModels.AddRange(
            new VehicleModel { Id = _modelId, MakeId = _makeId, Name = "RAV4" },
            new VehicleModel { Id = _otherModelId, MakeId = _makeId, Name = "Corolla" });
        _context.UserProfiles.AddRange(
            new UserProfile { Id = _userId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x", Region = "DK" },
            new UserProfile { Id = _otherUserId, DisplayName = "Fatou", Phone = "+221770000002", PasswordHash = "x" },
            new UserProfile { Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000003", PasswordHash = "x" });
        _context.GarageVehicles.Add(new GarageVehicle
        {
            Id = _vehicleId, UserId = _userId, MakeId = _makeId, ModelId = _modelId,
            Year = 2019, Mileage = 147_500,
            FuelType = FuelType.Diesel, Transmission = TransmissionType.Automatique
        });
        _context.SaveChanges();

        _service = new VehicleValuationService(_context);
        _query   = new GetVehicleValuationQueryHandler(_context, _service);
    }

    /// <summary>Publica un anuncio que sirve de comparable.</summary>
    private void Listing(
        decimal price,
        int year = 2019,
        int? mileage = 145_000,
        Guid? modelId = null,
        FuelType? fuel = FuelType.Diesel,
        TransmissionType? gearbox = TransmissionType.Automatique,
        string? region = "DK",
        VehicleStatus status = VehicleStatus.Actif)
    {
        var id = Guid.NewGuid();
        _context.Vehicles.Add(new Vehicle
        {
            Id = id,
            PublicReference = $"YU{Guid.NewGuid().ToString()[..5]}",
            Slug = $"annonce-{id}",
            Title = "Toyota RAV4",
            MakeId = _makeId,
            ModelId = modelId ?? _modelId,
            Year = year,
            Mileage = mileage,
            Price = price,
            FuelType = fuel,
            Transmission = gearbox,
            Region = region,
            SellerId = _sellerId,
            Status = status,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-10)
        });
    }

    private void Listings(params decimal[] prices)
    {
        foreach (var price in prices) Listing(price);
        _context.SaveChanges();
    }

    // ─── Muestra mínima ────────────────────────────────────────────────────

    [Fact]
    public async Task SinComparablesSuficientesNoDebeDarNingunaCifra()
    {
        // Cuatro anuncios, y el mínimo son cinco.
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m);

        var result = await _service.EstimateAsync(_vehicleId);

        result.HasEstimate.Should().BeFalse();
        result.EstimatedValue.Should().BeNull();
        result.LowValue.Should().BeNull();
        result.HighValue.Should().BeNull();
    }

    [Fact]
    public async Task SinModeloNoHayNadaConQueComparar()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        var vehicle = await _context.GarageVehicles.SingleAsync();
        vehicle.ModelId = null;
        await _context.SaveChangesAsync();

        var result = await _service.EstimateAsync(_vehicleId);

        result.HasEstimate.Should().BeFalse();
    }

    [Fact]
    public async Task ConMuestraSuficienteDebeDevolverUnaHorquillaAlrededorDeLaMediana()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        var result = await _service.EstimateAsync(_vehicleId);

        result.HasEstimate.Should().BeTrue();
        result.ComparableCount.Should().Be(5);
        result.EstimatedValue.Should().Be(8_400_000m);
        // ±5 %
        result.LowValue.Should().Be(7_980_000m);
        result.HighValue.Should().Be(8_820_000m);
    }

    [Fact]
    public async Task DebeUsarLaMedianaYNoLaMedia()
    {
        // Un anuncio disparatado desplazaría la media y daría una horquilla irreal.
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 40_000_000m);

        var result = await _service.EstimateAsync(_vehicleId);

        result.EstimatedValue.Should().Be(8_400_000m);
    }

    // ─── Qué entra en la muestra ───────────────────────────────────────────

    [Fact]
    public async Task OtroModeloNoDebeContarComoComparable()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m);
        Listing(3_000_000m, modelId: _otherModelId);
        await _context.SaveChangesAsync();

        var result = await _service.EstimateAsync(_vehicleId);

        // Sigue habiendo solo cuatro comparables de verdad.
        result.HasEstimate.Should().BeFalse();
    }

    [Fact]
    public async Task UnAnoDemasiadoLejanoNoDebeContar()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m);
        Listing(4_000_000m, year: 2010);
        await _context.SaveChangesAsync();

        (await _service.EstimateAsync(_vehicleId)).HasEstimate.Should().BeFalse();
    }

    [Fact]
    public async Task LosAnunciosYaVendidosTambienDicenCuantoValeElCoche()
    {
        Listing(8_000_000m, status: VehicleStatus.Vendu);
        Listing(8_200_000m, status: VehicleStatus.Vendu);
        Listing(8_400_000m);
        Listing(8_600_000m);
        Listing(8_800_000m);
        await _context.SaveChangesAsync();

        var result = await _service.EstimateAsync(_vehicleId);

        result.HasEstimate.Should().BeTrue();
        result.ComparableCount.Should().Be(5);
    }

    [Fact]
    public async Task UnBorradorNoDebeContarComoReferenciaDeMercado()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m);
        Listing(1_000_000m, status: VehicleStatus.Brouillon);
        await _context.SaveChangesAsync();

        (await _service.EstimateAsync(_vehicleId)).HasEstimate.Should().BeFalse();
    }

    [Fact]
    public async Task UnAnuncioDemasiadoAntiguoNoDebeContar()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m);

        var old = new Vehicle
        {
            Id = Guid.NewGuid(), PublicReference = "YU99999", Slug = "vieux", Title = "Vieux",
            MakeId = _makeId, ModelId = _modelId, Year = 2019, Mileage = 145_000,
            Price = 5_000_000m, SellerId = _sellerId, Status = VehicleStatus.Actif,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-500)
        };
        _context.Vehicles.Add(old);
        await _context.SaveChangesAsync();

        (await _service.EstimateAsync(_vehicleId)).HasEstimate.Should().BeFalse();
    }

    // ─── Criterios progresivos ─────────────────────────────────────────────

    [Fact]
    public async Task DebeEmpezarPorLaMuestraMasParecida()
    {
        // Cinco iguales en todo: no hace falta soltar ningún criterio.
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        var result = await _service.EstimateAsync(_vehicleId);

        result.Criteria.Should().HaveFlag(ValuationCriteria.Region);
        result.Criteria.Should().HaveFlag(ValuationCriteria.FuelAndTransmission);
        result.Criteria.Should().HaveFlag(ValuationCriteria.Mileage);
    }

    [Fact]
    public async Task DebeSoltarCriteriosHastaReunirLaMuestraMinima()
    {
        // Solo dos coinciden en región; el resto están en otras regiones.
        Listing(8_000_000m, region: "DK");
        Listing(8_200_000m, region: "DK");
        Listing(8_400_000m, region: "TH");
        Listing(8_600_000m, region: "SL");
        Listing(8_800_000m, region: "ZG");
        await _context.SaveChangesAsync();

        var result = await _service.EstimateAsync(_vehicleId);

        result.HasEstimate.Should().BeTrue();
        result.ComparableCount.Should().Be(5);
        // La región se ha soltado; lo demás se mantiene.
        result.Criteria.Should().NotHaveFlag(ValuationCriteria.Region);
        result.Criteria.Should().HaveFlag(ValuationCriteria.FuelAndTransmission);
    }

    [Fact]
    public async Task NuncaDebeBajarDeMarcaModeloYAno()
    {
        // Ni un solo comparable con esos criterios: no hay estimación posible.
        Listing(8_000_000m, year: 2005);
        Listing(8_200_000m, modelId: _otherModelId);
        await _context.SaveChangesAsync();

        (await _service.EstimateAsync(_vehicleId)).HasEstimate.Should().BeFalse();
    }

    // ─── Evolución ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LaPrimeraConsultaDebeGuardarLaEstimacionDelDia()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        await _query.Handle(new GetVehicleValuationQuery(_userId, _vehicleId), CancellationToken.None);

        var snapshot = await _context.VehicleValuationSnapshots.SingleAsync();
        snapshot.EstimatedValue.Should().Be(8_400_000m);
        snapshot.Mileage.Should().Be(147_500);
    }

    [Fact]
    public async Task NoDebeGuardarDosInstantaneasElMismoDia()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        await _query.Handle(new GetVehicleValuationQuery(_userId, _vehicleId), CancellationToken.None);
        await _query.Handle(new GetVehicleValuationQuery(_userId, _vehicleId), CancellationToken.None);

        (await _context.VehicleValuationSnapshots.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ConUnSoloPuntoNoHayEvolucionQueMostrar()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        var result = await _query.Handle(
            new GetVehicleValuationQuery(_userId, _vehicleId), CancellationToken.None);

        result.Value!.Evolution.Should().BeNull();
    }

    [Fact]
    public async Task DeberiaCalcularLaDiferenciaDeLosUltimosMeses()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        // Historial ya existente: en enero valía más.
        _context.VehicleValuationSnapshots.Add(new VehicleValuationSnapshot
        {
            GarageVehicleId = _vehicleId,
            EstimatedValue = 8_850_000m, LowValue = 8_400_000m, HighValue = 9_300_000m,
            ComparableCount = 6,
            CapturedAt = DateTimeOffset.UtcNow.AddMonths(-4)
        });
        await _context.SaveChangesAsync();

        var result = await _query.Handle(
            new GetVehicleValuationQuery(_userId, _vehicleId), CancellationToken.None);

        var evolution = result.Value!.Evolution!;
        evolution.Points.Should().HaveCount(2);
        // 8.400.000 − 8.850.000
        evolution.ChangeAmount.Should().Be(-450_000m);
        evolution.ChangePercent.Should().Be(-5.1m);
    }

    [Fact]
    public async Task LaEvolucionNoDebeMirarMasAllaDeLaVentana()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        _context.VehicleValuationSnapshots.Add(new VehicleValuationSnapshot
        {
            GarageVehicleId = _vehicleId,
            EstimatedValue = 12_000_000m, LowValue = 11_000_000m, HighValue = 13_000_000m,
            ComparableCount = 6,
            CapturedAt = DateTimeOffset.UtcNow.AddMonths(-14)
        });
        await _context.SaveChangesAsync();

        var result = await _query.Handle(
            new GetVehicleValuationQuery(_userId, _vehicleId), CancellationToken.None);

        // La instantánea de hace más de un año queda fuera: solo hay un punto reciente.
        result.Value!.Evolution.Should().BeNull();
    }

    // ─── Resumen de Mon Garage ─────────────────────────────────────────────

    [Fact]
    public async Task ElResumenDebeSumarSoloLosVehiculosConEstimacion()
    {
        Listings(8_000_000m, 8_200_000m, 8_400_000m, 8_600_000m, 8_800_000m);

        // Un segundo vehículo sin comparables: no debe aportar nada a la suma.
        _context.GarageVehicles.Add(new GarageVehicle
        {
            UserId = _userId, MakeId = _makeId, ModelId = _otherModelId, Year = 2012
        });
        await _context.SaveChangesAsync();

        var garage = new GetMyGarageQueryHandler(_context, _service);
        var result = await garage.Handle(new GetMyGarageQuery(_userId), CancellationToken.None);

        result.Value!.VehicleCount.Should().Be(2);
        result.Value.TotalEstimatedValue.Should().Be(8_400_000m);

        var withEstimate = result.Value.Vehicles.Single(v => v.EstimatedValue is not null);
        withEstimate.EstimatedValue.Should().Be(8_400_000m);
    }

    // ─── Privacidad ────────────────────────────────────────────────────────

    [Fact]
    public async Task NadieMasDebeConsultarLaValoracion()
    {
        var result = await _query.Handle(
            new GetVehicleValuationQuery(_otherUserId, _vehicleId), CancellationToken.None);

        result.Error.Should().Be("GarageVehicle.AccessDenied");
    }

    public void Dispose() => _context.Dispose();
}
