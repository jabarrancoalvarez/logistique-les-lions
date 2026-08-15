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
/// Alertas de bajada de precio de Favoris. Verifica sobre todo la precedencia entre el
/// interruptor general del usuario y el de cada favorito.
/// </summary>
public class PriceDropAlertServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PriceDropAlertService _service;

    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public PriceDropAlertServiceTests()
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
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId,
            PublicReference = "YU10001",
            Slug = "toyota-hilux-yu10001",
            Title = "Toyota Hilux",
            MakeId = _makeId,
            Year = 2019,
            Price = 8_900_000m,
            SellerId = Guid.NewGuid(),
            Status = VehicleStatus.Actif
        });
        _context.SaveChanges();

        _service = new PriceDropAlertService(_context);
    }

    /// <summary>Crea un usuario que sigue el vehículo, con la configuración indicada.</summary>
    private Guid AddFollower(bool allEnabled, bool perVehicleEnabled)
    {
        var userId = Guid.NewGuid();
        _context.UserProfiles.Add(new UserProfile
        {
            Id = userId,
            DisplayName = "Suiveur",
            Phone = $"+22177{Random.Shared.Next(1000000, 9999999)}",
            PasswordHash = "x",
            FavoriteAlertsAllEnabled = allEnabled
        });
        _context.SavedVehicles.Add(new SavedVehicle
        {
            UserId = userId,
            VehicleId = _vehicleId,
            PriceWhenSaved = 9_500_000m,
            PriceAlertEnabled = perVehicleEnabled
        });
        _context.SaveChanges();
        return userId;
    }

    private Task<int> NotificationsFor(Guid userId) =>
        _context.UserNotifications.CountAsync(n => n.UserId == userId);

    [Fact]
    public async Task DeberiaNotificarCuandoElInterruptorGeneralEstaActivo()
    {
        var user = AddFollower(allEnabled: true, perVehicleEnabled: true);

        var sent = await _service.NotifyPriceDropAsync(_vehicleId, 9_500_000m, 8_900_000m);

        sent.Should().Be(1);
        (await NotificationsFor(user)).Should().Be(1);
    }

    [Fact]
    public async Task ElInterruptorGeneralDebePrevalecerSobreElIndividual()
    {
        // Aunque el favorito esté silenciado, el interruptor general activo manda.
        var user = AddFollower(allEnabled: true, perVehicleEnabled: false);

        await _service.NotifyPriceDropAsync(_vehicleId, 9_500_000m, 8_900_000m);

        (await NotificationsFor(user)).Should().Be(1);
    }

    [Fact]
    public async Task ConElGeneralApagado_SoloDebeNotificarLosFavoritosConAlerta()
    {
        var conAlerta = AddFollower(allEnabled: false, perVehicleEnabled: true);
        var sinAlerta = AddFollower(allEnabled: false, perVehicleEnabled: false);

        await _service.NotifyPriceDropAsync(_vehicleId, 9_500_000m, 8_900_000m);

        (await NotificationsFor(conAlerta)).Should().Be(1);
        (await NotificationsFor(sinAlerta)).Should().Be(0);
    }

    [Fact]
    public async Task NoDeberiaNotificarUnaSubidaDePrecio()
    {
        var user = AddFollower(allEnabled: true, perVehicleEnabled: true);

        var sent = await _service.NotifyPriceDropAsync(_vehicleId, 8_900_000m, 9_500_000m);

        sent.Should().Be(0);
        (await NotificationsFor(user)).Should().Be(0);
    }

    [Fact]
    public async Task NoDeberiaNotificarDosVecesElMismoPrecio()
    {
        var user = AddFollower(allEnabled: true, perVehicleEnabled: true);

        await _service.NotifyPriceDropAsync(_vehicleId, 9_500_000m, 8_900_000m);
        await _service.NotifyPriceDropAsync(_vehicleId, 9_500_000m, 8_900_000m);

        (await NotificationsFor(user)).Should().Be(1);
    }

    [Fact]
    public async Task DeberiaNotificarDeNuevoSiElPrecioVuelveABajar()
    {
        var user = AddFollower(allEnabled: true, perVehicleEnabled: true);

        await _service.NotifyPriceDropAsync(_vehicleId, 9_500_000m, 8_900_000m);
        await _service.NotifyPriceDropAsync(_vehicleId, 8_900_000m, 8_200_000m);

        (await NotificationsFor(user)).Should().Be(2);
    }

    [Fact]
    public async Task LaNotificacionDebeEnlazarAlAnuncioYCitarAmbosPrecios()
    {
        var user = AddFollower(allEnabled: true, perVehicleEnabled: true);

        await _service.NotifyPriceDropAsync(_vehicleId, 9_500_000m, 8_900_000m);

        var notification = await _context.UserNotifications.SingleAsync(n => n.UserId == user);
        notification.Title.Should().Be("Baisse de prix");
        notification.Body.Should().Contain("9.500.000").And.Contain("8.900.000");
        notification.Link.Should().Be("/vehiculos/toyota-hilux-yu10001");
    }

    public void Dispose() => _context.Dispose();
}
