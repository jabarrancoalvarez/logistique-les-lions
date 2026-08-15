using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.CompareVehicles;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

public class CompareVehiclesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CompareVehiclesQueryHandler _handler;

    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _clim = Guid.NewGuid();

    public CompareVehiclesQueryHandlerTests()
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
        _context.VehicleEquipments.Add(new VehicleEquipment
        {
            Id = _clim, Code = "CLIMATISATION", Name = "Climatisation", IsActive = true
        });
        _context.SaveChanges();

        _handler = new CompareVehiclesQueryHandler(_context, new PriceIndicatorService(_context));
    }

    private Guid AddVehicle(decimal price, VehicleStatus status = VehicleStatus.Actif,
                            bool withClim = false, int? mileage = 100_000)
    {
        var id = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = id,
            PublicReference = $"YU{Random.Shared.Next(10000, 99999)}",
            Slug = id.ToString("N"),
            Title = "Toyota",
            MakeId = _makeId,
            Year = 2019,
            Mileage = mileage,
            Price = price,
            SellerId = Guid.NewGuid(),
            Status = status,
            CustomsStatus = CustomsStatus.Dedouane
        };
        if (withClim) vehicle.Equipments.Add(new VehicleEquipmentLink { EquipmentId = _clim });

        _context.Vehicles.Add(vehicle);
        _context.SaveChanges();
        return id;
    }

    private async Task<List<VehicleComparisonDto>> Compare(params Guid[] ids)
    {
        var result = await _handler.Handle(new CompareVehiclesQuery(ids), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    [Fact]
    public async Task DeberiaRespetarElOrdenDeSeleccion()
    {
        var a = AddVehicle(8_000_000m);
        var b = AddVehicle(9_000_000m);
        var c = AddVehicle(7_000_000m);

        var items = await Compare(c, a, b);

        items.Select(i => i.Id).Should().ContainInOrder(c, a, b);
    }

    [Fact]
    public async Task NoDeberiaAdmitirMasDeTresVehiculos()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => AddVehicle(8_000_000m)).ToArray();

        var items = await Compare(ids);

        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeberiaIgnorarIdentificadoresRepetidos()
    {
        var a = AddVehicle(8_000_000m);

        var items = await Compare(a, a, a);

        items.Should().ContainSingle();
    }

    [Fact]
    public async Task UnVehiculoVendidoDebeSeguirApareciendo()
    {
        // La especificación pide conservar la referencia de lo que se estaba comparando.
        var vendu = AddVehicle(8_000_000m, status: VehicleStatus.Vendu);

        var items = await Compare(vendu);

        items.Should().ContainSingle();
        items[0].Status.Should().Be(VehicleStatus.Vendu);
    }

    [Fact]
    public async Task UnBorradorNoDebeAparecer()
    {
        var brouillon = AddVehicle(8_000_000m, status: VehicleStatus.Brouillon);

        (await Compare(brouillon)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeberiaDevolverElEquipamientoDeclarado()
    {
        var conClim = AddVehicle(8_000_000m, withClim: true);
        var sinClim = AddVehicle(9_000_000m);

        var items = await Compare(conClim, sinClim);

        items[0].EquipmentCodes.Should().BeEquivalentTo(["CLIMATISATION"]);
        items[1].EquipmentCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task LosCamposSinDeclararDebenLlegarComoNull()
    {
        // El frontend los muestra como «Non renseigné»; nunca se infieren.
        var id = AddVehicle(8_000_000m, mileage: null);

        var items = await Compare(id);

        items[0].Mileage.Should().BeNull();
        items[0].PowerCv.Should().BeNull();
        items[0].Color.Should().BeNull();
    }

    [Fact]
    public async Task SinIdentificadoresDebeDevolverListaVacia()
    {
        (await Compare()).Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
