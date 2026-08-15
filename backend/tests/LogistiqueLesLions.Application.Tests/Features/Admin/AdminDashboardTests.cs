using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Dashboard;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Tableau de bord» del backoffice.
/// </summary>
/// <remarks>
/// Lo que se comprueba es que los conteos miran donde deben: los borradores cuentan como
/// borradores y no como anuncios activos, y las ventas verificadas salen de los contratos
/// validados y de ningún otro sitio.
/// </remarks>
public class AdminDashboardTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetAdminDashboardQueryHandler _handler;

    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _modelId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _buyerId = Guid.NewGuid();

    public AdminDashboardTests()
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
        _context.VehicleModels.Add(new VehicleModel { Id = _modelId, MakeId = _makeId, Name = "RAV4" });
        _context.UserProfiles.AddRange(
            new UserProfile
            {
                Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000001",
                PasswordHash = "x", AccountType = AccountType.Professionnel, PhoneVerified = true
            },
            new UserProfile
            {
                Id = _buyerId, DisplayName = "Mamadou", Phone = "+221770000002",
                PasswordHash = "x", AccountType = AccountType.Particulier
            });
        _context.SaveChanges();

        _handler = new GetAdminDashboardQueryHandler(_context);
    }

    private Vehicle Listing(VehicleStatus status, DateTimeOffset? publishedAt = null)
    {
        var vehicle = new Vehicle
        {
            PublicReference = $"YU{Guid.NewGuid().ToString()[..5]}",
            Slug = $"annonce-{Guid.NewGuid()}",
            Title = "Toyota RAV4",
            MakeId = _makeId,
            ModelId = _modelId,
            Year = 2019,
            Price = 8_900_000m,
            SellerId = _sellerId,
            Status = status,
            PublishedAt = publishedAt
        };
        _context.Vehicles.Add(vehicle);
        return vehicle;
    }

    private Task<AdminDashboardDto> DashboardAsync() =>
        _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None)
            .ContinueWith(t => t.Result.Value!);

    // ─── Usuarios ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DebeSepararParticularesYProfesionales()
    {
        var users = (await DashboardAsync()).Users;

        users.Total.Should().Be(2);
        users.Particuliers.Should().Be(1);
        users.Professionnels.Should().Be(1);
        users.PhoneVerified.Should().Be(1);
        // Los dos se han creado en esta misma ejecución.
        users.NewToday.Should().Be(2);
    }

    // ─── Marketplace ───────────────────────────────────────────────────────

    [Fact]
    public async Task LosBorradoresNoDebenContarComoAnunciosActivos()
    {
        Listing(VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-2));
        Listing(VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-40));
        Listing(VehicleStatus.Brouillon);
        Listing(VehicleStatus.EnPause, DateTimeOffset.UtcNow.AddDays(-10));
        Listing(VehicleStatus.Reserve, DateTimeOffset.UtcNow.AddDays(-5));
        Listing(VehicleStatus.Vendu, DateTimeOffset.UtcNow.AddDays(-20));
        Listing(VehicleStatus.Archive);
        await _context.SaveChangesAsync();

        var market = (await DashboardAsync()).Marketplace;

        market.Active.Should().Be(2);
        market.Drafts.Should().Be(1);
        market.Paused.Should().Be(1);
        market.Reserved.Should().Be(1);
        market.Sold.Should().Be(1);
        market.Archived.Should().Be(1);
    }

    [Fact]
    public async Task LosAnunciosNuevosSeCuentanPorFechaDePublicacion()
    {
        Listing(VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-2));
        Listing(VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-20));
        // Un borrador creado hoy no es un anuncio nuevo: nadie lo ha visto.
        Listing(VehicleStatus.Brouillon);
        await _context.SaveChangesAsync();

        var market = (await DashboardAsync()).Marketplace;

        market.NewLast7Days.Should().Be(1);
        market.NewLast30Days.Should().Be(2);
    }

    [Fact]
    public async Task UnAnuncioEliminadoNoDebeContarEnNingunEstado()
    {
        var vehicle = Listing(VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-1));
        await _context.SaveChangesAsync();

        vehicle.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        (await DashboardAsync()).Marketplace.Active.Should().Be(0);
    }

    // ─── Actividad ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LasVentasVerificadasDebenSalirDeLosContratosValidados()
    {
        var vehicle = Listing(VehicleStatus.Vendu, DateTimeOffset.UtcNow.AddDays(-10));
        await _context.SaveChangesAsync();

        var negotiation = new Negotiation
        {
            BuyerId = _buyerId, SellerId = _sellerId, VehicleId = vehicle.Id,
            Status = NegotiationStatus.Terminee
        };
        _context.Negotiations.Add(negotiation);

        _context.Contracts.AddRange(
            Contract(negotiation.Id, vehicle.Id, ContractStatus.Valide),
            Contract(negotiation.Id, vehicle.Id, ContractStatus.AValider));
        await _context.SaveChangesAsync();

        var activity = (await DashboardAsync()).Activity;

        activity.ContractsCreated.Should().Be(2);
        activity.ContractsValidated.Should().Be(1);
        // Las dos cifras no pueden discrepar: la venta verificada es el contrato validado.
        activity.VerifiedSales.Should().Be(activity.ContractsValidated);
    }

    [Fact]
    public async Task LasNegociacionesTerminadasNoDebenContarComoActivas()
    {
        var vehicle = Listing(VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-1));
        await _context.SaveChangesAsync();

        _context.Negotiations.AddRange(
            new Negotiation { BuyerId = _buyerId, SellerId = _sellerId, VehicleId = vehicle.Id,
                              Status = NegotiationStatus.EnCours },
            new Negotiation { BuyerId = _buyerId, SellerId = _sellerId, VehicleId = vehicle.Id,
                              Status = NegotiationStatus.EnAttente },
            new Negotiation { BuyerId = _buyerId, SellerId = _sellerId, VehicleId = vehicle.Id,
                              Status = NegotiationStatus.Terminee });
        await _context.SaveChangesAsync();

        var activity = (await DashboardAsync()).Activity;

        activity.NegotiationsStarted.Should().Be(3);
        activity.NegotiationsActive.Should().Be(2);
    }

    // ─── Demanda ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DebeListarLosModelosMasGuardadosEnFavoritos()
    {
        var rav4 = Listing(VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-1));
        await _context.SaveChangesAsync();

        _context.SavedVehicles.AddRange(
            new SavedVehicle { UserId = _buyerId, VehicleId = rav4.Id },
            new SavedVehicle { UserId = _sellerId, VehicleId = rav4.Id });
        await _context.SaveChangesAsync();

        var demand = (await DashboardAsync()).Demand;

        demand.FavoritesTotal.Should().Be(2);
        demand.TopFavoritedModels.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ModelDemandDto("Toyota RAV4", 2));
    }

    [Fact]
    public async Task DebeSepararLasSolicitudesNuevasDeLasQueYaEstanEnBusqueda()
    {
        _context.VehicleRequests.AddRange(
            Request(VehicleRequestStatus.NouvelleDemande),
            Request(VehicleRequestStatus.NouvelleDemande),
            Request(VehicleRequestStatus.EnRecherche),
            Request(VehicleRequestStatus.Terminee));
        await _context.SaveChangesAsync();

        var demand = (await DashboardAsync()).Demand;

        demand.RequestsPending.Should().Be(2);
        demand.RequestsSearching.Should().Be(1);
        demand.TopRequestedModels.Should().ContainSingle()
            .Which.Count.Should().Be(4);
    }

    // ─── Mon Garage ────────────────────────────────────────────────────────

    [Fact]
    public async Task DebeDistinguirLosCochesCompradosEnLaPlataformaDeLosAnadidosAMano()
    {
        var bought = new GarageVehicle
        {
            UserId = _buyerId, MakeId = _makeId, Year = 2019, SourceContractId = Guid.NewGuid()
        };
        var manual = new GarageVehicle { UserId = _buyerId, MakeId = _makeId, Year = 2015 };
        var listed = new GarageVehicle
        {
            UserId = _buyerId, MakeId = _makeId, Year = 2012, ListedVehicleId = Guid.NewGuid()
        };
        _context.GarageVehicles.AddRange(bought, manual, listed);
        await _context.SaveChangesAsync();

        var garage = (await DashboardAsync()).Garage;

        garage.VehiclesTotal.Should().Be(3);
        garage.FromYoonUAuto.Should().Be(1);
        garage.AddedManually.Should().Be(2);
        garage.ConvertedToListings.Should().Be(1);
    }

    // ─── Ayudas ────────────────────────────────────────────────────────────

    private Contract Contract(Guid negotiationId, Guid vehicleId, ContractStatus status) => new()
    {
        PublicReference = $"YC{Guid.NewGuid().ToString()[..5]}",
        NegotiationId   = negotiationId,
        VehicleId       = vehicleId,
        SellerId        = _sellerId,
        BuyerId         = _buyerId,
        CreatedByUserId = _sellerId,
        Status          = status,
        VehicleMake     = "Toyota",
        VehicleYear     = 2019,
        VehicleReference = "YU10001",
        AgreedPrice     = 8_300_000m,
        SellerLegalName = "Auto Dakar SARL",
        BuyerLegalName  = "Mamadou Diop",
        ValidatedAt     = status == ContractStatus.Valide ? DateTimeOffset.UtcNow : null
    };

    private VehicleRequest Request(VehicleRequestStatus status) => new()
    {
        PublicReference = $"YD{Guid.NewGuid().ToString()[..5]}",
        UserId    = _buyerId,
        MakeId    = _makeId,
        ModelName = "RAV4",
        Status    = status
    };

    public void Dispose() => _context.Dispose();
}
