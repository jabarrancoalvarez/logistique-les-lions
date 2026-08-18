using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

public class CreateVehicleCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreateVehicleCommandHandler _handler;
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _equipmentId = Guid.NewGuid();

    public CreateVehicleCommandHandlerTests()
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
            Id = _equipmentId, Code = "CLIMATISATION", Name = "Climatisation", IsActive = true
        });
        _context.SaveChanges();

        // La secuencia de PostgreSQL no existe en la base InMemory: se simula.
        var counter = 10000;
        var mockReferences = new Mock<IPublicReferenceGenerator>();
        mockReferences
            .Setup(r => r.NextVehicleReferenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"YU{counter++:D5}");

        // Servicio real: así la publicación ejercita también la alerta de nuevos
        // vehículos de las búsquedas guardadas.
        _handler = new CreateVehicleCommandHandler(
            _context,
            mockReferences.Object,
            new Application.Services.NewVehicleAlertService(_context));
    }

    private CreateVehicleCommand Command(
        bool publish = true,
        IReadOnlyList<Guid>? equipmentIds = null) =>
        new(
            Title: "Toyota RAV4 2019",
            Description: "Très bon état, entretien à jour.",
            MakeId: _makeId,
            ModelId: null,
            Version: "2.0 VVT-i",
            Year: 2019,
            Mileage: 126000,
            Condition: VehicleCondition.Used,
            BodyType: BodyType.Suv,
            FuelType: FuelType.Essence,
            Transmission: TransmissionType.Automatique,
            Color: "Gris",
            Doors: 5,
            Seats: 5,
            Vin: null,
            PowerCv: 152,
            EngineDisplacementCc: 1987,
            Drivetrain: Drivetrain.Integrale,
            EngineName: null,
            Price: 8_900_000m,
            PriceNegotiable: true,
            Region: "DK",
            City: "Dakar",
            District: null,
            EquipmentIds: equipmentIds ?? [],
            Publish: publish,
            SellerId: _sellerId);

    [Fact]
    public async Task Handle_DeberiaAsignarUnaReferenciaPublicaYUnSlugQueLaIncluye()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.PublicReference.Should().Be("YU10000");
        vehicle.Slug.Should().Be("toyota-2019-yu10000");
    }

    [Fact]
    public async Task Handle_DeberiaRegistrarElPrecioInicialEnElHistorico()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        var history = await _context.VehiclePriceHistories.ToListAsync();
        history.Should().ContainSingle();
        history[0].Price.Should().Be(8_900_000m);
    }

    [Fact]
    public async Task Handle_DeberiaPublicarComoActifCuandoSePideSuPublicacion()
    {
        await _handler.Handle(Command(publish: true), CancellationToken.None);

        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Status.Should().Be(VehicleStatus.Actif);
        vehicle.PublishedAt.Should().NotBeNull();
        vehicle.IsPubliclyVisible.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeberiaDejarloEnBrouillonSiNoSePublica()
    {
        await _handler.Handle(Command(publish: false), CancellationToken.None);

        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Status.Should().Be(VehicleStatus.Brouillon);
        vehicle.PublishedAt.Should().BeNull();
        vehicle.IsPubliclyVisible.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeberiaVincularElEquipamientoDelCatalogo()
    {
        await _handler.Handle(Command(equipmentIds: [_equipmentId]), CancellationToken.None);

        var links = await _context.VehicleEquipmentLinks.ToListAsync();
        links.Should().ContainSingle();
        links[0].EquipmentId.Should().Be(_equipmentId);
    }

    [Fact]
    public async Task Handle_DeberiaRechazarEquipamientoFueraDelCatalogo()
    {
        var result = await _handler.Handle(
            Command(equipmentIds: [Guid.NewGuid()]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Vehicle.UnknownEquipment");
        (await _context.Vehicles.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_DeberiaRechazarUnaMarcaInexistente()
    {
        var command = Command() with { MakeId = Guid.NewGuid() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Vehicle.MakeNotFound");
    }

    [Fact]
    public async Task Handle_UnVehiculoVenduNoDebeAdmitirNegociacion()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.AcceptsNegotiation.Should().BeTrue();

        vehicle.Status = VehicleStatus.Vendu;
        vehicle.AcceptsNegotiation.Should().BeFalse();
        // La ficha sigue existiendo por favoritos, comparaciones y contratos.
        vehicle.IsPubliclyVisible.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
