using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Negotiations;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Negotiations;

/// <summary>
/// Checklist privada de inspección. Lo esencial aquí es que sea realmente privada:
/// la otra parte no debe poder verla ni sobrescribirla.
/// </summary>
public class InspectionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetMyInspectionQueryHandler _get;
    private readonly SaveInspectionCommandHandler _save;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();
    private readonly Guid _negotiationId = Guid.NewGuid();

    public InspectionTests()
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
        var vehicleId = Guid.NewGuid();

        _context.VehicleMakes.Add(new VehicleMake { Id = makeId, Name = "Toyota", Country = "JP" });
        _context.UserProfiles.AddRange(
            new UserProfile { Id = _buyerId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000002", PasswordHash = "x" },
            new UserProfile { Id = _strangerId, DisplayName = "Fatou", Phone = "+221770000003", PasswordHash = "x" });
        _context.Vehicles.Add(new Vehicle
        {
            Id = vehicleId, PublicReference = "YU10001", Slug = "rav4", Title = "Toyota RAV4",
            MakeId = makeId, Year = 2019, Price = 8_900_000m, SellerId = _sellerId,
            Status = VehicleStatus.Actif
        });
        _context.Negotiations.Add(new Negotiation
        {
            Id = _negotiationId, BuyerId = _buyerId, SellerId = _sellerId,
            VehicleId = vehicleId, Status = NegotiationStatus.EnCours
        });
        _context.SaveChanges();

        _get = new GetMyInspectionQueryHandler(_context);
        _save = new SaveInspectionCommandHandler(_context);
    }

    private Task<Result> Save(Guid userId, params InspectionItemDto[] items) =>
        _save.Handle(new SaveInspectionCommand(
            userId, _negotiationId, DateTimeOffset.UtcNow, 126_000, "Bon état général", items),
            CancellationToken.None);

    [Fact]
    public async Task DeberiaDevolverSiempreLosOncePuntosDeLaEspecificacion()
    {
        var result = await _get.Handle(
            new GetMyInspectionQuery(_buyerId, _negotiationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(Enum.GetValues<InspectionItemType>().Length);
        // Sin rellenar, ninguno tiene valoración.
        result.Value!.Items.Should().OnlyContain(i => i.Result == null);
    }

    [Fact]
    public async Task DeberiaGuardarYRecuperarLaChecklist()
    {
        await Save(_buyerId,
            new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Bon, "Démarrage correct"),
            new InspectionItemDto(InspectionItemType.Pneus, InspectionResult.Mauvais, "À changer"));

        var result = await _get.Handle(
            new GetMyInspectionQuery(_buyerId, _negotiationId), CancellationToken.None);

        var moteur = result.Value!.Items.Single(i => i.Type == InspectionItemType.Moteur);
        moteur.Result.Should().Be(InspectionResult.Bon);
        moteur.Notes.Should().Be("Démarrage correct");

        result.Value!.ObservedMileage.Should().Be(126_000);
        result.Value!.Notes.Should().Be("Bon état général");
    }

    [Fact]
    public async Task NoDebePersistirLosPuntosSinContenido()
    {
        // Un formulario en blanco no debe generar once filas vacías.
        await Save(_buyerId,
            new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Bon, null),
            new InspectionItemDto(InspectionItemType.Freins, null, null),
            new InspectionItemDto(InspectionItemType.Vin, null, "   "));

        (await _context.VehicleInspectionItems.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GuardarDeNuevoDebeActualizarEnLugarDeDuplicar()
    {
        await Save(_buyerId, new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Bon, null));
        await Save(_buyerId, new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Mauvais, "Fuite"));

        (await _context.VehicleInspections.CountAsync()).Should().Be(1);
        var item = await _context.VehicleInspectionItems.SingleAsync();
        item.Result.Should().Be(InspectionResult.Mauvais);
        item.Notes.Should().Be("Fuite");
    }

    // ─── Privacidad ────────────────────────────────────────────────────────

    [Fact]
    public async Task LaOtraParteNoDebeVerLaChecklistAjena()
    {
        await Save(_buyerId,
            new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Mauvais, "Fuite d'huile"));

        // El vendedor participa en la negociación, pero su checklist es la suya y
        // aparece vacía: no ve nada de lo que escribió el comprador.
        var result = await _get.Handle(
            new GetMyInspectionQuery(_sellerId, _negotiationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().BeNull();
        result.Value!.Notes.Should().BeNull();
        result.Value!.Items.Should().OnlyContain(i => i.Result == null && i.Notes == null);
    }

    [Fact]
    public async Task CadaParteDebePoderTenerLaSuyaSinPisarLaOtra()
    {
        await Save(_buyerId, new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Mauvais, null));
        await Save(_sellerId, new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Bon, null));

        (await _context.VehicleInspections.CountAsync()).Should().Be(2);

        var buyerView = await _get.Handle(
            new GetMyInspectionQuery(_buyerId, _negotiationId), CancellationToken.None);
        buyerView.Value!.Items.Single(i => i.Type == InspectionItemType.Moteur)
            .Result.Should().Be(InspectionResult.Mauvais);
    }

    [Fact]
    public async Task UnTerceroNoDebePoderLeerla()
    {
        var result = await _get.Handle(
            new GetMyInspectionQuery(_strangerId, _negotiationId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Negotiation.AccessDenied");
    }

    [Fact]
    public async Task UnTerceroNoDebePoderEscribirla()
    {
        var result = await Save(_strangerId,
            new InspectionItemDto(InspectionItemType.Moteur, InspectionResult.Bon, null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Negotiation.AccessDenied");
        (await _context.VehicleInspections.CountAsync()).Should().Be(0);
    }

    public void Dispose() => _context.Dispose();
}
