using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Requests;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Demandes de véhicules» del backoffice.
/// </summary>
/// <remarks>
/// Aquí el administrador deja de moderar y presta un servicio: busca coches para quien
/// los pide, anexa anuncios propios o propuestas externas, y avisa al usuario.
/// </remarks>
public class AdminRequestTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetAdminRequestsQueryHandler _list;
    private readonly GetAdminRequestQueryHandler _detail;
    private readonly AssignRequestCommandHandler _assign;
    private readonly ChangeRequestStatusCommandHandler _status;
    private readonly AddInternalProposalCommandHandler _internal;
    private readonly AddExternalProposalCommandHandler _external;
    private readonly RemoveProposalCommandHandler _remove;
    private readonly ReplyToRequestCommandHandler _reply;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _requestId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public AdminRequestTests()
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
                Id = _userId, DisplayName = "Mamadou Diop", Phone = "+221770000001",
                PasswordHash = "x"
            },
            new UserProfile
            {
                Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000002",
                PasswordHash = "x"
            });
        _context.SaveChanges();

        _list     = new GetAdminRequestsQueryHandler(_context);
        _detail   = new GetAdminRequestQueryHandler(_context);
        _assign   = new AssignRequestCommandHandler(_context);
        _status   = new ChangeRequestStatusCommandHandler(_context);
        _internal = new AddInternalProposalCommandHandler(_context);
        _external = new AddExternalProposalCommandHandler(_context);
        _remove   = new RemoveProposalCommandHandler(_context);
        _reply    = new ReplyToRequestCommandHandler(_context);
    }

    private async Task<VehicleRequest> RequestAsync(
        VehicleRequestStatus status = VehicleRequestStatus.NouvelleDemande,
        Guid? id = null,
        string reference = "YD00001")
    {
        var request = new VehicleRequest
        {
            Id = id ?? _requestId,
            PublicReference = reference,
            UserId = _userId,
            MakeId = _makeId,
            MakeName = "Toyota",
            ModelName = "RAV4",
            YearFrom = 2017,
            MaxBudget = 9_000_000m,
            Status = status
        };
        _context.VehicleRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    private async Task<Vehicle> ListingAsync(VehicleStatus status = VehicleStatus.Actif)
    {
        var vehicle = new Vehicle
        {
            Id = _vehicleId,
            PublicReference = "YU10001",
            Slug = "toyota-rav4-yu10001",
            Title = "Toyota RAV4 2019",
            MakeId = _makeId,
            Year = 2019,
            Price = 8_900_000m,
            SellerId = _sellerId,
            Status = status,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    // ─── Listado ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoQueEsperaRespuestaDebeIrAntesQueLoCerrado()
    {
        await RequestAsync(VehicleRequestStatus.Terminee, Guid.NewGuid(), "YD00001");
        await RequestAsync(VehicleRequestStatus.NouvelleDemande, Guid.NewGuid(), "YD00002");

        var result = await _list.Handle(new GetAdminRequestsQuery(), CancellationToken.None);

        result.Value!.Items[0].PublicReference.Should().Be("YD00002");
    }

    [Fact]
    public async Task DeberiaPoderVerseLaColaSinResponsable()
    {
        var taken = await RequestAsync(id: Guid.NewGuid(), reference: "YD00001");
        await RequestAsync(id: Guid.NewGuid(), reference: "YD00002");

        await _assign.Handle(new AssignRequestCommand(_adminId, taken.Id, true), CancellationToken.None);

        var result = await _list.Handle(
            new GetAdminRequestsQuery(Unassigned: true), CancellationToken.None);

        result.Value!.Items.Should().ContainSingle()
            .Which.PublicReference.Should().Be("YD00002");
    }

    // ─── Responsable ───────────────────────────────────────────────────────

    [Fact]
    public async Task HacerseCargoYSoltarDebenQuedarRegistrados()
    {
        var request = await RequestAsync();

        await _assign.Handle(new AssignRequestCommand(_adminId, request.Id, true), CancellationToken.None);
        (await _context.VehicleRequests.SingleAsync()).AssignedAdminId.Should().Be(_adminId);

        await _assign.Handle(new AssignRequestCommand(_adminId, request.Id, false), CancellationToken.None);
        (await _context.VehicleRequests.SingleAsync()).AssignedAdminId.Should().BeNull();

        var detail = await _detail.Handle(new GetAdminRequestQuery(request.Id), CancellationToken.None);
        detail.Value!.Actions.Should().HaveCount(2);
    }

    // ─── Estados ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaSeguirseElCaminoDelDocumento()
    {
        var request = await RequestAsync();

        // Nouvelle demande → En recherche
        (await _status.Handle(new ChangeRequestStatusCommand(
            _adminId, request.Id, VehicleRequestStatus.EnRecherche, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        // …y no se salta directamente a «Véhicule proposé» desde el principio.
        var another = await RequestAsync(id: Guid.NewGuid(), reference: "YD00002");
        (await _status.Handle(new ChangeRequestStatusCommand(
            _adminId, another.Id, VehicleRequestStatus.VehiculePropose, null), CancellationToken.None))
            .Error.Should().Be("VehicleRequest.InvalidTransition");
    }

    [Fact]
    public async Task UnaSolicitudCerradaNoSeReabre()
    {
        var request = await RequestAsync(VehicleRequestStatus.Terminee);

        var result = await _status.Handle(new ChangeRequestStatusCommand(
            _adminId, request.Id, VehicleRequestStatus.EnRecherche, null), CancellationToken.None);

        // Si vuelve a necesitar un coche, crea otra: esta conserva lo que se hizo por ella.
        result.Error.Should().Be("VehicleRequest.InvalidTransition");
    }

    [Fact]
    public async Task AnularLaSolicitudDeOtroExigeMotivo()
    {
        var request = await RequestAsync();

        (await _status.Handle(new ChangeRequestStatusCommand(
            _adminId, request.Id, VehicleRequestStatus.Annulee, null), CancellationToken.None))
            .Error.Should().Be("Admin.ReasonRequired");

        (await _status.Handle(new ChangeRequestStatusCommand(
            _adminId, request.Id, VehicleRequestStatus.Annulee, "Doublon"), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await _context.VehicleRequests.SingleAsync()).ClosedAt.Should().NotBeNull();
    }

    // ─── Propuestas internas ───────────────────────────────────────────────

    [Fact]
    public async Task AnexarUnAnuncioDebeAvisarAlUsuarioYMoverLaSolicitud()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);
        var vehicle = await ListingAsync();

        var result = await _internal.Handle(new AddInternalProposalCommand(
            _adminId, request.Id, vehicle.Id, "Correspond à vos critères."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        (await _context.VehicleRequests.SingleAsync()).Status
            .Should().Be(VehicleRequestStatus.VehiculePropose);

        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_userId);
        notification.Category.Should().Be(NotificationCategories.RequestProposal);
        notification.Title.Should().Be("Nous avons trouvé un véhicule pour vous");
    }

    [Fact]
    public async Task NoDebeProponerseUnAnuncioQueElUsuarioNoPuedeAbrir()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);
        await ListingAsync(VehicleStatus.Brouillon);

        var result = await _internal.Handle(new AddInternalProposalCommand(
            _adminId, request.Id, _vehicleId, null), CancellationToken.None);

        result.Error.Should().Be("Vehicle.NotAvailable");
    }

    [Fact]
    public async Task NoDebeProponerseUnAnuncioOcultadoPorModeracion()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);
        var vehicle = await ListingAsync();
        vehicle.AdminHiddenAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        var result = await _internal.Handle(new AddInternalProposalCommand(
            _adminId, request.Id, vehicle.Id, null), CancellationToken.None);

        result.Error.Should().Be("Vehicle.NotAvailable");
    }

    [Fact]
    public async Task ElMismoAnuncioNoDebeProponerseDosVeces()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);
        var vehicle = await ListingAsync();

        await _internal.Handle(new AddInternalProposalCommand(
            _adminId, request.Id, vehicle.Id, null), CancellationToken.None);

        var second = await _internal.Handle(new AddInternalProposalCommand(
            _adminId, request.Id, vehicle.Id, null), CancellationToken.None);

        second.Error.Should().Be("VehicleRequest.ProposalAlreadyExists");
    }

    // ─── Propuestas externas ───────────────────────────────────────────────

    [Fact]
    public async Task DeberiaGuardarseUnVehiculoEncontradoFuera()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);

        var result = await _external.Handle(new AddExternalProposalCommand(
            _adminId, request.Id, new ExternalProposalInput(
                "Toyota RAV4", "2.0 D-4D", 2018, 92_000,
                FuelType.Diesel, TransmissionType.Automatique,
                7_800_000m, 1_200_000m, "BE",
                ["https://exemple.be/photo1.jpg", "https://exemple.be/photo2.jpg"],
                "https://exemple.be/annonce", "Import Belgique, dédouanement inclus.")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var proposal = await _context.VehicleRequestProposals.SingleAsync();
        proposal.IsInternal.Should().BeFalse();
        proposal.MakeModel.Should().Be("Toyota RAV4");
        proposal.Version.Should().Be("2.0 D-4D");
        proposal.FuelType.Should().Be(FuelType.Diesel);
        // Los costes van aparte del precio: el usuario debe ver qué es cada cosa.
        proposal.EstimatedPrice.Should().Be(7_800_000m);
        proposal.AdditionalCosts.Should().Be(1_200_000m);

        var detail = await _detail.Handle(new GetAdminRequestQuery(request.Id), CancellationToken.None);
        detail.Value!.Proposals.Single().PhotoUrls.Should().HaveCount(2);
    }

    [Fact]
    public async Task UnaPropuestaExternaSinMarcaNoSirve()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);

        var result = await _external.Handle(new AddExternalProposalCommand(
            _adminId, request.Id, new ExternalProposalInput(
                "  ", null, null, null, null, null, null, null, null, null, null, null)),
            CancellationToken.None);

        result.Error.Should().Be("VehicleRequest.MakeModelRequired");
    }

    [Fact]
    public async Task RetirarUnaPropuestaDebeSacarlaDeLaFicha()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);
        var proposal = await _external.Handle(new AddExternalProposalCommand(
            _adminId, request.Id, new ExternalProposalInput(
                "Toyota RAV4", null, 2018, null, null, null, null, null, null, null, null, null)),
            CancellationToken.None);

        await _remove.Handle(new RemoveProposalCommand(_adminId, proposal.Value), CancellationToken.None);

        var detail = await _detail.Handle(new GetAdminRequestQuery(request.Id), CancellationToken.None);
        detail.Value!.Proposals.Should().BeEmpty();
        detail.Value.Actions.Should().Contain(a => a.Type == AdminActionType.RequestProposalRemoved);
    }

    // ─── Comunicación ──────────────────────────────────────────────────────

    [Fact]
    public async Task ResponderDebeLlegarleAlUsuario()
    {
        var request = await RequestAsync(VehicleRequestStatus.EnRecherche);

        var result = await _reply.Handle(new ReplyToRequestCommand(
            _adminId, request.Id, "Nous cherchons, deux pistes en cours."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var detail = await _detail.Handle(new GetAdminRequestQuery(request.Id), CancellationToken.None);
        var message = detail.Value!.Messages.Should().ContainSingle().Subject;
        message.FromAdmin.Should().BeTrue();
        message.Body.Should().Be("Nous cherchons, deux pistes en cours.");

        (await _context.UserNotifications.SingleAsync()).UserId.Should().Be(_userId);
    }

    public void Dispose() => _context.Dispose();
}
