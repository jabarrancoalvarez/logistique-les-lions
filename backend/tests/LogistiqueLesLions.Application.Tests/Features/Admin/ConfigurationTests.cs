using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Configuration;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Configuration générale»: parámetros, interruptores, catálogos y journal.
/// </summary>
/// <remarks>
/// Lo que se comprueba es lo que el documento pide de verdad: que los valores se puedan
/// cambiar sin tocar código, que no se puedan poner valores que rompan la aplicación, y
/// que todo cambio deje constancia de qué había antes.
/// </remarks>
public class ConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetSettingsQueryHandler _get;
    private readonly UpdateSettingsCommandHandler _update;
    private readonly ToggleFeatureFlagCommandHandler _toggle;
    private readonly GetPublicSettingsQueryHandler _public;
    private readonly GetActivityLogQueryHandler _activity;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _flagId = Guid.NewGuid();

    public ConfigurationTests()
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

        _context.UserProfiles.Add(new UserProfile
        {
            Id = _adminId, DisplayName = "Admin", Phone = "+221770000000",
            PasswordHash = "x", Role = UserRole.Admin
        });

        // El proveedor en memoria no aplica los HasData de las configuraciones.
        _context.PlatformSettings.Add(new PlatformSettings { Id = PlatformSettings.SingletonId });
        _context.PriceIndicatorSettings.Add(
            new PriceIndicatorSettings { Id = PriceIndicatorSettings.SingletonId });
        _context.VehicleValuationSettings.Add(
            new VehicleValuationSettings { Id = VehicleValuationSettings.SingletonId });
        _context.FeatureFlags.Add(new FeatureFlag
        {
            Id = _flagId, Key = FeatureFlagKeys.PriceIndicator,
            Label = "Indicateur de prix", IsEnabled = true
        });
        _context.SaveChanges();

        _get      = new GetSettingsQueryHandler(_context);
        _update   = new UpdateSettingsCommandHandler(_context);
        _toggle   = new ToggleFeatureFlagCommandHandler(_context);
        _public   = new GetPublicSettingsQueryHandler(_context);
        _activity = new GetActivityLogQueryHandler(_context);
    }

    private async Task<LogistiqueLesLions.Application.Common.Models.Result> UpdateAsync(
        Action<Mutable> mutate)
    {
        var current = (await _get.Handle(new GetSettingsQuery(), CancellationToken.None)).Value!;

        var m = new Mutable
        {
            Platform = current.Platform,
            Price = current.PriceIndicator,
            Valuation = current.Valuation
        };
        mutate(m);

        return await _update.Handle(
            new UpdateSettingsCommand(_adminId, m.Platform, m.Price, m.Valuation),
            CancellationToken.None);
    }

    private sealed class Mutable
    {
        public PlatformSettingsDto Platform { get; set; } = null!;
        public PriceIndicatorSettingsDto Price { get; set; } = null!;
        public ValuationSettingsDto Valuation { get; set; } = null!;
    }

    // ─── Parámetros ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaDevolverLosTresBloquesYLosInterruptores()
    {
        var settings = (await _get.Handle(new GetSettingsQuery(), CancellationToken.None)).Value!;

        settings.Platform.ComparatorMaxVehicles.Should().Be(3);
        settings.Platform.PointsPerVerifiedSale.Should().Be(100);
        settings.PriceIndicator.MinComparables.Should().Be(5);
        settings.Valuation.MinComparables.Should().Be(5);
        settings.Flags.Should().ContainSingle();
    }

    [Fact]
    public async Task DeberiaPoderCambiarseUnParametroSinTocarCodigo()
    {
        var result = await UpdateAsync(m => m.Platform = m.Platform with { PointsPerVerifiedSale = 250 });

        result.IsSuccess.Should().BeTrue();
        (await _context.PlatformSettings.SingleAsync()).PointsPerVerifiedSale.Should().Be(250);
    }

    [Fact]
    public async Task UnComparadorDeVeinteCochesDebeRechazarse()
    {
        // Los rangos existen para que la configuración no pueda tumbar la aplicación.
        var result = await UpdateAsync(m => m.Platform = m.Platform with { ComparatorMaxVehicles = 20 });

        result.Error.Should().Be("Settings.ComparatorOutOfRange");
        (await _context.PlatformSettings.SingleAsync()).ComparatorMaxVehicles.Should().Be(3);
    }

    [Fact]
    public async Task UnMargenImposibleDebeRechazarse()
    {
        var result = await UpdateAsync(m => m.Price = m.Price with { GoodDealMargin = 3m });

        result.Error.Should().Be("Settings.MarginOutOfRange");
    }

    [Fact]
    public async Task CambiarUnParametroDebeDejarElValorAnteriorYElNuevo()
    {
        await UpdateAsync(m => m.Price = m.Price with { MinComparables = 8 });

        var action = await _context.AdminActions.SingleAsync();

        action.Type.Should().Be(AdminActionType.SettingsChanged);
        action.AdminId.Should().Be(_adminId);
        action.OldValue.Should().Contain("min=5");
        action.NewValue.Should().Contain("min=8");
    }

    [Fact]
    public async Task GuardarSinCambiarNadaNoDebeEnsuciarElJournal()
    {
        await UpdateAsync(_ => { });

        (await _context.AdminActions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LaFechaDeLasCondicionesSoloSeMueveAlCambiarLaVersion()
    {
        await UpdateAsync(m => m.Platform = m.Platform with { MaxImagesPerListing = 15 });
        (await _context.PlatformSettings.SingleAsync()).LegalTermsUpdatedAt.Should().BeNull();

        await UpdateAsync(m => m.Platform = m.Platform with { LegalTermsVersion = "2.0" });
        (await _context.PlatformSettings.SingleAsync()).LegalTermsUpdatedAt.Should().NotBeNull();
    }

    // ─── Interruptores ─────────────────────────────────────────────────────

    [Fact]
    public async Task ApagarUnaFuncionalidadDebeQuedarRegistrado()
    {
        var result = await _toggle.Handle(
            new ToggleFeatureFlagCommand(_adminId, _flagId, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.FeatureFlags.SingleAsync()).IsEnabled.Should().BeFalse();

        var action = await _context.AdminActions.SingleAsync();
        action.Type.Should().Be(AdminActionType.FeatureFlagToggled);
        action.OldValue.Should().Be("activé");
        action.NewValue.Should().Be("désactivé");
    }

    [Fact]
    public async Task ApagarLoQueYaEstabaApagadoNoDebeRegistrarNada()
    {
        await _toggle.Handle(
            new ToggleFeatureFlagCommand(_adminId, _flagId, true), CancellationToken.None);

        (await _context.AdminActions.CountAsync()).Should().Be(0);
    }

    // ─── Lectura pública ───────────────────────────────────────────────────

    [Fact]
    public async Task LosParametrosPublicosNoDebenFiltrarNadaInterno()
    {
        var settings = (await _public.Handle(
            new GetPublicSettingsQuery(), CancellationToken.None)).Value!;

        settings.ComparatorMaxVehicles.Should().Be(3);
        settings.Features[FeatureFlagKeys.PriceIndicator].Should().BeTrue();

        // Lo que el público no tiene por qué saber no viaja: el DTO ni siquiera lo lleva.
        typeof(PublicSettingsDto).GetProperty("PointsPerVerifiedSale").Should().BeNull();
        typeof(PublicSettingsDto).GetProperty("GoodDealMargin").Should().BeNull();
    }

    // ─── Journal d'activité ────────────────────────────────────────────────

    [Fact]
    public async Task ElJournalDebeOrdenarseDelMasRecienteAlMasAntiguo()
    {
        await UpdateAsync(m => m.Price = m.Price with { MinComparables = 6 });
        await Task.Delay(10);
        await _toggle.Handle(
            new ToggleFeatureFlagCommand(_adminId, _flagId, false), CancellationToken.None);

        var log = (await _activity.Handle(
            new GetActivityLogQuery(), CancellationToken.None)).Value!;

        log.TotalCount.Should().Be(2);
        log.Items[0].Type.Should().Be(AdminActionType.FeatureFlagToggled);
        log.Items[0].AdminName.Should().Be("Admin");
    }

    [Fact]
    public async Task ElJournalDebePoderFiltrarsePorTipoDeAccion()
    {
        await UpdateAsync(m => m.Price = m.Price with { MinComparables = 6 });
        await _toggle.Handle(
            new ToggleFeatureFlagCommand(_adminId, _flagId, false), CancellationToken.None);

        var log = (await _activity.Handle(
            new GetActivityLogQuery(Type: AdminActionType.SettingsChanged),
            CancellationToken.None)).Value!;

        log.TotalCount.Should().Be(1);
        log.Items.Should().OnlyContain(i => i.Type == AdminActionType.SettingsChanged);
    }

    [Fact]
    public async Task FiltrarHastaUnDiaDebeIncluirEseDia()
    {
        // Quien filtra «hasta el 8» espera ver lo del 8, no lo anterior a las 00:00.
        await UpdateAsync(m => m.Price = m.Price with { MinComparables = 6 });

        var today = DateTimeOffset.UtcNow.Date;

        var log = (await _activity.Handle(
            new GetActivityLogQuery(To: new DateTimeOffset(today, TimeSpan.Zero)),
            CancellationToken.None)).Value!;

        log.TotalCount.Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
