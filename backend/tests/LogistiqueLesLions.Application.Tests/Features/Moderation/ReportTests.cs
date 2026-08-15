using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Listings;
using LogistiqueLesLions.Application.Features.Moderation;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Moderation;

/// <summary>
/// Modération: signalements y su tratamiento.
/// </summary>
/// <remarks>
/// La moderación es un módulo propio: un mismo reporte puede señalar un anuncio, a una
/// persona o una conversación, y todos acaban en la misma bandeja. Todas las acciones
/// importantes quedan registradas.
/// </remarks>
public class ReportTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreateReportCommandHandler _create;
    private readonly GetReportsQueryHandler _list;
    private readonly GetReportQueryHandler _detail;
    private readonly ChangeReportStatusCommandHandler _status;
    private readonly WarnReportedUserCommandHandler _warn;
    private readonly RequestReportInfoCommandHandler _requestInfo;
    private readonly GetAdminListingsQueryHandler _listings;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _reporterId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    private int _sequence;

    public ReportTests()
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
                Id = _reporterId, DisplayName = "Mamadou Diop", Phone = "+221770000001", PasswordHash = "x"
            },
            new UserProfile
            {
                Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000002", PasswordHash = "x"
            });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId, PublicReference = "YU10001", Slug = "toyota-rav4-yu10001",
            Title = "Toyota RAV4 2019", MakeId = _makeId, Year = 2019, Price = 8_900_000m,
            SellerId = _sellerId, Status = VehicleStatus.Actif,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-3)
        });
        _context.SaveChanges();

        var references = new Mock<IPublicReferenceGenerator>();
        references
            .Setup(r => r.NextReportReferenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"SG{++_sequence:D5}");

        _create      = new CreateReportCommandHandler(_context, references.Object);
        _list        = new GetReportsQueryHandler(_context);
        _detail      = new GetReportQueryHandler(_context);
        _status      = new ChangeReportStatusCommandHandler(_context);
        _warn        = new WarnReportedUserCommandHandler(_context);
        _requestInfo = new RequestReportInfoCommandHandler(_context);
        _listings    = new GetAdminListingsQueryHandler(_context);
    }

    private Task<LogistiqueLesLions.Application.Common.Models.Result<string>> ReportListingAsync(
        ReportReason reason = ReportReason.InformationFausse, Guid? reporterId = null) =>
        _create.Handle(new CreateReportCommand(
            reporterId ?? _reporterId, ReportTargetType.Listing, _vehicleId, reason,
            "Le kilométrage annoncé ne correspond pas.", null), CancellationToken.None);

    // ─── Crear el reporte ──────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaGuardarElReporteConSuReferencia()
    {
        var result = await ReportListingAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("SG00001");

        var report = await _context.Reports.SingleAsync();
        report.Status.Should().Be(ReportStatus.Nouveau);
        report.Reason.Should().Be(ReportReason.InformationFausse);
        // Se resuelve a quién se señala en el momento de reportar.
        report.ReportedUserId.Should().Be(_sellerId);
    }

    [Fact]
    public async Task NadieDebePoderReportarseASiMismo()
    {
        var result = await ReportListingAsync(reporterId: _sellerId);

        result.Error.Should().Be("Report.CannotReportSelf");
    }

    [Fact]
    public async Task ElMismoUsuarioNoDebeAbrirDosReportesAbiertosSobreLoMismo()
    {
        await ReportListingAsync();

        var second = await ReportListingAsync();

        // Sería ruido en la bandeja, no más información.
        second.Error.Should().Be("Report.AlreadyReported");
    }

    [Fact]
    public async Task TrasCerrarloDebePoderVolverAReportarse()
    {
        await ReportListingAsync();
        var report = await _context.Reports.SingleAsync();

        await _status.Handle(new ChangeReportStatusCommand(
            _adminId, report.Id, ReportStatus.Rejete, "Sans fondement"), CancellationToken.None);

        var second = await ReportListingAsync();
        second.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SoloLasPartesDebenPoderReportarSuConversacion()
    {
        var negotiation = new Negotiation
        {
            BuyerId = _reporterId, SellerId = _sellerId, VehicleId = _vehicleId,
            Status = NegotiationStatus.EnCours
        };
        _context.Negotiations.Add(negotiation);
        await _context.SaveChangesAsync();

        // Un tercero no tiene nada que reportar de una conversación que no es suya.
        var stranger = await _create.Handle(new CreateReportCommand(
            _adminId, ReportTargetType.Negotiation, negotiation.Id,
            ReportReason.ComportementInapproprie, null, null), CancellationToken.None);

        stranger.Error.Should().Be("Negotiation.AccessDenied");

        // Y quien participa señala a la otra parte.
        var party = await _create.Handle(new CreateReportCommand(
            _reporterId, ReportTargetType.Negotiation, negotiation.Id,
            ReportReason.ComportementInapproprie, "Propos déplacés", null), CancellationToken.None);

        party.IsSuccess.Should().BeTrue();
        (await _context.Reports.SingleAsync()).ReportedUserId.Should().Be(_sellerId);
    }

    // ─── Bandeja ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoAbiertoDebeIrPrimeroEnLaBandeja()
    {
        await ReportListingAsync();
        var closed = await _context.Reports.SingleAsync();
        await _status.Handle(new ChangeReportStatusCommand(
            _adminId, closed.Id, ReportStatus.Resolu, "Corrigé"), CancellationToken.None);

        await _create.Handle(new CreateReportCommand(
            _reporterId, ReportTargetType.User, _sellerId, ReportReason.Spam, null, null),
            CancellationToken.None);

        var result = await _list.Handle(new GetReportsQuery(), CancellationToken.None);

        result.Value!.Items[0].Status.Should().Be(ReportStatus.Nouveau);
        result.Value.CountByStatus[ReportStatus.Resolu].Should().Be(1);
    }

    [Fact]
    public async Task LaBandejaDebeDecirQueSeEstaReportando()
    {
        await ReportListingAsync();

        var result = await _list.Handle(new GetReportsQuery(), CancellationToken.None);

        var row = result.Value!.Items.Single();
        row.TargetLabel.Should().Contain("Toyota RAV4");
        row.TargetLabel.Should().Contain("YU10001");
        row.ReporterName.Should().Be("Mamadou Diop");
        row.ReportedUserName.Should().Be("Auto Dakar");
    }

    // ─── Tratamiento ───────────────────────────────────────────────────────

    [Fact]
    public async Task CerrarUnReporteExigeExplicarLaDecision()
    {
        await ReportListingAsync();
        var report = await _context.Reports.SingleAsync();

        foreach (var status in new[] { ReportStatus.Resolu, ReportStatus.Rejete })
        {
            (await _status.Handle(new ChangeReportStatusCommand(
                _adminId, report.Id, status, "  "), CancellationToken.None))
                .Error.Should().Be("Report.ResolutionRequired");
        }

        // Pasarlo a examen no: todavía no se ha decidido nada.
        (await _status.Handle(new ChangeReportStatusCommand(
            _adminId, report.Id, ReportStatus.EnExamen, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CerrarloDebeDejarQuienYCuando()
    {
        await ReportListingAsync();
        var report = await _context.Reports.SingleAsync();

        await _status.Handle(new ChangeReportStatusCommand(
            _adminId, report.Id, ReportStatus.Resolu, "Annonce corrigée par le vendeur"),
            CancellationToken.None);

        var detail = await _detail.Handle(new GetReportQuery(report.Id), CancellationToken.None);

        detail.Value!.Resolution.Should().Be("Annonce corrigée par le vendeur");
        detail.Value.ResolvedAt.Should().NotBeNull();
        detail.Value.HandledByAdminName.Should().Be("Admin");
        detail.Value.Actions.Should().ContainSingle()
            .Which.Type.Should().Be(AdminActionType.ReportResolved);
    }

    [Fact]
    public async Task AdvertirDebeAvisarAlSenaladoYQuedarEnSuFicha()
    {
        await ReportListingAsync();
        var report = await _context.Reports.SingleAsync();

        var result = await _warn.Handle(new WarnReportedUserCommand(
            _adminId, report.Id, "Vos annonces doivent refléter l'état réel du véhicule."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_sellerId);
        notification.Title.Should().Be("Avertissement");

        // Queda tanto en el reporte como en la ficha de quien la recibe.
        var actions = await _context.AdminActions.ToListAsync();
        actions.Should().HaveCount(2);
        actions.Should().Contain(a => a.TargetType == AdminTargetType.User && a.TargetId == _sellerId);
        actions.Should().Contain(a => a.TargetType == AdminTargetType.Report);
    }

    [Fact]
    public async Task PedirInformacionDebeMoverElReporteAExamen()
    {
        await ReportListingAsync();
        var report = await _context.Reports.SingleAsync();

        await _requestInfo.Handle(new RequestReportInfoCommand(
            _adminId, report.Id, "Pouvez-vous préciser ce qui ne correspond pas ?"),
            CancellationToken.None);

        // Alguien lo está mirando.
        (await _context.Reports.SingleAsync()).Status.Should().Be(ReportStatus.EnExamen);

        // Y la petición va a quien reportó, no al señalado.
        (await _context.UserNotifications.SingleAsync()).UserId.Should().Be(_reporterId);
    }

    // ─── Integración con el resto del backoffice ───────────────────────────

    [Fact]
    public async Task UnAnuncioConReportesAbiertosDebeAparecerComoReportado()
    {
        await ReportListingAsync();

        var reported = await _listings.Handle(
            new GetAdminListingsQuery(Reported: true), CancellationToken.None);
        reported.Value!.Items.Should().ContainSingle()
            .Which.OpenReports.Should().Be(1);

        var notReported = await _listings.Handle(
            new GetAdminListingsQuery(Reported: false), CancellationToken.None);
        notReported.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UnReporteResueltoNoDebeDejarElAnuncioMarcadoParaSiempre()
    {
        await ReportListingAsync();
        var report = await _context.Reports.SingleAsync();

        await _status.Handle(new ChangeReportStatusCommand(
            _adminId, report.Id, ReportStatus.Resolu, "Corrigé"), CancellationToken.None);

        var reported = await _listings.Handle(
            new GetAdminListingsQuery(Reported: true), CancellationToken.None);

        reported.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LaFichaDebeAvisarDeOtrosReportesAbiertosSobreLoMismo()
    {
        await ReportListingAsync();
        // Otra persona reporta el mismo anuncio.
        await ReportListingAsync(ReportReason.PrixTrompeur, _adminId);

        var first = await _context.Reports.OrderBy(r => r.PublicReference).FirstAsync();
        var detail = await _detail.Handle(new GetReportQuery(first.Id), CancellationToken.None);

        detail.Value!.OtherOpenReports.Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
