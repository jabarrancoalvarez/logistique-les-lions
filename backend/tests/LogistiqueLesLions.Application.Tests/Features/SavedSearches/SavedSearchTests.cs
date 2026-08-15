using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.SavedSearches;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.SavedSearches;

public class SavedSearchTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _toyota = Guid.NewGuid();
    private readonly Guid _peugeot = Guid.NewGuid();

    public SavedSearchTests()
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

        _context.VehicleMakes.AddRange(
            new VehicleMake { Id = _toyota, Name = "Toyota", Country = "JP" },
            new VehicleMake { Id = _peugeot, Name = "Peugeot", Country = "FR" });
        _context.UserProfiles.AddRange(
            new UserProfile { Id = _userId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _otherUserId, DisplayName = "Fatou", Phone = "+221770000002", PasswordHash = "x" });
        _context.SaveChanges();
    }

    private Guid AddVehicle(Guid makeId, decimal price, string region = "DK",
                            Guid? sellerId = null, VehicleStatus status = VehicleStatus.Actif)
    {
        var id = Guid.NewGuid();
        _context.Vehicles.Add(new Vehicle
        {
            Id = id,
            PublicReference = $"YU{Random.Shared.Next(10000, 99999)}",
            Slug = id.ToString("N"),
            Title = "Véhicule",
            MakeId = makeId,
            Year = 2019,
            Price = price,
            Region = region,
            SellerId = sellerId ?? Guid.NewGuid(),
            Status = status
        });
        _context.SaveChanges();
        return id;
    }

    private Task<Guid> CreateSearch(GetVehiclesQuery filters, string name = "Toyota Dakar",
                                    bool alert = true, Guid? userId = null) =>
        new CreateSavedSearchCommandHandler(_context)
            .Handle(new CreateSavedSearchCommand(userId ?? _userId, name, filters, alert), CancellationToken.None)
            .ContinueWith(t => t.Result.Value);

    // ─── Serialización de filtros ──────────────────────────────────────────

    [Fact]
    public void LosFiltrosDebenSobrevivirAlViajeDeIdaYVuelta()
    {
        var filters = new GetVehiclesQuery
        {
            MakeId = _toyota,
            PriceTo = 12_000_000m,
            YearFrom = 2017,
            YearTo = 2022,
            MileageTo = 150_000,
            Region = "DK",
            CustomsStatus = CustomsStatus.Dedouane,
            FuelType = FuelType.Diesel,
            EquipmentIds = [Guid.NewGuid()]
        };

        var restored = SavedSearchFilters.Deserialize(SavedSearchFilters.Serialize(filters));

        restored.MakeId.Should().Be(filters.MakeId);
        restored.PriceTo.Should().Be(12_000_000m);
        restored.YearFrom.Should().Be(2017);
        restored.YearTo.Should().Be(2022);
        restored.MileageTo.Should().Be(150_000);
        restored.Region.Should().Be("DK");
        restored.CustomsStatus.Should().Be(CustomsStatus.Dedouane);
        restored.FuelType.Should().Be(FuelType.Diesel);
        restored.EquipmentIds.Should().BeEquivalentTo(filters.EquipmentIds);
    }

    [Fact]
    public void LosEnumsDebenGuardarsePorNombreYNoPorNumero()
    {
        // Guardarlos por número haría que reordenar el enum cambiara en silencio el
        // significado de todas las búsquedas ya guardadas.
        var json = SavedSearchFilters.Serialize(new GetVehiclesQuery
        {
            CustomsStatus = CustomsStatus.Passavant
        });

        json.Should().Contain("Passavant");
    }

    [Fact]
    public void NoDebeGuardarsePermisoParaVerAnunciosNoPublicos()
    {
        var json = SavedSearchFilters.Serialize(new GetVehiclesQuery
        {
            IncludeNonPublic = true,
            SellerId = Guid.NewGuid(),
            Status = VehicleStatus.Brouillon
        });

        var restored = SavedSearchFilters.Deserialize(json);

        restored.IncludeNonPublic.Should().BeFalse();
        restored.SellerId.Should().BeNull();
        restored.Status.Should().BeNull();
    }

    [Fact]
    public void UnJsonCorruptoNoDebeTumbarElListado()
    {
        var restored = SavedSearchFilters.Deserialize("{ esto no es json ");

        restored.Should().NotBeNull();
        restored.MakeId.Should().BeNull();
    }

    // ─── Listado con recuento ──────────────────────────────────────────────

    [Fact]
    public async Task ElListadoDebeIndicarCuantosVehiculosCoinciden()
    {
        AddVehicle(_toyota, 8_000_000m);
        AddVehicle(_toyota, 9_000_000m);
        AddVehicle(_peugeot, 5_000_000m);

        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota });

        var result = await new GetMySavedSearchesQueryHandler(_context)
            .Handle(new GetMySavedSearchesQuery(_userId), CancellationToken.None);

        result.Value!.Should().ContainSingle();
        result.Value![0].ResultsCount.Should().Be(2);
    }

    [Fact]
    public async Task ElRecuentoNoDebeIncluirAnunciosNoPublicados()
    {
        AddVehicle(_toyota, 8_000_000m);
        AddVehicle(_toyota, 9_000_000m, status: VehicleStatus.Brouillon);

        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota });

        var result = await new GetMySavedSearchesQueryHandler(_context)
            .Handle(new GetMySavedSearchesQuery(_userId), CancellationToken.None);

        result.Value![0].ResultsCount.Should().Be(1);
    }

    [Fact]
    public async Task CadaUsuarioSoloDebeVerSusPropiasBusquedas()
    {
        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota }, userId: _userId);
        await CreateSearch(new GetVehiclesQuery { MakeId = _peugeot }, userId: _otherUserId);

        var result = await new GetMySavedSearchesQueryHandler(_context)
            .Handle(new GetMySavedSearchesQuery(_userId), CancellationToken.None);

        result.Value!.Should().ContainSingle();
    }

    // ─── Propiedad ─────────────────────────────────────────────────────────

    [Fact]
    public async Task NoDebePoderseBorrarLaBusquedaDeOtroUsuario()
    {
        var searchId = await CreateSearch(new GetVehiclesQuery(), userId: _userId);

        var result = await new DeleteSavedSearchCommandHandler(_context)
            .Handle(new DeleteSavedSearchCommand(_otherUserId, searchId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("SavedSearch.NotFound");
        (await _context.SavedSearches.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task BorrarDebeSerSoftDelete()
    {
        var searchId = await CreateSearch(new GetVehiclesQuery());

        await new DeleteSavedSearchCommandHandler(_context)
            .Handle(new DeleteSavedSearchCommand(_userId, searchId), CancellationToken.None);

        (await _context.SavedSearches.CountAsync()).Should().Be(0);
        (await _context.SavedSearches.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    // ─── Alerta de nuevos vehículos ────────────────────────────────────────

    [Fact]
    public async Task DebeNotificarUnAnuncioQueCoincideConLaBusqueda()
    {
        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota, Region = "DK" });
        var vehicleId = AddVehicle(_toyota, 8_000_000m, region: "DK");

        var sent = await new NewVehicleAlertService(_context).NotifyMatchingSearchesAsync(vehicleId);

        sent.Should().Be(1);
        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_userId);
        notification.Title.Should().Be("Nouvelle annonce");
        notification.Body.Should().Contain("Toyota Dakar");
    }

    [Fact]
    public async Task NoDebeNotificarUnAnuncioQueNoCoincide()
    {
        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota });
        var vehicleId = AddVehicle(_peugeot, 8_000_000m);

        var sent = await new NewVehicleAlertService(_context).NotifyMatchingSearchesAsync(vehicleId);

        sent.Should().Be(0);
    }

    [Fact]
    public async Task NoDebeNotificarSiLaAlertaEstaApagada()
    {
        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota }, alert: false);
        var vehicleId = AddVehicle(_toyota, 8_000_000m);

        var sent = await new NewVehicleAlertService(_context).NotifyMatchingSearchesAsync(vehicleId);

        sent.Should().Be(0);
    }

    [Fact]
    public async Task NoDebeAvisarAlPropioVendedorDeSuAnuncio()
    {
        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota });
        var vehicleId = AddVehicle(_toyota, 8_000_000m, sellerId: _userId);

        var sent = await new NewVehicleAlertService(_context).NotifyMatchingSearchesAsync(vehicleId);

        sent.Should().Be(0);
    }

    [Fact]
    public async Task NoDebeNotificarUnBorrador()
    {
        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota });
        var vehicleId = AddVehicle(_toyota, 8_000_000m, status: VehicleStatus.Brouillon);

        var sent = await new NewVehicleAlertService(_context).NotifyMatchingSearchesAsync(vehicleId);

        sent.Should().Be(0);
    }

    [Fact]
    public async Task LaBusquedaGuardadaDebeRespetarSusRangosDePrecio()
    {
        await CreateSearch(new GetVehiclesQuery { MakeId = _toyota, PriceTo = 9_000_000m });

        var barato = AddVehicle(_toyota, 8_000_000m);
        var caro = AddVehicle(_toyota, 15_000_000m);

        var service = new NewVehicleAlertService(_context);

        (await service.NotifyMatchingSearchesAsync(barato)).Should().Be(1);
        (await service.NotifyMatchingSearchesAsync(caro)).Should().Be(0);
    }

    public void Dispose() => _context.Dispose();
}
