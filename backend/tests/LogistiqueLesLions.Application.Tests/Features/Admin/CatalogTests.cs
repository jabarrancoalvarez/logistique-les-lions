using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Configuration;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// Catálogos: marcas, modelos y equipamiento.
/// </summary>
/// <remarks>
/// Lo delicado de un catálogo no es darlo de alta, es lo que ya cuelga de él: por eso
/// aquí se prueba sobre todo que no se pueda duplicar, que el código de un equipamiento
/// no cambie una vez creado, y que retirar no borre.
/// </remarks>
public class CatalogTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetCatalogsQueryHandler _get;
    private readonly SaveCatalogMakeCommandHandler _saveMake;
    private readonly SaveCatalogModelCommandHandler _saveModel;
    private readonly SaveCatalogEquipmentCommandHandler _saveEquipment;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _toyotaId = Guid.NewGuid();
    private readonly Guid _corollaId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();

    public CatalogTests()
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

        _context.VehicleMakes.Add(new VehicleMake { Id = _toyotaId, Name = "Toyota" });
        _context.VehicleModels.Add(new VehicleModel
        {
            Id = _corollaId, MakeId = _toyotaId, Name = "Corolla"
        });
        _context.SaveChanges();

        _get           = new GetCatalogsQueryHandler(_context);
        _saveMake      = new SaveCatalogMakeCommandHandler(_context);
        _saveModel     = new SaveCatalogModelCommandHandler(_context);
        _saveEquipment = new SaveCatalogEquipmentCommandHandler(_context);
    }

    // ─── Marcas ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaPoderDarseDeAltaUnaMarca()
    {
        var result = await _saveMake.Handle(
            new SaveCatalogMakeCommand(_adminId, null, "Peugeot", "France", true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.VehicleMakes.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task NoDebeHaberDosMarcasConElMismoNombre()
    {
        // Ni escrito de otra manera: «toyota» y «Toyota» son la misma marca.
        var result = await _saveMake.Handle(
            new SaveCatalogMakeCommand(_adminId, null, "toyota", null, false),
            CancellationToken.None);

        result.Error.Should().Be("Catalog.MakeAlreadyExists");
    }

    [Fact]
    public async Task RenombrarUnaMarcaDebeDejarElNombreAnterior()
    {
        await _saveMake.Handle(
            new SaveCatalogMakeCommand(_adminId, _toyotaId, "Toyota Motor", null, true),
            CancellationToken.None);

        var action = await _context.AdminActions.SingleAsync();
        action.Type.Should().Be(AdminActionType.CatalogChanged);
        action.OldValue.Should().Be("Toyota");
        action.NewValue.Should().Be("Toyota Motor");
    }

    // ─── Modelos ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DosModelosIgualesDeMarcasDistintasSonLegitimos()
    {
        var otherMake = (await _saveMake.Handle(
            new SaveCatalogMakeCommand(_adminId, null, "Nissan", null, false),
            CancellationToken.None)).Value;

        var result = await _saveModel.Handle(
            new SaveCatalogModelCommand(_adminId, null, otherMake, "Corolla", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DosModelosIgualesDeLaMismaMarcaNo()
    {
        var result = await _saveModel.Handle(
            new SaveCatalogModelCommand(_adminId, null, _toyotaId, "Corolla", null),
            CancellationToken.None);

        result.Error.Should().Be("Catalog.ModelAlreadyExists");
    }

    [Fact]
    public async Task UnModeloDeUnaMarcaQueNoExisteNoDebeCrearse()
    {
        var result = await _saveModel.Handle(
            new SaveCatalogModelCommand(_adminId, null, Guid.NewGuid(), "Hilux", null),
            CancellationToken.None);

        result.Error.Should().Be("Catalog.MakeNotFound");
    }

    // ─── Equipamiento ──────────────────────────────────────────────────────

    [Fact]
    public async Task ElCodigoDeUnEquipamientoNoDebeCambiarNunca()
    {
        // Los anuncios ya enlazados dejarían de significar lo mismo.
        var id = (await _saveEquipment.Handle(
            new SaveCatalogEquipmentCommand(_adminId, null, "clim", "Climatisation", 1, true),
            CancellationToken.None)).Value;

        await _saveEquipment.Handle(
            new SaveCatalogEquipmentCommand(_adminId, id, "AUTRE", "Climatisation auto", 1, true),
            CancellationToken.None);

        var equipment = await _context.VehicleEquipments.SingleAsync();
        equipment.Code.Should().Be("CLIM");
        equipment.Name.Should().Be("Climatisation auto");
    }

    [Fact]
    public async Task RetirarUnEquipamientoNoDebeBorrarlo()
    {
        var id = (await _saveEquipment.Handle(
            new SaveCatalogEquipmentCommand(_adminId, null, "ISOFIX", "Isofix", 1, true),
            CancellationToken.None)).Value;

        await _saveEquipment.Handle(
            new SaveCatalogEquipmentCommand(_adminId, id, "ISOFIX", "Isofix", 1, false),
            CancellationToken.None);

        var equipment = await _context.VehicleEquipments.SingleAsync();
        equipment.IsActive.Should().BeFalse();
        equipment.DeletedAt.Should().BeNull();
    }

    // ─── Lectura ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ElCatalogoDebeDecirCuantosAnunciosUsanCadaEntrada()
    {
        // Es lo que impide retirar a la ligera algo de lo que cuelgan anuncios.
        _context.Vehicles.Add(new Vehicle
        {
            Title = "Corolla 2018", Slug = "corolla-2018", PublicReference = "YU00001",
            MakeId = _toyotaId, ModelId = _corollaId, Year = 2018, Price = 5_000_000,
            SellerId = _sellerId, Status = VehicleStatus.Actif
        });
        await _context.SaveChangesAsync();

        var catalogs = (await _get.Handle(new GetCatalogsQuery(), CancellationToken.None)).Value!;

        var toyota = catalogs.Makes.Single(m => m.Name == "Toyota");
        toyota.ListingsCount.Should().Be(1);
        toyota.ModelsCount.Should().Be(1);
        toyota.Models.Single().ListingsCount.Should().Be(1);
    }

    [Fact]
    public async Task UnaMarcaSinAnunciosDebeSalirConCero()
    {
        var catalogs = (await _get.Handle(new GetCatalogsQuery(), CancellationToken.None)).Value!;

        catalogs.Makes.Single().ListingsCount.Should().Be(0);
    }

    public void Dispose() => _context.Dispose();
}
