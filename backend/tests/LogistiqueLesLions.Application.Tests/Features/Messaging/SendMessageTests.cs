using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Messaging;

/// <summary>
/// Enviar un mensaje tiene que avisar a quien lo recibe.
/// </summary>
/// <remarks>
/// Probado en producción el 16/08/2026: un mensaje llegaba a la base de datos y ahí se
/// quedaba. El destinatario no veía nada en la pantalla abierta, ni le saltaba la campana,
/// y solo descubría el mensaje al volver a entrar. El aviso salía únicamente del hub, y la
/// pantalla de la negociación envía por REST.
/// </remarks>
public class SendMessageTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<INotificationPusher> _pusher = new();
    private readonly Mock<IChatPusher> _chat = new();
    private readonly SendMessageCommandHandler _send;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public SendMessageTests()
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

        var makeId = Guid.NewGuid();
        _context.VehicleMakes.Add(new VehicleMake { Id = makeId, Name = "Toyota", Country = "JP" });
        _context.UserProfiles.AddRange(
            new UserProfile { Id = _buyerId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000002", PasswordHash = "x" });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId, PublicReference = "YU10001", Slug = "rav4", Title = "Toyota RAV4",
            MakeId = makeId, Year = 2019, Price = 8_900_000m, SellerId = _sellerId,
            Status = VehicleStatus.Actif
        });
        _context.SaveChanges();

        _send = new SendMessageCommandHandler(_context, _pusher.Object, _chat.Object);
    }

    private Task Enviar(Guid de, Guid para, string cuerpo) =>
        _send.Handle(new SendMessageCommand(de, para, _vehicleId, cuerpo), CancellationToken.None);

    [Fact]
    public async Task DeberiaDejarNotificacionAlDestinatario()
    {
        await Enviar(_buyerId, _sellerId, "Bonjour, le véhicule est-il toujours disponible ?");

        var notificaciones = await _context.UserNotifications.ToListAsync();

        notificaciones.Should().ContainSingle();
        notificaciones[0].UserId.Should().Be(_sellerId, "avisa a quien recibe, no a quien escribe");
        notificaciones[0].Category.Should().Be(NotificationCategories.Message);
        notificaciones[0].Body.Should().Contain("toujours disponible");
    }

    [Fact]
    public async Task DeberiaEnlazarLaNotificacionConLaNegociacion()
    {
        await Enviar(_buyerId, _sellerId, "Bonjour");

        var negociacion = await _context.Negotiations.SingleAsync();
        var notificacion = await _context.UserNotifications.SingleAsync();

        notificacion.Link.Should().Be($"/mis-negociaciones/{negociacion.Id}",
            "la campana tiene que llevar al hilo, no al buzón que ya no existe");
    }

    [Fact]
    public async Task DeberiaEmpujarElMensajeAlDestinatario()
    {
        await Enviar(_sellerId, _buyerId, "Oui, toujours disponible.");

        var negociacion = await _context.Negotiations.SingleAsync();

        _chat.Verify(c => c.PushMessageAsync(
            It.Is<PushedChatMessage>(m =>
                m.RecipientId == _buyerId &&
                m.SenderId == _sellerId &&
                m.NegotiationId == negociacion.Id &&
                m.Body == "Oui, toujours disponible."),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeberiaResumirElCuerpoLargoSinPartirUnaPalabra()
    {
        var largo = string.Join(' ', Enumerable.Repeat("disponible", 40));

        await Enviar(_buyerId, _sellerId, largo);

        var cuerpo = (await _context.UserNotifications.SingleAsync()).Body!;

        cuerpo.Should().EndWith("…");
        cuerpo.Length.Should().BeLessThanOrEqualTo(121, "120 caracteres más los puntos suspensivos");
        cuerpo.TrimEnd('…').Should().EndWith("disponible", "el corte cae entre palabras");
    }

    [Fact]
    public async Task NoDeberiaAvisarSiElAnuncioYaNoAdmiteNegociacion()
    {
        var vendido = await _context.Vehicles.SingleAsync();
        vendido.Status = VehicleStatus.Vendu;
        await _context.SaveChangesAsync();

        await Enviar(_buyerId, _sellerId, "Encore disponible ?");

        (await _context.UserNotifications.AnyAsync()).Should().BeFalse();
        _chat.Verify(c => c.PushMessageAsync(It.IsAny<PushedChatMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    public void Dispose() => _context.Dispose();
}
