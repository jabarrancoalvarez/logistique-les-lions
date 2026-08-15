using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

public class UpdateVehicleCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UpdateVehicleCommandHandler _handler;
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();
    private readonly Guid _equipmentId = Guid.NewGuid();

    public UpdateVehicleCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var auditInterceptor = new Infrastructure.Persistence.Interceptors.AuditInterceptor(mockCurrentUser.Object);
        var auditLogInterceptor = new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
            mockCurrentUser.Object,
            new Microsoft.AspNetCore.Http.HttpContextAccessor());

        _context = new ApplicationDbContext(options, auditInterceptor, auditLogInterceptor);

        _context.VehicleMakes.Add(new VehicleMake { Id = _makeId, Name = "Toyota", Country = "JP" });
        _context.VehicleEquipments.Add(new VehicleEquipment
        {
            Id = _equipmentId, Code = "BLUETOOTH", Name = "Bluetooth", IsActive = true
        });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId,
            PublicReference = "YU10000",
            Slug = "toyota-rav4-2019-yu10000",
            Title = "Toyota RAV4 2019",
            MakeId = _makeId,
            Year = 2019,
            Price = 9_500_000m,
            SellerId = _ownerId,
            Status = VehicleStatus.Actif,
            CustomsStatus = CustomsStatus.Dedouane
        });
        _context.SaveChanges();

        // Servicio real: así la edición ejercita también el envío de alertas de bajada.
        _handler = new UpdateVehicleCommandHandler(
            _context, new Application.Services.PriceDropAlertService(_context));
    }

    private UpdateVehicleCommand Command(decimal price = 9_500_000m, Guid? requesterId = null) =>
        new(
            Id: _vehicleId,
            Title: "Toyota RAV4 2019",
            Description: null,
            MakeId: _makeId,
            ModelId: null,
            Version: null,
            Year: 2019,
            Mileage: 126000,
            Condition: VehicleCondition.Used,
            BodyType: BodyType.Suv,
            FuelType: FuelType.Essence,
            Transmission: TransmissionType.Automatique,
            Color: null,
            Doors: 5,
            Seats: 5,
            Vin: null,
            PowerCv: null,
            EngineDisplacementCc: null,
            Drivetrain: null,
            EngineName: null,
            CustomsStatus: CustomsStatus.Dedouane,
            Price: price,
            PriceNegotiable: true,
            Region: "DK",
            City: "Dakar",
            District: null,
            EquipmentIds: [],
            RequesterId: requesterId ?? _ownerId);

    [Fact]
    public async Task Handle_DeberiaRegistrarElHistoricoSoloCuandoElPrecioCambia()
    {
        await _handler.Handle(Command(price: 8_900_000m), CancellationToken.None);

        var history = await _context.VehiclePriceHistories.ToListAsync();
        history.Should().ContainSingle();
        history[0].Price.Should().Be(8_900_000m);
    }

    [Fact]
    public async Task Handle_NoDeberiaRegistrarNadaSiElPrecioNoCambia()
    {
        await _handler.Handle(Command(price: 9_500_000m), CancellationToken.None);

        (await _context.VehiclePriceHistories.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_DeberiaRechazarAQuienNoEsElPropietario()
    {
        var result = await _handler.Handle(
            Command(price: 1m, requesterId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Vehicle.NotOwner");

        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Price.Should().Be(9_500_000m);
    }

    [Fact]
    public async Task Handle_DeberiaSincronizarElEquipamiento()
    {
        await _handler.Handle(
            Command() with { EquipmentIds = [_equipmentId] }, CancellationToken.None);

        (await _context.VehicleEquipmentLinks.CountAsync()).Should().Be(1);

        // Al quitarlo de la selección, el vínculo desaparece.
        await _handler.Handle(Command() with { EquipmentIds = [] }, CancellationToken.None);

        (await _context.VehicleEquipmentLinks.CountAsync()).Should().Be(0);
    }

    public void Dispose() => _context.Dispose();
}
