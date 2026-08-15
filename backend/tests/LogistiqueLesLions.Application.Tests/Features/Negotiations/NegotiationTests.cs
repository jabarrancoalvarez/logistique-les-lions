using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;
using LogistiqueLesLions.Application.Features.Negotiations;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Negotiations;

/// <summary>
/// La negociación es el agregado raíz de la Etapa 2: nace del primer contacto y agrupa
/// conversación, ofertas y contrato.
/// </summary>
public class NegotiationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SendMessageCommandHandler _send;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public NegotiationTests()
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
            new UserProfile { Id = _buyerId, DisplayName = "Mamadou Diop", Phone = "+221770000001", PasswordHash = "x" },
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

        _send = new SendMessageCommandHandler(_context);
    }

    private Task<Result<Guid>> Send(Guid senderId, Guid recipientId, string body = "Bonjour") =>
        _send.Handle(new SendMessageCommand(senderId, recipientId, _vehicleId, body), CancellationToken.None);

    [Fact]
    public async Task ElPrimerMensajeDebeCrearLaNegociacion()
    {
        var result = await Send(_buyerId, _sellerId);

        result.IsSuccess.Should().BeTrue();

        var negotiation = await _context.Negotiations.SingleAsync();
        negotiation.BuyerId.Should().Be(_buyerId);
        negotiation.SellerId.Should().Be(_sellerId);
        negotiation.VehicleId.Should().Be(_vehicleId);
        negotiation.Status.Should().Be(NegotiationStatus.EnCours);
    }

    [Fact]
    public async Task DeberiaAbrirLaCronologiaConElHitoDeApertura()
    {
        await Send(_buyerId, _sellerId);

        var events = await _context.NegotiationEvents.ToListAsync();
        events.Should().ContainSingle();
        events[0].Type.Should().Be(NegotiationEventType.ConversationStarted);
        events[0].ActorId.Should().Be(_buyerId);
    }

    [Fact]
    public async Task LosMensajesSiguientesNoDebenCrearOtraNegociacion()
    {
        await Send(_buyerId, _sellerId);
        await Send(_sellerId, _buyerId, "Bonjour, oui il est disponible.");
        await Send(_buyerId, _sellerId, "Quel est le kilométrage ?");

        (await _context.Negotiations.CountAsync()).Should().Be(1);
        (await _context.Messages.CountAsync()).Should().Be(3);
        // La cronología no se repite: solo hay un hito de apertura.
        (await _context.NegotiationEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeberiaFuncionarIgualSiEmpiezaElVendedor()
    {
        await Send(_sellerId, _buyerId, "Bonjour, ce véhicule vous intéresse ?");

        var negotiation = await _context.Negotiations.SingleAsync();
        negotiation.BuyerId.Should().Be(_buyerId);
        negotiation.SellerId.Should().Be(_sellerId);
    }

    [Fact]
    public async Task UnVehiculoVendidoNoDebeAdmitirNuevosContactos()
    {
        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Status = VehicleStatus.Vendu;
        await _context.SaveChangesAsync();

        var result = await Send(_buyerId, _sellerId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Vehicle.NotOpenForNegotiation");
    }

    [Fact]
    public async Task UnMensajeDebeReabrirUnaNegociacionEnEspera()
    {
        await Send(_buyerId, _sellerId);
        var negotiation = await _context.Negotiations.SingleAsync();
        negotiation.Status = NegotiationStatus.EnAttente;
        await _context.SaveChangesAsync();

        await Send(_sellerId, _buyerId, "Toujours intéressé ?");

        (await _context.Negotiations.SingleAsync()).Status.Should().Be(NegotiationStatus.EnCours);
    }

    [Fact]
    public async Task SoloLasDosPartesDebenPoderAbrirLaNegociacion()
    {
        await Send(_buyerId, _sellerId);
        var id = (await _context.Negotiations.SingleAsync()).Id;
        var handler = new GetNegotiationQueryHandler(_context);

        (await handler.Handle(new GetNegotiationQuery(_buyerId, id), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        (await handler.Handle(new GetNegotiationQuery(_sellerId, id), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        var denied = await handler.Handle(
            new GetNegotiationQuery(_strangerId, id), CancellationToken.None);
        denied.IsSuccess.Should().BeFalse();
        denied.Error.Should().Be("Negotiation.AccessDenied");
    }

    [Fact]
    public async Task ElListadoDebePoderFiltrarsePorEstado()
    {
        await Send(_buyerId, _sellerId);
        var handler = new GetMyNegotiationsQueryHandler(_context);

        var enCours = await handler.Handle(
            new GetMyNegotiationsQuery(_buyerId, NegotiationStatus.EnCours), CancellationToken.None);
        enCours.Value!.Should().ContainSingle();

        var terminees = await handler.Handle(
            new GetMyNegotiationsQuery(_buyerId, NegotiationStatus.Terminee), CancellationToken.None);
        terminees.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task ElListadoDebeIndicarQueParteEsCadaUno()
    {
        await Send(_buyerId, _sellerId);
        var handler = new GetMyNegotiationsQueryHandler(_context);

        var asBuyer = await handler.Handle(new GetMyNegotiationsQuery(_buyerId), CancellationToken.None);
        asBuyer.Value![0].IsBuyer.Should().BeTrue();
        asBuyer.Value![0].OtherUserName.Should().Be("Auto Dakar");

        var asSeller = await handler.Handle(new GetMyNegotiationsQuery(_sellerId), CancellationToken.None);
        asSeller.Value![0].IsBuyer.Should().BeFalse();
        asSeller.Value![0].OtherUserName.Should().Be("Mamadou Diop");
    }

    [Fact]
    public async Task ElListadoDebeContarLosMensajesSinLeer()
    {
        await Send(_buyerId, _sellerId, "Bonjour");
        await Send(_buyerId, _sellerId, "Toujours disponible ?");

        var handler = new GetMyNegotiationsQueryHandler(_context);

        // Para el vendedor los dos están sin leer; para quien los escribió, ninguno.
        var seller = await handler.Handle(new GetMyNegotiationsQuery(_sellerId), CancellationToken.None);
        seller.Value![0].UnreadCount.Should().Be(2);

        var buyer = await handler.Handle(new GetMyNegotiationsQuery(_buyerId), CancellationToken.None);
        buyer.Value![0].UnreadCount.Should().Be(0);
    }

    public void Dispose() => _context.Dispose();
}
