using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Communications;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Notifications et communications» del backoffice.
/// </summary>
/// <remarks>
/// El MVP se queda corto a propósito: avisos, mantenimiento, información importante y
/// soporte individual. Lo que sí exige el documento es que el histórico registre qué se
/// envió, cuándo y a quién.
/// </remarks>
public class CommunicationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly SendCommunicationCommandHandler _send;
    private readonly GetCommunicationsQueryHandler _list;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _particulierId = Guid.NewGuid();
    private readonly Guid _proId = Guid.NewGuid();
    private readonly Guid _blockedId = Guid.NewGuid();

    public CommunicationTests()
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

        _context.UserProfiles.AddRange(
            new UserProfile
            {
                Id = _adminId, DisplayName = "Admin", Phone = "+221770000000",
                PasswordHash = "x", Role = UserRole.Admin
            },
            new UserProfile
            {
                Id = _particulierId, DisplayName = "Mamadou Diop", Phone = "+221770000001",
                Email = "mamadou@example.sn", PasswordHash = "x",
                AccountType = AccountType.Particulier, Region = "DK"
            },
            new UserProfile
            {
                // Sin correo: el correo es opcional en Yoon u Auto.
                Id = _proId, DisplayName = "Auto Dakar", Phone = "+221770000002",
                PasswordHash = "x", AccountType = AccountType.Professionnel, Region = "TH"
            },
            new UserProfile
            {
                Id = _blockedId, DisplayName = "Bloqué", Phone = "+221770000003",
                Email = "bloque@example.sn", PasswordHash = "x",
                AccountType = AccountType.Particulier, Status = AccountStatus.Blocked
            });
        _context.SaveChanges();

        _send = new SendCommunicationCommandHandler(_context, _emailSender.Object);
        _list = new GetCommunicationsQueryHandler(_context);
    }

    private Task<LogistiqueLesLions.Application.Common.Models.Result<SendCommunicationResultDto>> SendAsync(
        CommunicationAudience audience = CommunicationAudience.Tous,
        Guid? targetUserId = null,
        string? region = null,
        bool byEmail = false,
        string title = "Maintenance programmée",
        string body = "La plateforme sera indisponible dimanche de 2h à 4h.") =>
        _send.Handle(new SendCommunicationCommand(
            _adminId, CommunicationType.Maintenance, audience, targetUserId, region,
            title, body, byEmail), CancellationToken.None);

    // ─── Envío ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaLlegarACadaDestinatarioComoNotificacion()
    {
        var result = await SendAsync();

        result.IsSuccess.Should().BeTrue();
        // Admin, particulier y professionnel: el bloqueado no.
        result.Value!.RecipientCount.Should().Be(3);

        var notifications = await _context.UserNotifications.ToListAsync();
        notifications.Should().HaveCount(3);
        notifications.Should().OnlyContain(n => n.Category == NotificationCategories.System);
        notifications.Select(n => n.UserId).Should().NotContain(_blockedId);
    }

    [Fact]
    public async Task UnaCuentaBloqueadaNoDebeRecibirAvisos()
    {
        // No es para quien ya no puede entrar.
        await SendAsync();

        (await _context.UserNotifications.AnyAsync(n => n.UserId == _blockedId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task DeberiaPoderDirigirseSoloAProfesionales()
    {
        var result = await SendAsync(CommunicationAudience.Professionnels);

        result.Value!.RecipientCount.Should().Be(1);
        (await _context.UserNotifications.SingleAsync()).UserId.Should().Be(_proId);
    }

    [Fact]
    public async Task DeberiaPoderAcotarsePorRegion()
    {
        var result = await SendAsync(region: "DK");

        result.Value!.RecipientCount.Should().Be(1);
        (await _context.UserNotifications.SingleAsync()).UserId.Should().Be(_particulierId);
    }

    [Fact]
    public async Task LaComunicacionIndividualExigeDestinatario()
    {
        var result = await SendAsync(CommunicationAudience.Individuel);

        result.Error.Should().Be("Communication.TargetRequired");
    }

    [Fact]
    public async Task LaComunicacionIndividualDebeLlegarSoloAEsaPersona()
    {
        var result = await SendAsync(CommunicationAudience.Individuel, _particulierId);

        result.Value!.RecipientCount.Should().Be(1);
        (await _context.UserNotifications.SingleAsync()).UserId.Should().Be(_particulierId);
        (await _context.Communications.SingleAsync()).TargetUserId.Should().Be(_particulierId);
    }

    [Fact]
    public async Task SinTituloOSinCuerpoNoSeEnviaNada()
    {
        (await SendAsync(title: "   ")).Error.Should().Be("Communication.TitleRequired");
        (await SendAsync(body: "  ")).Error.Should().Be("Communication.BodyRequired");

        (await _context.UserNotifications.CountAsync()).Should().Be(0);
        (await _context.Communications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SinDestinatariosNoSeGuardaLaComunicacion()
    {
        var result = await SendAsync(region: "ZZ");

        result.Error.Should().Be("Communication.NoRecipients");
        (await _context.Communications.CountAsync()).Should().Be(0);
    }

    // ─── Canal de correo ───────────────────────────────────────────────────

    [Fact]
    public async Task ElCorreoSoloDebeIrAQuienLoTiene()
    {
        var result = await SendAsync(byEmail: true);

        // Tres destinatarios, pero solo el admin y el particulier tienen correo… el
        // admin de este test no lo tiene, así que queda uno.
        result.Value!.RecipientCount.Should().Be(3);
        result.Value.EmailsSent.Should().Be(1);

        _emailSender.Verify(s => s.SendAsync(
            It.Is<EmailMessage>(m => m.To == "mamadou@example.sn"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SinPedirCorreoNoDebeEnviarseNinguno()
    {
        await SendAsync(byEmail: false);

        _emailSender.Verify(s => s.SendAsync(
            It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);

        (await _context.Communications.SingleAsync()).EmailsSent.Should().Be(0);
    }

    // ─── Histórico ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ElHistoricoDebeRegistrarQueSeEnvioCuandoYAQuienes()
    {
        await SendAsync(CommunicationAudience.Professionnels, byEmail: true);

        var result = await _list.Handle(new GetCommunicationsQuery(), CancellationToken.None);

        var row = result.Value!.Items.Should().ContainSingle().Subject;
        row.Title.Should().Be("Maintenance programmée");
        row.Audience.Should().Be(CommunicationAudience.Professionnels);
        row.RecipientCount.Should().Be(1);
        row.SentByEmail.Should().BeTrue();
        // El profesional no tiene correo: se pidió enviarlo, pero no salió ninguno.
        row.EmailsSent.Should().Be(0);
        row.AdminName.Should().Be("Admin");
        row.SentAt.Should().NotBe(default);
    }

    [Fact]
    public async Task ElHistoricoDebeOrdenarseDelMasRecienteAlMasAntiguo()
    {
        await SendAsync(title: "Premier avis");
        await Task.Delay(10);
        await SendAsync(title: "Deuxième avis");

        var result = await _list.Handle(new GetCommunicationsQuery(), CancellationToken.None);

        result.Value!.Items[0].Title.Should().Be("Deuxième avis");
    }

    public void Dispose() => _context.Dispose();
}
