using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

/// <summary>
/// Filtros del Marketplace. Cubre sobre todo los que tienen semántica no evidente:
/// el equipamiento (conjunción, no disyunción), el estado aduanero y la visibilidad
/// pública de cada estado del anuncio.
/// </summary>
public class GetVehiclesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetVehiclesQueryHandler _handler;

    private readonly Guid _makeToyota = Guid.NewGuid();
    private readonly Guid _makePeugeot = Guid.NewGuid();
    private readonly Guid _particulier = Guid.NewGuid();
    private readonly Guid _professionnel = Guid.NewGuid();
    private readonly Guid _clim = Guid.NewGuid();
    private readonly Guid _gps = Guid.NewGuid();

    public GetVehiclesQueryHandlerTests()
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

        // Se usa el servicio real: así el listado ejercita también el camino del
        // indicador de precio, en lugar de un doble que siempre devuelve vacío.
        _handler = new GetVehiclesQueryHandler(
            _context, new Application.Services.PriceIndicatorService(_context));
        Seed();
    }

    private void Seed()
    {
        _context.VehicleMakes.AddRange(
            new VehicleMake { Id = _makeToyota, Name = "Toyota", Country = "JP" },
            new VehicleMake { Id = _makePeugeot, Name = "Peugeot", Country = "FR" });

        _context.VehicleEquipments.AddRange(
            new VehicleEquipment { Id = _clim, Code = "CLIMATISATION", Name = "Climatisation", IsActive = true },
            new VehicleEquipment { Id = _gps, Code = "GPS", Name = "Navigation / GPS", IsActive = true });

        _context.UserProfiles.AddRange(
            new UserProfile
            {
                Id = _particulier, DisplayName = "Mamadou Diop", Phone = "+221770000001",
                PasswordHash = "x", AccountType = AccountType.Particulier
            },
            new UserProfile
            {
                Id = _professionnel, DisplayName = "Auto Dakar Services", Phone = "+221770000002",
                PasswordHash = "x", AccountType = AccountType.Professionnel
            });

        // RAV4: dédouané, Dakar, professionnel, climatisation + GPS
        var rav4 = Vehicle("Toyota RAV4", _makeToyota, 8_900_000m, 2019, 126000,
            "DK", "Dakar", _professionnel, VehicleStatus.Actif);
        rav4.Equipments.Add(new VehicleEquipmentLink { EquipmentId = _clim });
        rav4.Equipments.Add(new VehicleEquipmentLink { EquipmentId = _gps });

        // Hilux: non dédouané, Thiès, particulier, solo climatisation
        var hilux = Vehicle("Toyota Hilux", _makeToyota, 12_000_000m, 2021, 80000,
            "TH", "Mbour", _particulier, VehicleStatus.Actif);
        hilux.Equipments.Add(new VehicleEquipmentLink { EquipmentId = _clim });

        // 208: passavant, Dakar, particulier, sin equipamiento
        var p208 = Vehicle("Peugeot 208", _makePeugeot, 4_500_000m, 2017, 150000,
            "DK", "Rufisque", _particulier, VehicleStatus.Actif);

        // Reservado: sigue siendo visible públicamente
        var reserve = Vehicle("Toyota Corolla", _makeToyota, 6_000_000m, 2018, 100000,
            "DK", "Dakar", _particulier, VehicleStatus.Reserve);

        // Borrador y archivado: no deben aparecer
        var brouillon = Vehicle("Toyota Yaris", _makeToyota, 3_000_000m, 2016, 90000,
            "DK", "Dakar", _particulier, VehicleStatus.Brouillon);
        var archive = Vehicle("Peugeot 308", _makePeugeot, 3_500_000m, 2015, 200000,
            "DK", "Dakar", _particulier, VehicleStatus.Archive);

        _context.Vehicles.AddRange(rav4, hilux, p208, reserve, brouillon, archive);
        _context.SaveChanges();
    }

    private static Vehicle Vehicle(
        string title, Guid makeId, decimal price, int year, int mileage,
        string region, string city, Guid sellerId, VehicleStatus status)
    {
        var id = Guid.NewGuid();
        return new Vehicle
        {
            Id = id,
            PublicReference = $"YU{Random.Shared.Next(10000, 99999)}",
            Slug = $"{title.ToLowerInvariant().Replace(' ', '-')}-{id:N}",
            Title = title,
            MakeId = makeId,
            Year = year,
            Mileage = mileage,
            Price = price,
            Region = region,
            City = city,
            SellerId = sellerId,
            Status = status,
            FuelType = FuelType.Essence,
            Transmission = TransmissionType.Automatique
        };
    }

    private async Task<List<string>> TitlesFor(GetVehiclesQuery query)
    {
        var result = await _handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!.Items.Select(i => i.Title).ToList();
    }

    [Fact]
    public async Task DeberiaMostrarSoloLosAnunciosPublicamenteVisibles()
    {
        var titles = await TitlesFor(new GetVehiclesQuery());

        // Actif y Réservé sí; Brouillon y Archivé no.
        titles.Should().BeEquivalentTo(
            ["Toyota RAV4", "Toyota Hilux", "Peugeot 208", "Toyota Corolla"]);
    }

    [Fact]
    public async Task FiltrarPorVendedorNoDebeRevelarSusBorradores()
    {
        // La ficha enlaza a "Voir ses autres véhicules" con el sellerId: un visitante
        // no debe ver por esa vía los anuncios que el vendedor no ha publicado.
        var titles = await TitlesFor(new GetVehiclesQuery { SellerId = _particulier });

        titles.Should().NotContain("Toyota Yaris");   // Brouillon
        titles.Should().NotContain("Peugeot 308");    // Archivé
    }

    [Fact]
    public async Task ElPropietarioSiDebeVerSusBorradores()
    {
        var titles = await TitlesFor(new GetVehiclesQuery
        {
            SellerId = _particulier,
            IncludeNonPublic = true
        });

        titles.Should().Contain("Toyota Yaris");
        titles.Should().Contain("Peugeot 308");
    }

    [Fact]
    public async Task DeberiaFiltrarPorRegionYCiudad()
    {
        (await TitlesFor(new GetVehiclesQuery { Region = "DK" }))
            .Should().BeEquivalentTo(["Toyota RAV4", "Peugeot 208", "Toyota Corolla"]);

        (await TitlesFor(new GetVehiclesQuery { City = "Mbour" }))
            .Should().BeEquivalentTo(["Toyota Hilux"]);
    }

    [Fact]
    public async Task ElEquipamientoDebeExigirseTodo_NoAlguno()
    {
        // Solo el RAV4 declara climatisation Y GPS.
        (await TitlesFor(new GetVehiclesQuery { EquipmentIds = [_clim, _gps] }))
            .Should().BeEquivalentTo(["Toyota RAV4"]);

        // Con climatisation sola aparecen los dos que la tienen.
        (await TitlesFor(new GetVehiclesQuery { EquipmentIds = [_clim] }))
            .Should().BeEquivalentTo(["Toyota RAV4", "Toyota Hilux"]);
    }

    [Fact]
    public async Task DeberiaFiltrarPorTipoDeUsuarioQuePublica()
    {
        (await TitlesFor(new GetVehiclesQuery { SellerAccountType = AccountType.Professionnel }))
            .Should().BeEquivalentTo(["Toyota RAV4"]);
    }

    [Fact]
    public async Task DeberiaFiltrarPorRangosDePrecioYKilometraje()
    {
        (await TitlesFor(new GetVehiclesQuery { PriceFrom = 5_000_000m, PriceTo = 10_000_000m }))
            .Should().BeEquivalentTo(["Toyota RAV4", "Toyota Corolla"]);

        (await TitlesFor(new GetVehiclesQuery { MileageTo = 100_000 }))
            .Should().BeEquivalentTo(["Toyota Hilux", "Toyota Corolla"]);
    }

    [Fact]
    public async Task LaBusquedaDebeEncontrarPorMarcaYPorReferencia()
    {
        (await TitlesFor(new GetVehiclesQuery { Search = "peugeot" }))
            .Should().BeEquivalentTo(["Peugeot 208"]);

        var reference = await _context.Vehicles
            .Where(v => v.Title == "Toyota Hilux")
            .Select(v => v.PublicReference)
            .SingleAsync();

        (await TitlesFor(new GetVehiclesQuery { Search = reference }))
            .Should().BeEquivalentTo(["Toyota Hilux"]);
    }

    [Fact]
    public async Task DeberiaOrdenarSegunLaEspecificacion()
    {
        (await TitlesFor(new GetVehiclesQuery { SortBy = "price", SortDesc = false }))
            .Should().ContainInOrder("Peugeot 208", "Toyota Corolla", "Toyota RAV4", "Toyota Hilux");

        (await TitlesFor(new GetVehiclesQuery { SortBy = "mileage", SortDesc = false }))
            .Should().ContainInOrder("Toyota Hilux", "Toyota Corolla", "Toyota RAV4", "Peugeot 208");

        (await TitlesFor(new GetVehiclesQuery { SortBy = "year", SortDesc = true }))
            .Should().ContainInOrder("Toyota Hilux", "Toyota RAV4", "Toyota Corolla", "Peugeot 208");
    }

    [Fact]
    public async Task DeberiaCombinarVariosFiltros()
    {
        var titles = await TitlesFor(new GetVehiclesQuery
        {
            MakeId = _makeToyota,
            Region = "DK",
            PriceTo = 9_000_000m
        });

        titles.Should().BeEquivalentTo(["Toyota RAV4", "Toyota Corolla"]);
    }

    public void Dispose() => _context.Dispose();
}
