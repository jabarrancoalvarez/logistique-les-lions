using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.UpcomingFeatures;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features;

/// <summary>
/// «Prochainement» y el botón «Ça m'intéresse».
/// </summary>
/// <remarks>
/// El documento no lo plantea como un escaparate sino como una encuesta: sirve para
/// «decidir qué servicio premium merece realmente desarrollarse». De ahí que lo que se
/// prueba sea el recuento por persona y la segmentación, no la lista.
/// </remarks>
public class UpcomingFeaturesTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetUpcomingFeaturesQueryHandler _list;
    private readonly SetFeatureInterestCommandHandler _interest;
    private readonly GetFeatureInterestReportQueryHandler _report;

    private readonly Guid _crmId = Guid.NewGuid();
    private readonly Guid _retiredId = Guid.NewGuid();
    private readonly Guid _proId = Guid.NewGuid();
    private readonly Guid _particulierId = Guid.NewGuid();

    public UpcomingFeaturesTests()
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

        _context.UpcomingFeatures.AddRange(
            new UpcomingFeature { Id = _crmId, Code = "CRM", Name = "CRM", DisplayOrder = 1 },
            new UpcomingFeature
            {
                Id = _retiredId, Code = "STOCK", Name = "Gestion de stock",
                DisplayOrder = 2, IsActive = false
            });

        _context.UserProfiles.AddRange(
            new UserProfile
            {
                Id = _proId, DisplayName = "Auto Dakar", Phone = "+221770000001",
                PasswordHash = "x", AccountType = AccountType.Professionnel, City = "Dakar"
            },
            new UserProfile
            {
                Id = _particulierId, DisplayName = "Mamadou", Phone = "+221770000002",
                PasswordHash = "x", AccountType = AccountType.Particulier, City = "Thiès"
            });
        _context.SaveChanges();

        _list     = new GetUpcomingFeaturesQueryHandler(_context);
        _interest = new SetFeatureInterestCommandHandler(_context);
        _report   = new GetFeatureInterestReportQueryHandler(_context);
    }

    private Task<LogistiqueLesLions.Application.Common.Models.Result<int>> MarkAsync(
        Guid userId, Guid featureId, bool interested = true) =>
        _interest.Handle(new SetFeatureInterestCommand(userId, featureId, interested),
            CancellationToken.None);

    // ─── Catálogo ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UnaFuncionalidadRetiradaNoDebeMostrarse()
    {
        var list = (await _list.Handle(
            new GetUpcomingFeaturesQuery(null), CancellationToken.None)).Value!;

        list.Items.Should().ContainSingle().Which.Code.Should().Be("CRM");
    }

    [Fact]
    public async Task SinCuentaSeVeLaListaPeroNadaMarcado()
    {
        await MarkAsync(_proId, _crmId);

        var list = (await _list.Handle(
            new GetUpcomingFeaturesQuery(null), CancellationToken.None)).Value!;

        var crm = list.Items.Single();
        crm.InterestedCount.Should().Be(1);
        crm.IsInterested.Should().BeFalse();
    }

    [Fact]
    public async Task ConCuentaSeVeLoQueUnoMismoMarco()
    {
        await MarkAsync(_proId, _crmId);

        var list = (await _list.Handle(
            new GetUpcomingFeaturesQuery(_proId), CancellationToken.None)).Value!;

        list.Items.Single().IsInterested.Should().BeTrue();
    }

    // ─── Ça m'intéresse ────────────────────────────────────────────────────

    [Fact]
    public async Task PulsarDosVecesNoDebeValerPorDos()
    {
        await MarkAsync(_proId, _crmId);
        var result = await MarkAsync(_proId, _crmId);

        result.Value.Should().Be(1);
        (await _context.FeatureInterests.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RetirarElInteresDebeDejarDeContar()
    {
        // Lo que mide esta pantalla es la demanda de hoy: quien cambia de idea deja
        // de contar, aunque su fila siga ahí.
        await MarkAsync(_proId, _crmId);
        var result = await MarkAsync(_proId, _crmId, interested: false);

        result.Value.Should().Be(0);
        (await _context.FeatureInterests.CountAsync()).Should().Be(0);
        (await _context.FeatureInterests.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DebePoderVolverADeclararseElInteresRetirado()
    {
        await MarkAsync(_proId, _crmId);
        await MarkAsync(_proId, _crmId, interested: false);
        var result = await MarkAsync(_proId, _crmId);

        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task NoSePuedeDeclararInteresPorAlgoRetirado()
    {
        (await MarkAsync(_proId, _retiredId)).Error.Should().Be("Feature.NotFound");
    }

    // ─── Lectura administrativa ────────────────────────────────────────────

    [Fact]
    public async Task ElRankingDebeIrDeLoMasPedidoALoMenos()
    {
        await MarkAsync(_proId, _crmId);
        await MarkAsync(_particulierId, _crmId);

        var report = (await _report.Handle(
            new GetFeatureInterestReportQuery(), CancellationToken.None)).Value!;

        report.Features[0].Code.Should().Be("CRM");
        report.Features[0].InterestedCount.Should().Be(2);
        // Lo retirado sigue en la lista del backoffice: su medición no se pierde.
        report.Features.Should().Contain(f => f.Code == "STOCK" && !f.IsActive);
        report.Segmentation.Should().BeNull();
    }

    [Fact]
    public async Task LaSegmentacionDebeSepararParticularDeProfesional()
    {
        await MarkAsync(_proId, _crmId);
        await MarkAsync(_particulierId, _crmId);

        var segmentation = (await _report.Handle(
            new GetFeatureInterestReportQuery(_crmId), CancellationToken.None))
            .Value!.Segmentation!;

        segmentation.Total.Should().Be(2);
        segmentation.Particuliers.Should().Be(1);
        segmentation.Professionnels.Should().Be(1);
        segmentation.ByCity.Should().Contain(c => c.Label == "Dakar" && c.Count == 1);
    }

    [Fact]
    public async Task LaSegmentacionPorActividadDebeDistinguirAlCuriosoDelQuePublica()
    {
        _context.Vehicles.AddRange(
            Vehicle("A", _proId), Vehicle("B", _proId), Vehicle("C", _proId));
        await _context.SaveChangesAsync();

        await MarkAsync(_proId, _crmId);
        await MarkAsync(_particulierId, _crmId);

        var segmentation = (await _report.Handle(
            new GetFeatureInterestReportQuery(_crmId), CancellationToken.None))
            .Value!.Segmentation!;

        segmentation.ByActivity.Should().Contain(a => a.Label == "2 à 5 annonces" && a.Count == 1);
        segmentation.ByActivity.Should().Contain(a => a.Label == "Aucune annonce" && a.Count == 1);
    }

    private Vehicle Vehicle(string slug, Guid sellerId) => new()
    {
        Title = "Annonce", Slug = slug, PublicReference = "YU" + Random.Shared.Next(10000, 99999),
        MakeId = Guid.NewGuid(), Year = 2018, Price = 5_000_000,
        SellerId = sellerId, Status = VehicleStatus.Actif
    };

    public void Dispose() => _context.Dispose();
}
