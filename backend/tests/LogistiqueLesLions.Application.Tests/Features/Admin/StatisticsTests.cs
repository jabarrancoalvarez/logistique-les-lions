using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Statistics;
using LogistiqueLesLions.Application.Features.SavedSearches;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Statistiques» del backoffice.
/// </summary>
/// <remarks>
/// Lo que se comprueba aquí no son los totales —contar es trivial— sino las decisiones
/// de lectura: la mediana en lugar de la media, una persona contada una vez aunque tenga
/// varias búsquedas, y el desajuste entre lo que se busca y lo que hay publicado.
/// </remarks>
public class StatisticsTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetStatisticsQueryHandler _handler;

    private readonly Guid _makeToyotaId = Guid.NewGuid();
    private readonly Guid _modelHiluxId = Guid.NewGuid();
    private readonly Guid _modelCorollaId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _buyerId = Guid.NewGuid();

    public StatisticsTests()
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

        Seed();

        _handler = new GetStatisticsQueryHandler(_context);
    }

    private void Seed()
    {
        _context.VehicleMakes.Add(new VehicleMake
        {
            Id = _makeToyotaId, Name = "Toyota"
        });
        _context.VehicleModels.AddRange(
            new VehicleModel
            {
                Id = _modelHiluxId, MakeId = _makeToyotaId, Name = "Hilux"
            },
            new VehicleModel
            {
                Id = _modelCorollaId, MakeId = _makeToyotaId, Name = "Corolla"
            });

        _context.UserProfiles.AddRange(
            new UserProfile
            {
                Id = _sellerId, DisplayName = "Vendeur", Phone = "+221770000001",
                PasswordHash = "x", AccountType = AccountType.Professionnel, Region = "DK",
                LastLoginAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new UserProfile
            {
                Id = _buyerId, DisplayName = "Acheteur", Phone = "+221770000002",
                PasswordHash = "x", AccountType = AccountType.Particulier, Region = "TH"
                // Sin LastLoginAt: existe, pero no ha entrado.
            });

        _context.SaveChanges();
    }

    private Vehicle AddVehicle(
        Guid? modelId, decimal price, int mileage = 100_000, int year = 2018,
        VehicleStatus status = VehicleStatus.Actif, int views = 0)
    {
        var vehicle = new Vehicle
        {
            Title = "Annonce", Slug = Guid.NewGuid().ToString(),
            PublicReference = "YU" + Random.Shared.Next(10000, 99999),
            MakeId = _makeToyotaId, ModelId = modelId,
            Year = year, Mileage = mileage, Price = price,
            City = "Dakar", FuelType = FuelType.Diesel,
            Status = status, SellerId = _sellerId, ViewsCount = views,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        _context.Vehicles.Add(vehicle);
        return vehicle;
    }

    private void AddSavedSearch(Guid userId, Guid? modelId, decimal? budget = null)
    {
        _context.SavedSearches.Add(new SavedSearch
        {
            UserId = userId, Name = "Ma recherche",
            FiltersJson = SavedSearchFilters.Serialize(new GetVehiclesQuery
            {
                MakeId = _makeToyotaId, ModelId = modelId, PriceTo = budget
            })
        });
    }

    private Task<LogistiqueLesLions.Application.Common.Models.Result<StatisticsDto>> RunAsync(
        int days = 30) =>
        _handler.Handle(new GetStatisticsQuery(days), CancellationToken.None);

    // ─── Usuarios ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaSepararParticulierDeProfessionnel()
    {
        var stats = (await RunAsync()).Value!.Users;

        stats.Total.Should().Be(2);
        stats.Particuliers.Should().Be(1);
        stats.Professionnels.Should().Be(1);
    }

    [Fact]
    public async Task ActivoEsHaberEntradoEnElPeriodo()
    {
        // Tener cuenta no es estar activo: solo cuenta quien ha entrado.
        (await RunAsync()).Value!.Users.Active.Should().Be(1);
    }

    [Fact]
    public async Task DeberiaRepartirLosUsuariosPorRegion()
    {
        var byRegion = (await RunAsync()).Value!.Users.ByRegion;

        byRegion.Should().HaveCount(2);
        byRegion.Should().Contain(r => r.Label == "DK" && r.Count == 1);
    }

    // ─── Oferta ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LaMedianaNoDebeDejarseArrastrarPorUnAnuncioCaro()
    {
        AddVehicle(_modelCorollaId, 5_000_000);
        AddVehicle(_modelCorollaId, 6_000_000);
        // Un solo anuncio de lujo dispara la media, pero no la mediana.
        AddVehicle(_modelCorollaId, 100_000_000);
        await _context.SaveChangesAsync();

        var supply = (await RunAsync()).Value!.Supply;

        supply.MedianPrice.Should().Be(6_000_000);
        supply.AveragePrice.Should().BeGreaterThan(30_000_000);
    }

    [Fact]
    public async Task ConNumeroParDeAnunciosLaMedianaEsElPuntoMedio()
    {
        AddVehicle(_modelCorollaId, 4_000_000);
        AddVehicle(_modelCorollaId, 6_000_000);
        await _context.SaveChangesAsync();

        (await RunAsync()).Value!.Supply.MedianPrice.Should().Be(5_000_000);
    }

    [Fact]
    public async Task SinAnunciosNoDebeInventarseUnPrecioMedio()
    {
        var supply = (await RunAsync()).Value!.Supply;

        supply.ActiveListings.Should().Be(0);
        supply.MedianPrice.Should().BeNull();
        supply.AveragePrice.Should().BeNull();
        supply.MedianYear.Should().BeNull();
    }

    [Fact]
    public async Task LaOfertaNoDebeContarLosAnunciosArchivados()
    {
        AddVehicle(_modelCorollaId, 5_000_000);
        AddVehicle(_modelCorollaId, 5_000_000, status: VehicleStatus.Archive);
        AddVehicle(_modelCorollaId, 5_000_000, status: VehicleStatus.Vendu);
        await _context.SaveChangesAsync();

        (await RunAsync()).Value!.Supply.ActiveListings.Should().Be(1);
    }

    [Fact]
    public async Task ElRankingDeModelosDebeLlevarLaMarcaDelante()
    {
        AddVehicle(_modelHiluxId, 12_000_000);
        await _context.SaveChangesAsync();

        var supply = (await RunAsync()).Value!.Supply;

        supply.TopMakes.Should().ContainSingle(m => m.Label == "Toyota");
        supply.TopModels.Should().ContainSingle(m => m.Label == "Toyota Hilux");
    }

    // ─── Demanda ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaLeerLosFiltrosGuardadosParaSaberQueSeBusca()
    {
        AddSavedSearch(_buyerId, _modelHiluxId, budget: 10_000_000);
        await _context.SaveChangesAsync();

        var demand = (await RunAsync()).Value!.Demand;

        demand.SavedSearches.Should().Be(1);
        demand.TopSearchedMakes.Should().ContainSingle(m => m.Label == "Toyota");
        demand.TopUsedFilters.Should().Contain(f => f.Label == "Marque");
        demand.TopUsedFilters.Should().Contain(f => f.Label == "Modèle");
        demand.TopUsedFilters.Should().Contain(f => f.Label == "Prix");
        demand.MedianSearchBudget.Should().Be(10_000_000);
    }

    [Fact]
    public async Task UnJsonIlegibleNoDebeTumbarLasEstadisticas()
    {
        // Una fila corrupta no puede dejar el panel entero sin datos.
        _context.SavedSearches.Add(new SavedSearch
        {
            UserId = _buyerId, Name = "Corrompue", FiltersJson = "{ pas du json"
        });
        await _context.SaveChangesAsync();

        var result = await RunAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Demand.SavedSearches.Should().Be(1);
    }

    // ─── Desajuste oferta/demanda ──────────────────────────────────────────

    [Fact]
    public async Task DeberiaDestacarElModeloQueSeBuscaYNoSeEncuentra()
    {
        AddSavedSearch(_buyerId, _modelHiluxId);
        AddSavedSearch(_sellerId, _modelHiluxId);
        _context.VehicleRequests.Add(new VehicleRequest
        {
            PublicReference = "YD11111", UserId = _buyerId,
            MakeId = _makeToyotaId, MakeName = "Toyota", ModelName = "Hilux"
        });
        // Hay Corollas de sobra, pero ningún Hilux.
        AddVehicle(_modelCorollaId, 5_000_000);
        await _context.SaveChangesAsync();

        var gaps = (await RunAsync()).Value!.Demand.Gaps;

        var hilux = gaps.Should().ContainSingle(g => g.Label == "Toyota Hilux").Subject;
        hilux.SearchingUsers.Should().Be(2);
        hilux.Requests.Should().Be(1);
        hilux.AvailableListings.Should().Be(0);
    }

    [Fact]
    public async Task VariasBusquedasDeLaMismaPersonaCuentanComoUna()
    {
        // Si no, cualquiera podría inflar la demanda guardando la misma búsqueda.
        AddSavedSearch(_buyerId, _modelHiluxId);
        AddSavedSearch(_buyerId, _modelHiluxId);
        AddSavedSearch(_buyerId, _modelHiluxId);
        await _context.SaveChangesAsync();

        var hilux = (await RunAsync()).Value!.Demand.Gaps
            .Should().ContainSingle(g => g.Label == "Toyota Hilux").Subject;

        hilux.SearchingUsers.Should().Be(1);
    }

    [Fact]
    public async Task UnModeloQueNadieBuscaNoEsUnHuecoDeMercado()
    {
        AddVehicle(_modelCorollaId, 5_000_000);
        await _context.SaveChangesAsync();

        (await RunAsync()).Value!.Demand.Gaps.Should().BeEmpty();
    }

    [Fact]
    public async Task ElHuecoMayorDebeIrPrimero()
    {
        // Hilux: dos personas buscando, ningún anuncio. Corolla: una y con oferta.
        AddSavedSearch(_buyerId, _modelHiluxId);
        AddSavedSearch(_sellerId, _modelHiluxId);
        AddSavedSearch(_buyerId, _modelCorollaId);
        AddVehicle(_modelCorollaId, 5_000_000);
        await _context.SaveChangesAsync();

        (await RunAsync()).Value!.Demand.Gaps[0].Label.Should().Be("Toyota Hilux");
    }

    [Fact]
    public async Task LaSolicitudSinModeloEnCatalogoTambienDebeContar()
    {
        // «Trouvez-moi cette voiture» admite texto libre: no puede perderse.
        _context.VehicleRequests.Add(new VehicleRequest
        {
            PublicReference = "YD22222", UserId = _buyerId,
            MakeName = "Mitsubishi", ModelName = "L200"
        });
        await _context.SaveChangesAsync();

        var gap = (await RunAsync()).Value!.Demand.Gaps
            .Should().ContainSingle(g => g.Label == "Mitsubishi L200").Subject;

        gap.Requests.Should().Be(1);
        gap.SearchingUsers.Should().Be(0);
    }

    // ─── Conversión ────────────────────────────────────────────────────────

    [Fact]
    public async Task ElEmbudoDebeIrDeLaVisitaALaVentaVerificada()
    {
        var vehicle = AddVehicle(_modelCorollaId, 5_000_000, views: 40);
        await _context.SaveChangesAsync();

        _context.SavedVehicles.Add(new SavedVehicle
        {
            UserId = _buyerId, VehicleId = vehicle.Id
        });

        var negotiation = new Negotiation
        {
            VehicleId = vehicle.Id, BuyerId = _buyerId, SellerId = _sellerId
        };
        _context.Negotiations.Add(negotiation);
        await _context.SaveChangesAsync();

        _context.Offers.Add(new Offer
        {
            NegotiationId = negotiation.Id, FromUserId = _buyerId,
            Amount = 4_500_000, ListedPrice = 5_000_000, Status = OfferStatus.Acceptee
        });
        await _context.SaveChangesAsync();

        var funnel = (await RunAsync()).Value!.Funnel;

        funnel.Views.Should().Be(40);
        funnel.Favorites.Should().Be(1);
        funnel.Negotiations.Should().Be(1);
        funnel.Offers.Should().Be(1);
        funnel.AcceptedOffers.Should().Be(1);
        funnel.Contracts.Should().Be(0);
        funnel.VerifiedSales.Should().Be(0);
    }

    // ─── Periodo ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ElPeriodoDebeQuedarseEnUnRangoRazonable()
    {
        // Ni cero días ni diez años: la consulta no puede quedar a merced de la URL.
        (await RunAsync(days: 0)).Value!.PeriodDays.Should().Be(1);
        (await RunAsync(days: 5_000)).Value!.PeriodDays.Should().Be(365);
    }

    public void Dispose() => _context.Dispose();
}
