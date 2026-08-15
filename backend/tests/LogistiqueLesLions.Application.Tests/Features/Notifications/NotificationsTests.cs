using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Notifications.Commands.MarkNotificationRead;
using LogistiqueLesLions.Application.Features.Notifications.Queries.GetMyNotifications;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Notifications;

/// <summary>
/// La campana. Lo crítico aquí es el aislamiento: nadie debe ver ni marcar como leídas
/// las notificaciones de otro usuario.
/// </summary>
public class NotificationsTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Mock<ICurrentUser> _currentUser = new();

    public NotificationsTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var interceptorUser = new Mock<ICurrentUser>();
        interceptorUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        _context = new ApplicationDbContext(
            options,
            new Infrastructure.Persistence.Interceptors.AuditInterceptor(interceptorUser.Object),
            new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
                interceptorUser.Object, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        _currentUser.Setup(u => u.UserId).Returns(_userId);
    }

    private Guid AddNotification(Guid userId, string category = NotificationCategories.PriceDrop,
                                 bool isRead = false)
    {
        var notification = new UserNotification
        {
            UserId   = userId,
            Category = category,
            Title    = "Baisse de prix",
            Body     = "La Toyota Hilux que vous suivez est passée de 9.500.000 à 8.900.000 FCFA.",
            Link     = "/vehiculos/toyota-hilux",
            IsRead   = isRead
        };
        _context.UserNotifications.Add(notification);
        _context.SaveChanges();
        return notification.Id;
    }

    private Task<Result<NotificationListDto>> List(bool unreadOnly = false) =>
        new GetMyNotificationsQueryHandler(_context, _currentUser.Object)
            .Handle(new GetMyNotificationsQuery(unreadOnly), CancellationToken.None);

    [Fact]
    public async Task SoloDebeDevolverLasNotificacionesDelUsuario()
    {
        AddNotification(_userId);
        AddNotification(_userId);
        AddNotification(_otherUserId);

        var result = await List();

        result.Value!.Items.Should().HaveCount(2);
        result.Value!.UnreadCount.Should().Be(2);
    }

    [Fact]
    public async Task ElContadorNoDebeIncluirLasYaLeidas()
    {
        AddNotification(_userId, isRead: false);
        AddNotification(_userId, isRead: true);

        var result = await List();

        result.Value!.Items.Should().HaveCount(2);
        result.Value!.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task DeberiaPoderFiltrarseSoloLasNoLeidas()
    {
        AddNotification(_userId, isRead: false);
        AddNotification(_userId, isRead: true);

        var result = await List(unreadOnly: true);

        result.Value!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task DeberiaOrdenarLasMasRecientesPrimero()
    {
        var older = AddNotification(_userId);
        // El AuditInterceptor fija CreatedAt: se separa explícitamente en el tiempo.
        var olderEntity = await _context.UserNotifications.SingleAsync(n => n.Id == older);
        olderEntity.CreatedAt = DateTimeOffset.UtcNow.AddHours(-2);
        await _context.SaveChangesAsync();

        var newer = AddNotification(_userId);

        var result = await List();

        result.Value!.Items[0].Id.Should().Be(newer);
    }

    [Fact]
    public async Task MarcarComoLeidaDebeReducirElContador()
    {
        var id = AddNotification(_userId);

        var result = await new MarkNotificationReadCommandHandler(_context, _currentUser.Object)
            .Handle(new MarkNotificationReadCommand(id), CancellationToken.None);

        result.Value.Should().Be(1);
        (await List()).Value!.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task NoDebePoderMarcarseLaNotificacionDeOtroUsuario()
    {
        var id = AddNotification(_otherUserId);

        var result = await new MarkNotificationReadCommandHandler(_context, _currentUser.Object)
            .Handle(new MarkNotificationReadCommand(id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Notification.NotFound");

        var notification = await _context.UserNotifications.SingleAsync(n => n.Id == id);
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarcarTodasNoDebeTocarLasDeOtrosUsuarios()
    {
        AddNotification(_userId);
        AddNotification(_userId);
        var ajena = AddNotification(_otherUserId);

        var result = await new MarkNotificationReadCommandHandler(_context, _currentUser.Object)
            .Handle(new MarkNotificationReadCommand(null, All: true), CancellationToken.None);

        result.Value.Should().Be(2);

        var otherNotification = await _context.UserNotifications.SingleAsync(n => n.Id == ajena);
        otherNotification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task SinSesionNoDebeDevolverNada()
    {
        AddNotification(_userId);
        var anonymous = new Mock<ICurrentUser>();
        anonymous.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await new GetMyNotificationsQueryHandler(_context, anonymous.Object)
            .Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Auth.Required");
    }

    public void Dispose() => _context.Dispose();
}
