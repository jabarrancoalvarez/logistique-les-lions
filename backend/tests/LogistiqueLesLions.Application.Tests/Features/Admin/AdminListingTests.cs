using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Listings;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Gestion des annonces» del backoffice.
/// </summary>
/// <remarks>
/// La regla de la especificación: el administrador modera, pero <b>no reescribe</b> la
/// información comercial. Ante un dato incorrecto, pide la corrección.
/// </remarks>
public class AdminListingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetAdminListingsQueryHandler _list;
    private readonly GetAdminListingQueryHandler _detail;
    private readonly ApplyAdminListingActionCommandHandler _action;
    private readonly RequestListingCorrectionCommandHandler _correction;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _proId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public AdminListingTests()
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
        _context.UserProfiles.AddRange(
            new UserProfile
            {
                Id = _adminId, DisplayName = "Admin", Phone = "+221770000000",
                PasswordHash = "x", Role = UserRole.Admin
            },
            new UserProfile
            {
                Id = _sellerId, DisplayName = "Mamadou Diop", Phone = "+221770000001",
                PasswordHash = "x", City = "Dakar", AccountType = AccountType.Particulier
            },
            new UserProfile
            {
                Id = _proId, DisplayName = "Auto Dakar", Phone = "+221770000002",
                PasswordHash = "x", City = "Thiès", AccountType = AccountType.Professionnel
            });
        _context.SaveChanges();

        _list       = new GetAdminListingsQueryHandler(_context);
        _detail     = new GetAdminListingQueryHandler(_context);
        _action     = new ApplyAdminListingActionCommandHandler(_context);
        _correction = new RequestListingCorrectionCommandHandler(_context);
    }

    private async Task<Vehicle> ListingAsync(
        Guid? id = null,
        Guid? sellerId = null,
        VehicleStatus status = VehicleStatus.Actif,
        string reference = "YU10001",
        decimal price = 8_900_000m,
        string? city = "Dakar")
    {
        var vehicle = new Vehicle
        {
            Id = id ?? _vehicleId,
            PublicReference = reference,
            Slug = $"toyota-rav4-{reference.ToLowerInvariant()}",
            Title = "Toyota RAV4 2019",
            MakeId = _makeId,
            Year = 2019,
            Price = price,
            City = city,
            SellerId = sellerId ?? _sellerId,
            Status = status,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-3)
        };
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    // ─── Listado ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaBuscarPorReferenciaYoon()
    {
        await ListingAsync(reference: "YU10001");
        await ListingAsync(id: Guid.NewGuid(), reference: "YU20002");

        var result = await _list.Handle(
            new GetAdminListingsQuery(Search: "yu20002"), CancellationToken.None);

        result.Value!.Items.Should().ContainSingle()
            .Which.PublicReference.Should().Be("YU20002");
    }

    [Fact]
    public async Task DeberiaVerTambienBorradoresYArchivados()
    {
        await ListingAsync(status: VehicleStatus.Brouillon);
        await ListingAsync(id: Guid.NewGuid(), reference: "YU20002", status: VehicleStatus.Archive);

        var result = await _list.Handle(new GetAdminListingsQuery(), CancellationToken.None);

        result.Value!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task DeberiaFiltrarPorTipoDeCuentaDeQuienPublica()
    {
        await ListingAsync();
        await ListingAsync(id: Guid.NewGuid(), sellerId: _proId, reference: "YU20002", city: "Thiès");

        var result = await _list.Handle(
            new GetAdminListingsQuery(SellerAccountType: AccountType.Professionnel),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle()
            .Which.SellerName.Should().Be("Auto Dakar");
    }

    [Fact]
    public async Task DeberiaFiltrarPorOcultosYMarcados()
    {
        var hidden = await ListingAsync();
        await ListingAsync(id: Guid.NewGuid(), reference: "YU20002");

        await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, hidden.Id, AdminListingAction.Hide, "Photos incorrectes"),
            CancellationToken.None);

        (await _list.Handle(new GetAdminListingsQuery(Hidden: true), CancellationToken.None))
            .Value!.Items.Should().ContainSingle().Which.Id.Should().Be(hidden.Id);

        (await _list.Handle(new GetAdminListingsQuery(Hidden: false), CancellationToken.None))
            .Value!.Items.Should().ContainSingle().Which.Id.Should().NotBe(hidden.Id);
    }

    // ─── Ocultar y reactivar ───────────────────────────────────────────────

    [Fact]
    public async Task OcultarDebeSacarloDelMarketplaceSinCambiarSuEstado()
    {
        var vehicle = await ListingAsync();

        var result = await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Hide, "Informations fausses"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var hidden = await _context.Vehicles.SingleAsync();
        hidden.AdminHiddenAt.Should().NotBeNull();
        // El estado sigue siendo suyo: no se le cambia a «En pause» por la espalda.
        hidden.Status.Should().Be(VehicleStatus.Actif);
        hidden.IsPubliclyVisible.Should().BeFalse();
        hidden.AcceptsNegotiation.Should().BeFalse();
    }

    [Fact]
    public async Task UnAnuncioOcultadoNoDebeSalirEnElBuscadorPublico()
    {
        var vehicle = await ListingAsync();
        await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Hide, "Véhicule inexistant"),
            CancellationToken.None);

        var publicQuery = VehicleQueryFilters.Apply(_context, new GetVehiclesQuery());

        (await publicQuery.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReactivarDebeDevolverloAlMarketplace()
    {
        var vehicle = await ListingAsync();
        await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Hide, "Vérification"), CancellationToken.None);

        var result = await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Reactivate, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.Vehicles.SingleAsync()).AdminHiddenAt.Should().BeNull();
    }

    [Fact]
    public async Task MarcarParaRevisionNoDebeOcultarNada()
    {
        var vehicle = await ListingAsync();

        await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Flag, null), CancellationToken.None);

        var flagged = await _context.Vehicles.SingleAsync();
        flagged.AdminFlaggedAt.Should().NotBeNull();
        // Es una señal interna: quien busca lo sigue viendo.
        flagged.IsPubliclyVisible.Should().BeTrue();
    }

    [Fact]
    public async Task LasMedidasQueAfectanAlUsuarioExigenMotivo()
    {
        var vehicle = await ListingAsync();

        foreach (var action in new[]
                 {
                     AdminListingAction.Hide,
                     AdminListingAction.Archive,
                     AdminListingAction.Delete
                 })
        {
            (await _action.Handle(new ApplyAdminListingActionCommand(
                _adminId, vehicle.Id, action, "   "), CancellationToken.None))
                .Error.Should().Be("Admin.ReasonRequired", $"«{action}» debe explicarse");
        }

        // Marcar para revisión es interno: no hace falta justificarlo.
        (await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Flag, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EliminarDebeSerSoftDelete()
    {
        var vehicle = await ListingAsync();

        await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Delete, "Tentative de fraude"),
            CancellationToken.None);

        (await _context.Vehicles.CountAsync()).Should().Be(0);
        (await _context.Vehicles.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CadaMedidaDebeDejarRastroConSuMotivo()
    {
        var vehicle = await ListingAsync();

        await _action.Handle(new ApplyAdminListingActionCommand(
            _adminId, vehicle.Id, AdminListingAction.Hide, "Prix trompeur"), CancellationToken.None);

        var detail = await _detail.Handle(new GetAdminListingQuery(vehicle.Id), CancellationToken.None);

        var action = detail.Value!.Actions.Should().ContainSingle().Subject;
        action.Type.Should().Be(AdminActionType.ListingHidden);
        action.Reason.Should().Be("Prix trompeur");
        action.AdminName.Should().Be("Admin");
    }

    // ─── Pedir corrección ──────────────────────────────────────────────────

    [Fact]
    public async Task PedirCorreccionDebeAvisarAQuienPublica()
    {
        var vehicle = await ListingAsync();

        var result = await _correction.Handle(new RequestListingCorrectionCommand(
            _adminId, vehicle.Id, "Le kilométrage annoncé ne correspond pas aux photos."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_sellerId);
        notification.Category.Should().Be(NotificationCategories.Admin);
        notification.Body.Should().Contain("YU10001");
        notification.Body.Should().Contain("kilométrage");

        // Y queda constancia de que se avisó.
        var action = await _context.AdminActions.SingleAsync();
        action.Type.Should().Be(AdminActionType.ListingCorrectionRequested);
    }

    [Fact]
    public async Task PedirCorreccionNoDebeTocarElAnuncio()
    {
        var vehicle = await ListingAsync(price: 8_900_000m);

        await _correction.Handle(new RequestListingCorrectionCommand(
            _adminId, vehicle.Id, "Le prix semble erroné."), CancellationToken.None);

        var unchanged = await _context.Vehicles.SingleAsync();
        // La información comercial pertenece a quien publica.
        unchanged.Price.Should().Be(8_900_000m);
        unchanged.Status.Should().Be(VehicleStatus.Actif);
        unchanged.AdminHiddenAt.Should().BeNull();
    }

    // ─── Ficha ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LaFichaDebeMostrarElHistorialDePreciosYLaCalidad()
    {
        var vehicle = await ListingAsync();
        _context.VehiclePriceHistories.AddRange(
            new VehiclePriceHistory { VehicleId = vehicle.Id, Price = 9_500_000m,
                                      ChangedAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new VehiclePriceHistory { VehicleId = vehicle.Id, Price = 8_900_000m,
                                      ChangedAt = DateTimeOffset.UtcNow.AddDays(-2) });
        await _context.SaveChangesAsync();

        var detail = await _detail.Handle(new GetAdminListingQuery(vehicle.Id), CancellationToken.None);

        detail.Value!.PriceHistory.Should().HaveCount(2);
        // De más reciente a más antiguo.
        detail.Value.PriceHistory[0].Price.Should().Be(8_900_000m);
        detail.Value.Quality.Items.Sum(i => i.MaxPoints).Should().Be(100);
        detail.Value.SellerPhone.Should().Be("+221770000001");
    }

    public void Dispose() => _context.Dispose();
}
