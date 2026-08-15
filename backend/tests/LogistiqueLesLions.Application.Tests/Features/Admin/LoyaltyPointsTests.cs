using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Users;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// Puntos de fidelización.
/// </summary>
/// <remarks>
/// El libro es lo importante: el saldo es una consecuencia. Aquí se comprueba que nunca
/// se mueve uno sin el otro, que un ajuste sin motivo no pasa, y que deshacer una venta
/// deshace también sus puntos.
/// </remarks>
public class LoyaltyPointsTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetUserPointsQueryHandler _get;
    private readonly AdjustUserPointsCommandHandler _adjust;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public LoyaltyPointsTests()
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
                Id = _userId, DisplayName = "Mamadou Diop", Phone = "+221770000001",
                PasswordHash = "x"
            });
        _context.SaveChanges();

        _get    = new GetUserPointsQueryHandler(_context);
        _adjust = new AdjustUserPointsCommandHandler(_context);
    }

    private Task<LogistiqueLesLions.Application.Common.Models.Result> AdjustAsync(
        int points, string reason = "geste commercial") =>
        _adjust.Handle(new AdjustUserPointsCommand(_adminId, _userId, points, reason),
            CancellationToken.None);

    // ─── Libro y saldo ─────────────────────────────────────────────────────

    [Fact]
    public async Task ElSaldoYElLibroDebenMoverseSiempreJuntos()
    {
        await AdjustAsync(50);

        (await _context.UserProfiles.SingleAsync(u => u.Id == _userId))
            .LoyaltyPoints.Should().Be(50);
        (await _context.LoyaltyPointEntries.SingleAsync()).Points.Should().Be(50);
    }

    [Fact]
    public async Task UnAjusteSinMotivoNoDebePasar()
    {
        // Un número que aparece de la nada en el saldo de alguien es justo lo que el
        // registro está para impedir.
        var result = await AdjustAsync(50, "   ");

        result.Error.Should().Be("Admin.ReasonRequired");
        (await _context.LoyaltyPointEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UnAjusteDeCeroNoDebeRegistrarse()
    {
        (await AdjustAsync(0)).Error.Should().Be("Points.ZeroAdjustment");
    }

    [Fact]
    public async Task UnAjusteDebeQuedarRegistradoConSaldoAnteriorYNuevo()
    {
        await AdjustAsync(50);
        await AdjustAsync(-20, "erreur de saisie");

        var actions = await _context.AdminActions
            .Where(a => a.Type == AdminActionType.PointsAdjusted)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        actions.Should().HaveCount(2);
        actions[1].OldValue.Should().Be("50");
        actions[1].NewValue.Should().Be("30");
        actions[1].Reason.Should().Be("erreur de saisie");
    }

    [Fact]
    public async Task ElUsuarioDebeEnterarseDeQueLePasaASusPuntos()
    {
        await AdjustAsync(50);

        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_userId);
        notification.Title.Should().Be("Points ajoutés");
        notification.Body.Should().Contain("+50");
    }

    [Fact]
    public async Task ElAjusteALaBajaDebeLlamarseComoLoQueEs()
    {
        await AdjustAsync(-30, "annulation");

        (await _context.UserNotifications.SingleAsync()).Title.Should().Be("Points retirés");
    }

    // ─── Consulta ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LaConsultaDebeDarSaldoOrigenFechaYMovimiento()
    {
        var user = await _context.UserProfiles.SingleAsync(u => u.Id == _userId);
        LoyaltyPointsService.Add(_context, user, 100, LoyaltyPointOrigin.VenteVerifiee,
            Guid.NewGuid(), "YC00125");
        await _context.SaveChangesAsync();

        await AdjustAsync(50);

        var points = (await _get.Handle(
            new GetUserPointsQuery(_userId), CancellationToken.None)).Value!;

        points.Balance.Should().Be(150);
        points.Entries.Should().HaveCount(2);

        var sale = points.Entries.Single(e => e.Origin == LoyaltyPointOrigin.VenteVerifiee);
        sale.Points.Should().Be(100);
        sale.ContractReference.Should().Be("YC00125");
        sale.At.Should().NotBe(default);

        var manual = points.Entries.Single(
            e => e.Origin == LoyaltyPointOrigin.AjustementAdministrateur);
        manual.AdminName.Should().Be("Admin");
        manual.Note.Should().Be("geste commercial");
    }

    [Fact]
    public async Task LaConsultaDebeOrdenarDelMasRecienteAlMasAntiguo()
    {
        await AdjustAsync(10, "premier");
        await Task.Delay(10);
        await AdjustAsync(20, "second");

        var points = (await _get.Handle(
            new GetUserPointsQuery(_userId), CancellationToken.None)).Value!;

        points.Entries[0].Note.Should().Be("second");
    }

    // ─── Servicio ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UnMovimientoDeCeroPuntosNoDebeEnsuciarElLibro()
    {
        var user = await _context.UserProfiles.SingleAsync(u => u.Id == _userId);

        var entry = LoyaltyPointsService.Add(
            _context, user, 0, LoyaltyPointOrigin.VenteVerifiee);

        entry.Should().BeNull();
        user.LoyaltyPoints.Should().Be(0);
    }

    [Fact]
    public async Task LosPuntosPorVentaDebenSalirDeLaConfiguracion()
    {
        _context.PlatformSettings.Add(new PlatformSettings
        {
            Id = PlatformSettings.SingletonId, PointsPerVerifiedSale = 250
        });
        await _context.SaveChangesAsync();

        (await LoyaltyPointsService.PointsPerSaleAsync(_context, CancellationToken.None))
            .Should().Be(250);
    }

    [Fact]
    public async Task SinConfiguracionLosPuntosPorVentaNoDebenSerCero()
    {
        // Una base sin la fila de configuración no puede dejar las ventas sin puntos
        // en silencio.
        (await LoyaltyPointsService.PointsPerSaleAsync(_context, CancellationToken.None))
            .Should().Be(100);
    }

    public void Dispose() => _context.Dispose();
}
