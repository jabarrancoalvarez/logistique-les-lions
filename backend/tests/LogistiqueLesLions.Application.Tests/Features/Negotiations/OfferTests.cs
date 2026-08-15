using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Negotiations;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Negotiations;

/// <summary>
/// Ofertas y contraofertas. Lo delicado aquí es que solo haya una oferta viva y que
/// responda siempre la otra parte.
/// </summary>
public class OfferTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MakeOfferCommandHandler _make;
    private readonly CounterOfferCommandHandler _counter;
    private readonly AcceptOfferCommandHandler _accept;
    private readonly RejectOfferCommandHandler _reject;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public OfferTests()
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
            new UserProfile { Id = _buyerId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000002", PasswordHash = "x" },
            new UserProfile { Id = _strangerId, DisplayName = "Fatou", Phone = "+221770000003", PasswordHash = "x" });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId,
            PublicReference = "YU10001",
            Slug = "toyota-rav4-yu10001",
            Title = "Toyota RAV4 2019",
            MakeId = _makeId,
            Year = 2019,
            Price = 8_900_000m,
            SellerId = _sellerId,
            Status = VehicleStatus.Actif
        });
        _context.SaveChanges();

        _make = new MakeOfferCommandHandler(_context);
        _counter = new CounterOfferCommandHandler(_context);
        _accept = new AcceptOfferCommandHandler(_context);
        _reject = new RejectOfferCommandHandler(_context);
    }

    private async Task<(Guid negotiationId, Guid offerId)> MakeInitialOffer(decimal amount = 8_300_000m)
    {
        var result = await _make.Handle(
            new MakeOfferCommand(_buyerId, _vehicleId, amount, "Bonjour"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return (result.Value!.NegotiationId, result.Value!.OfferId!.Value);
    }

    // ─── Crear la oferta ───────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaCrearLaNegociacionYRegistrarLaOferta()
    {
        var (negotiationId, offerId) = await MakeInitialOffer();

        var offer = await _context.Offers.SingleAsync(o => o.Id == offerId);
        offer.Amount.Should().Be(8_300_000m);
        // Se congela el precio publicado para poder comparar después.
        offer.ListedPrice.Should().Be(8_900_000m);
        offer.Status.Should().Be(OfferStatus.EnAttente);

        var negotiation = await _context.Negotiations.SingleAsync(n => n.Id == negotiationId);
        // Mientras espera respuesta, la negociación queda a la espera.
        negotiation.Status.Should().Be(NegotiationStatus.EnAttente);
    }

    [Fact]
    public async Task SinImporteNoDebeRegistrarseNingunaOferta()
    {
        // La especificación marca el importe como opcional: el formulario sirve entonces
        // solo para iniciar la conversación.
        var result = await _make.Handle(
            new MakeOfferCommand(_buyerId, _vehicleId, null, "Bonjour, est-il disponible ?"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OfferId.Should().BeNull();
        (await _context.Offers.CountAsync()).Should().Be(0);
        (await _context.Messages.CountAsync()).Should().Be(1);
        (await _context.Negotiations.SingleAsync()).Status.Should().Be(NegotiationStatus.EnCours);
    }

    [Fact]
    public async Task NoDebePoderOfertarseSobreElPropioAnuncio()
    {
        var result = await _make.Handle(
            new MakeOfferCommand(_sellerId, _vehicleId, 8_000_000m, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Offer.CannotOfferOnOwnVehicle");
    }

    [Fact]
    public async Task NoDebePoderOfertarseSobreUnVehiculoVendido()
    {
        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Status = VehicleStatus.Vendu;
        await _context.SaveChangesAsync();

        var result = await _make.Handle(
            new MakeOfferCommand(_buyerId, _vehicleId, 8_000_000m, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Vehicle.NotOpenForNegotiation");
    }

    [Fact]
    public async Task DeberiaRechazarUnImporteNoPositivo()
    {
        var result = await _make.Handle(
            new MakeOfferCommand(_buyerId, _vehicleId, 0m, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Offer.InvalidAmount");
    }

    [Fact]
    public async Task DeberiaAvisarAlVendedor()
    {
        await MakeInitialOffer();

        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_sellerId);
        notification.Title.Should().Be("Nouvelle offre");
        notification.Body.Should().Contain("8.300.000");
    }

    // ─── Contraoferta ──────────────────────────────────────────────────────

    [Fact]
    public async Task LaContraofertaDebeSuperarLaOfertaAnterior()
    {
        var (negotiationId, offerId) = await MakeInitialOffer();

        var result = await _counter.Handle(
            new CounterOfferCommand(_sellerId, negotiationId, 8_600_000m, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var previous = await _context.Offers.SingleAsync(o => o.Id == offerId);
        previous.Status.Should().Be(OfferStatus.ContreOfferte);

        var counter = await _context.Offers.SingleAsync(o => o.Id == result.Value);
        counter.Status.Should().Be(OfferStatus.EnAttente);
        counter.RepliesToOfferId.Should().Be(offerId);
    }

    [Fact]
    public async Task SoloDebeHaberUnaOfertaVivaALaVez()
    {
        var (negotiationId, _) = await MakeInitialOffer();
        await _counter.Handle(
            new CounterOfferCommand(_sellerId, negotiationId, 8_600_000m, null), CancellationToken.None);
        await _counter.Handle(
            new CounterOfferCommand(_buyerId, negotiationId, 8_450_000m, null), CancellationToken.None);

        var pending = await _context.Offers.CountAsync(o => o.Status == OfferStatus.EnAttente);
        pending.Should().Be(1);
    }

    [Fact]
    public async Task NoDebePoderContraofertarseLaPropiaOferta()
    {
        var (negotiationId, _) = await MakeInitialOffer();

        var result = await _counter.Handle(
            new CounterOfferCommand(_buyerId, negotiationId, 8_400_000m, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Offer.AlreadyYourTurn");
    }

    [Fact]
    public async Task UnTerceroNoDebePoderContraofertar()
    {
        var (negotiationId, _) = await MakeInitialOffer();

        var result = await _counter.Handle(
            new CounterOfferCommand(_strangerId, negotiationId, 8_600_000m, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Negotiation.AccessDenied");
    }

    // ─── Aceptar y rechazar ────────────────────────────────────────────────

    [Fact]
    public async Task AceptarDebeCerrarLaOfertaYDevolverLaNegociacionAEnCurso()
    {
        var (negotiationId, offerId) = await MakeInitialOffer();

        var result = await _accept.Handle(
            new AcceptOfferCommand(_sellerId, offerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var offer = await _context.Offers.SingleAsync();
        offer.Status.Should().Be(OfferStatus.Acceptee);
        offer.RespondedAt.Should().NotBeNull();

        (await _context.Negotiations.SingleAsync(n => n.Id == negotiationId))
            .Status.Should().Be(NegotiationStatus.EnCours);
    }

    [Fact]
    public async Task NoDebePoderAceptarseLaPropiaOferta()
    {
        var (_, offerId) = await MakeInitialOffer();

        var result = await _accept.Handle(
            new AcceptOfferCommand(_buyerId, offerId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Offer.CannotRespondToOwn");
    }

    [Fact]
    public async Task NoDebePoderResponderseDosVecesALaMismaOferta()
    {
        var (_, offerId) = await MakeInitialOffer();
        await _accept.Handle(new AcceptOfferCommand(_sellerId, offerId), CancellationToken.None);

        var result = await _reject.Handle(
            new RejectOfferCommand(_sellerId, offerId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Offer.NotPending");
    }

    [Fact]
    public async Task RechazarNoDebeCerrarLaNegociacion()
    {
        var (negotiationId, offerId) = await MakeInitialOffer();

        await _reject.Handle(new RejectOfferCommand(_sellerId, offerId), CancellationToken.None);

        var negotiation = await _context.Negotiations.SingleAsync(n => n.Id == negotiationId);
        negotiation.Status.Should().Be(NegotiationStatus.EnCours);
        negotiation.ClosedAt.Should().BeNull();
    }

    // ─── Cronología ────────────────────────────────────────────────────────

    [Fact]
    public async Task LaCronologiaDebeRecogerTodaLaSecuencia()
    {
        var (negotiationId, offerId) = await MakeInitialOffer();
        await _counter.Handle(
            new CounterOfferCommand(_sellerId, negotiationId, 8_600_000m, null), CancellationToken.None);

        var counterId = await _context.Offers
            .Where(o => o.Status == OfferStatus.EnAttente).Select(o => o.Id).SingleAsync();
        await _accept.Handle(new AcceptOfferCommand(_buyerId, counterId), CancellationToken.None);

        // Se ordena por Sequence: los hitos de una misma operación comparten CreatedAt.
        var events = await _context.NegotiationEvents
            .OrderBy(e => e.Sequence).ToListAsync();

        events.Select(e => e.Type).Should().ContainInOrder(
            NegotiationEventType.ConversationStarted,
            NegotiationEventType.OfferMade,
            NegotiationEventType.CounterOffer,
            NegotiationEventType.OfferAccepted);

        events.Single(e => e.Type == NegotiationEventType.CounterOffer)
            .Amount.Should().Be(8_600_000m);
    }

    public void Dispose() => _context.Dispose();
}
