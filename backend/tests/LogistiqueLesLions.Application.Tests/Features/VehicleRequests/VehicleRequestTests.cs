using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.VehicleRequests;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.VehicleRequests;

public class VehicleRequestTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreateVehicleRequestCommandHandler _create;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();

    public VehicleRequestTests()
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
            new UserProfile { Id = _userId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _otherUserId, DisplayName = "Fatou", Phone = "+221770000002", PasswordHash = "x" },
            new UserProfile
            {
                Id = _adminId, DisplayName = "Admin", Phone = "+221770000003",
                PasswordHash = "x", Role = UserRole.Admin
            });
        _context.SaveChanges();

        // La secuencia de PostgreSQL no existe en InMemory: se simula.
        var counter = 248;
        var mockReferences = new Mock<IPublicReferenceGenerator>();
        mockReferences
            .Setup(r => r.NextRequestReferenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"YD{counter++:D5}");

        _create = new CreateVehicleRequestCommandHandler(_context, mockReferences.Object);
    }

    private CreateVehicleRequestCommand Command(Guid? userId = null) => new(
        UserId: userId ?? _userId,
        MakeId: _makeId,
        MakeName: "Toyota",
        ModelName: "Hilux",
        Version: null,
        YearFrom: 2018,
        YearTo: 2022,
        MaxMileage: 120_000,
        FuelType: FuelType.Diesel,
        Transmission: TransmissionType.Automatique,
        BodyType: BodyType.PickUp,
        Color: null,
        ImportantEquipment: "Double cabine",
        MaxBudget: 12_000_000m,
        Origin: VehicleRequestOrigin.Importation,
        Notes: "Je préfère un véhicule européen.");

    private async Task<Guid> CreateRequest(Guid? userId = null)
    {
        var result = await _create.Handle(Command(userId), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!.Id;
    }

    // ─── Creación ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaAsignarReferenciaYEstadoInicial()
    {
        var result = await _create.Handle(Command(), CancellationToken.None);

        result.Value!.PublicReference.Should().Be("YD00248");

        var entity = await _context.VehicleRequests.SingleAsync();
        entity.Status.Should().Be(VehicleRequestStatus.NouvelleDemande);
        entity.Origin.Should().Be(VehicleRequestOrigin.Importation);
        entity.MaxBudget.Should().Be(12_000_000m);
    }

    [Fact]
    public async Task DeberiaNotificarAlPanelDeAdministracion()
    {
        await CreateRequest();

        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_adminId);
        notification.Title.Should().Be("Nouvelle demande #YD00248");
        notification.Body.Should().Contain("Toyota Hilux");
    }

    [Fact]
    public async Task NoDeberiaNotificarAUsuariosNormales()
    {
        await CreateRequest();

        var recipients = await _context.UserNotifications.Select(n => n.UserId).ToListAsync();
        recipients.Should().NotContain(_userId).And.NotContain(_otherUserId);
    }

    [Fact]
    public async Task DeberiaLimitarLasSolicitudesAbiertasPorUsuario()
    {
        for (var i = 0; i < 10; i++) await CreateRequest();

        var result = await _create.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("VehicleRequest.TooManyOpen");
    }

    [Fact]
    public async Task LasSolicitudesCerradasNoDebenContarParaElLimite()
    {
        var ids = new List<Guid>();
        for (var i = 0; i < 10; i++) ids.Add(await CreateRequest());

        await new CancelVehicleRequestCommandHandler(_context)
            .Handle(new CancelVehicleRequestCommand(_userId, ids[0]), CancellationToken.None);

        var result = await _create.Handle(Command(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    // ─── Cancelación ───────────────────────────────────────────────────────

    [Fact]
    public async Task CancelarDebeConservarLaSolicitudEnElHistorico()
    {
        var id = await CreateRequest();

        var result = await new CancelVehicleRequestCommandHandler(_context)
            .Handle(new CancelVehicleRequestCommand(_userId, id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Sigue existiendo: cambia el estado, no se borra.
        var entity = await _context.VehicleRequests.SingleAsync(r => r.Id == id);
        entity.Status.Should().Be(VehicleRequestStatus.Annulee);
        entity.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task NoDebePoderCancelarseUnaSolicitudYaFinalizada()
    {
        var id = await CreateRequest();
        var entity = await _context.VehicleRequests.SingleAsync(r => r.Id == id);
        entity.Status = VehicleRequestStatus.Terminee;
        await _context.SaveChangesAsync();

        var result = await new CancelVehicleRequestCommandHandler(_context)
            .Handle(new CancelVehicleRequestCommand(_userId, id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("VehicleRequest.AlreadyClosed");
    }

    [Fact]
    public async Task NoDebePoderCancelarseLaSolicitudDeOtroUsuario()
    {
        var id = await CreateRequest(_userId);

        var result = await new CancelVehicleRequestCommandHandler(_context)
            .Handle(new CancelVehicleRequestCommand(_otherUserId, id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("VehicleRequest.NotFound");
    }

    // ─── Hilo con administración ───────────────────────────────────────────

    [Fact]
    public async Task LasNotasInternasNoDebenLlegarAlUsuario()
    {
        var id = await CreateRequest();

        _context.VehicleRequestMessages.AddRange(
            new VehicleRequestMessage
            {
                RequestId = id, IsFromAdmin = true, IsInternalNote = false,
                Body = "Nous avons trouvé deux Toyota Hilux."
            },
            new VehicleRequestMessage
            {
                RequestId = id, IsFromAdmin = true, IsInternalNote = true,
                Body = "Client difficile, marge réduite."
            });
        await _context.SaveChangesAsync();

        var result = await new GetVehicleRequestQueryHandler(_context)
            .Handle(new GetVehicleRequestQuery(_userId, id), CancellationToken.None);

        result.Value!.Messages.Should().ContainSingle();
        result.Value!.Messages[0].Body.Should().Contain("deux Toyota Hilux");
    }

    [Fact]
    public async Task NoDebePoderLeerseLaSolicitudDeOtroUsuario()
    {
        var id = await CreateRequest(_userId);

        var result = await new GetVehicleRequestQueryHandler(_context)
            .Handle(new GetVehicleRequestQuery(_otherUserId, id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("VehicleRequest.NotFound");
    }

    [Fact]
    public async Task NoDebePoderEscribirseEnLaSolicitudDeOtroUsuario()
    {
        var id = await CreateRequest(_userId);

        var result = await new AddVehicleRequestMessageCommandHandler(_context)
            .Handle(new AddVehicleRequestMessageCommand(_otherUserId, id, "Bonjour"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await _context.VehicleRequestMessages.CountAsync()).Should().Be(0);
    }

    // ─── Propuestas ────────────────────────────────────────────────────────

    [Fact]
    public async Task ElListadoDebeSeñalarLasPropuestasNoVistas()
    {
        var id = await CreateRequest();

        _context.VehicleRequestProposals.Add(new VehicleRequestProposal
        {
            RequestId = id, MakeModel = "Toyota Hilux", Year = 2020, IsSeenByUser = false
        });
        await _context.SaveChangesAsync();

        var result = await new GetMyVehicleRequestsQueryHandler(_context)
            .Handle(new GetMyVehicleRequestsQuery(_userId), CancellationToken.None);

        result.Value![0].ProposalsCount.Should().Be(1);
        result.Value![0].UnseenProposals.Should().Be(1);
    }

    [Fact]
    public async Task MarcarComoVistasDebeSilenciarElAviso()
    {
        var id = await CreateRequest();
        _context.VehicleRequestProposals.Add(new VehicleRequestProposal
        {
            RequestId = id, MakeModel = "Toyota Hilux", IsSeenByUser = false
        });
        await _context.SaveChangesAsync();

        await new MarkProposalsSeenCommandHandler(_context)
            .Handle(new MarkProposalsSeenCommand(_userId, id), CancellationToken.None);

        var result = await new GetMyVehicleRequestsQueryHandler(_context)
            .Handle(new GetMyVehicleRequestsQuery(_userId), CancellationToken.None);

        result.Value![0].UnseenProposals.Should().Be(0);
    }

    [Fact]
    public async Task NoDebePoderMarcarseLasPropuestasDeOtroUsuario()
    {
        var id = await CreateRequest(_userId);
        _context.VehicleRequestProposals.Add(new VehicleRequestProposal
        {
            RequestId = id, MakeModel = "Toyota Hilux", IsSeenByUser = false
        });
        await _context.SaveChangesAsync();

        await new MarkProposalsSeenCommandHandler(_context)
            .Handle(new MarkProposalsSeenCommand(_otherUserId, id), CancellationToken.None);

        var proposal = await _context.VehicleRequestProposals.SingleAsync();
        proposal.IsSeenByUser.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
