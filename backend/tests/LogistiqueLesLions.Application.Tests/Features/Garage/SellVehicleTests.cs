using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Garage;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Garage;

/// <summary>
/// «Vendre ce véhicule» y «Transparence du véhicule».
/// </summary>
/// <remarks>
/// Dos reglas gobiernan esta parte: <b>no se publica nada automáticamente</b> y
/// <b>nada del historial privado se comparte sin marcarlo expresamente</b>.
/// </remarks>
public class SellVehicleTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreateListingFromGarageCommandHandler _sell;
    private readonly GetTransparencySettingsQueryHandler _settings;
    private readonly SaveTransparencySettingsCommandHandler _save;
    private readonly GetVehicleTransparencyQueryHandler _public;
    private readonly GetSharedInvoiceQueryHandler _invoice;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _modelId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();
    private readonly Guid _documentId = Guid.NewGuid();

    private int _sequence;

    public SellVehicleTests()
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
        _context.VehicleModels.Add(new VehicleModel { Id = _modelId, MakeId = _makeId, Name = "RAV4" });
        _context.UserProfiles.AddRange(
            new UserProfile { Id = _userId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _otherUserId, DisplayName = "Fatou", Phone = "+221770000002", PasswordHash = "x" });

        _context.GarageVehicles.Add(new GarageVehicle
        {
            Id = _vehicleId, UserId = _userId, MakeId = _makeId, ModelId = _modelId,
            Year = 2019, Version = "2.0 D-4D", Mileage = 147_500,
            FuelType = FuelType.Diesel, Transmission = TransmissionType.Automatique,
            BodyType = BodyType.Suv, PowerCv = 150, EngineDisplacementCc = 1998,
            Color = "Gris", Vin = "JTMBFREV60D012345"
        });
        _context.GarageVehicleImages.AddRange(
            new GarageVehicleImage { GarageVehicleId = _vehicleId, StorageKey = "garage/a.webp", FileName = "a.webp", ContentType = "image/webp", IsPrimary = true },
            new GarageVehicleImage { GarageVehicleId = _vehicleId, StorageKey = "garage/b.webp", FileName = "b.webp", ContentType = "image/webp", SortOrder = 1 });
        _context.GarageDocuments.Add(new GarageDocument
        {
            Id = _documentId, GarageVehicleId = _vehicleId, Type = GarageDocumentType.FactureEntretien,
            Name = "Facture vidange", StorageKey = "garage/facture.pdf",
            FileName = "facture.pdf", ContentType = "application/pdf"
        });
        _context.SaveChanges();

        var references = new Mock<IPublicReferenceGenerator>();
        references
            .Setup(r => r.NextVehicleReferenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"YU{10000 + ++_sequence}");

        // Publicar copia cada foto privada al almacenamiento público: el doble devuelve
        // la URL que tendría la copia, para poder comprobar que el anuncio la recibe.
        var storage = new Mock<IStorageService>();
        storage
            .Setup(s => s.PublishPrivateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string fileName, string _, string folder, CancellationToken _) =>
                ($"/{folder}/{fileName}", $"/{folder}/thumb-{fileName}"));

        _sell     = new CreateListingFromGarageCommandHandler(
            _context, references.Object, storage.Object);
        _settings = new GetTransparencySettingsQueryHandler(_context);
        _save     = new SaveTransparencySettingsCommandHandler(_context);
        _public   = new GetVehicleTransparencyQueryHandler(_context);
        _invoice  = new GetSharedInvoiceQueryHandler(_context);
    }

    private async Task<Guid> AddMaintenanceAsync(
        string description, int mileage, Guid? documentId = null)
    {
        var record = new MaintenanceRecord
        {
            GarageVehicleId = _vehicleId,
            Type = MaintenanceType.Vidange,
            PerformedAt = DateTimeOffset.UtcNow.AddMonths(-mileage % 12 - 1),
            Mileage = mileage,
            Description = description,
            DocumentId = documentId
        };
        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync();
        return record.Id;
    }

    private async Task<CreateListingResultDto> SellAsync()
    {
        var result = await _sell.Handle(
            new CreateListingFromGarageCommand(_userId, _vehicleId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    // ─── Vendre ce véhicule ────────────────────────────────────────────────

    [Fact]
    public async Task DebeCrearUnBorradorConLosDatosDelGaraje()
    {
        var result = await SellAsync();

        var listing = await _context.Vehicles.SingleAsync(v => v.Id == result.VehicleId);

        // ⚠️ Nace en borrador: no se publica nada automáticamente.
        listing.Status.Should().Be(VehicleStatus.Brouillon);
        listing.SellerId.Should().Be(_userId);
        listing.Title.Should().Be("Toyota RAV4 2.0 D-4D 2019");
        listing.MakeId.Should().Be(_makeId);
        listing.ModelId.Should().Be(_modelId);
        listing.Year.Should().Be(2019);
        listing.Mileage.Should().Be(147_500);
        listing.FuelType.Should().Be(FuelType.Diesel);
        listing.Transmission.Should().Be(TransmissionType.Automatique);
        listing.BodyType.Should().Be(BodyType.Suv);
        listing.PowerCv.Should().Be(150);
        listing.EngineDisplacementCc.Should().Be(1998);
        listing.Color.Should().Be("Gris");
        listing.Vin.Should().Be("JTMBFREV60D012345");
    }

    [Fact]
    public async Task NoDebeInventarseElPrecioNiElEstadoAduanero()
    {
        var result = await SellAsync();

        var listing = await _context.Vehicles.SingleAsync(v => v.Id == result.VehicleId);

        // Son las decisiones que el documento pide revisar expresamente.
        listing.Price.Should().Be(0m);
        listing.CustomsStatus.Should().BeNull();
        listing.Description.Should().BeNull();
    }

    [Fact]
    public async Task DebeCopiarLasFotografiasDelGarajeAlAnuncio()
    {
        var result = await SellAsync();

        var images = await _context.VehicleImages
            .Where(i => i.VehicleId == result.VehicleId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();

        images.Should().HaveCount(2);
        // La principal del garaje sigue siendo la principal del anuncio.
        images[0].IsPrimary.Should().BeTrue();
        images[1].IsPrimary.Should().BeFalse();
        result.CopiedImages.Should().Be(2);

        // Las del garaje son privadas: el anuncio recibe una copia en el almacenamiento
        // público, no la misma referencia.
        images[0].Url.Should().Be($"/vehicles/{result.VehicleId}/a.webp");
        images[0].Url.Should().NotContain("garage/");
    }

    [Fact]
    public async Task NoDebeVaciarElGarajeAlPublicar()
    {
        var result = await SellAsync();

        // Se copia, no se mueve: el vehículo conserva sus fotografías después de
        // ponerse a la venta.
        var enElGaraje = await _context.GarageVehicleImages
            .Where(i => i.GarageVehicleId == _vehicleId)
            .ToListAsync();

        enElGaraje.Should().HaveCount(2);
        enElGaraje.Should().OnlyContain(i => i.StorageKey.StartsWith("garage/"));
        result.CopiedImages.Should().Be(2);
    }

    [Fact]
    public async Task DebeEnlazarElAnuncioConLaFichaDelGaraje()
    {
        var result = await SellAsync();

        (await _context.GarageVehicles.SingleAsync(v => v.Id == _vehicleId))
            .ListedVehicleId.Should().Be(result.VehicleId);
    }

    [Fact]
    public async Task UnCocheNoPuedeEstarDosVecesALaVenta()
    {
        await SellAsync();

        var second = await _sell.Handle(
            new CreateListingFromGarageCommand(_userId, _vehicleId), CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.Error.Should().Be("GarageVehicle.AlreadyListed");
    }

    [Fact]
    public async Task TrasVenderloDebePoderVolverAPonerseALaVenta()
    {
        var first = await SellAsync();

        var listing = await _context.Vehicles.SingleAsync(v => v.Id == first.VehicleId);
        listing.Status = VehicleStatus.Vendu;
        await _context.SaveChangesAsync();

        var second = await _sell.Handle(
            new CreateListingFromGarageCommand(_userId, _vehicleId), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task NadieMasDebePonerALaVentaElCocheDeOtro()
    {
        var result = await _sell.Handle(
            new CreateListingFromGarageCommand(_otherUserId, _vehicleId), CancellationToken.None);

        result.Error.Should().Be("GarageVehicle.AccessDenied");
    }

    // ─── Transparence du véhicule ──────────────────────────────────────────

    [Fact]
    public async Task LaTransparenciaDebeNacerApagada()
    {
        var result = await SellAsync();

        var transparency = await _context.VehicleTransparencies.SingleAsync();
        transparency.ShowMaintenanceHistory.Should().BeFalse();
        transparency.ShowMaintenanceDetails.Should().BeFalse();
        transparency.ShowMileageEvolution.Should().BeFalse();

        // Y el anuncio no enseña nada del historial privado.
        var visible = await _public.Handle(
            new GetVehicleTransparencyQuery(result.VehicleId), CancellationToken.None);
        visible.Value.Should().BeNull();
    }

    [Fact]
    public async Task SoloDebeEnsenarseLoQueSeHaMarcado()
    {
        var shared = await AddMaintenanceAsync("Vidange", 145_000, _documentId);
        var hidden = await AddMaintenanceAsync("Réparation carrosserie", 130_000);
        var result = await SellAsync();

        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, true, false,
            [new SharedRecordInput(shared, true, false)]), CancellationToken.None);

        var visible = await _public.Handle(
            new GetVehicleTransparencyQuery(result.VehicleId), CancellationToken.None);

        visible.Value!.Records.Should().ContainSingle()
            .Which.Description.Should().Be("Vidange");
        visible.Value.Records.Should().NotContain(r => r.Description.Contains("carrosserie"));
        // El contador refleja el historial entero, aunque solo se detalle una.
        visible.Value.MaintenanceCount.Should().Be(2);
        hidden.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PuedeEnsenarseElHistorialSinFechasNiKilometraje()
    {
        var record = await AddMaintenanceAsync("Vidange", 145_000);
        var result = await SellAsync();

        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, false, false,
            [new SharedRecordInput(record, true, false)]), CancellationToken.None);

        var visible = await _public.Handle(
            new GetVehicleTransparencyQuery(result.VehicleId), CancellationToken.None);

        var shown = visible.Value!.Records.Single();
        shown.Description.Should().Be("Vidange");
        shown.PerformedAt.Should().BeNull();
        shown.Mileage.Should().BeNull();
    }

    [Fact]
    public async Task DesmarcarDebeDejarDeCompartirDeVerdad()
    {
        var record = await AddMaintenanceAsync("Vidange", 145_000, _documentId);
        var result = await SellAsync();

        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, true, false,
            [new SharedRecordInput(record, true, true)]), CancellationToken.None);

        // La factura se comparte…
        (await _invoice.Handle(new GetSharedInvoiceQuery(result.VehicleId, _documentId),
            CancellationToken.None)).IsSuccess.Should().BeTrue();

        // …y se retira el permiso.
        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, true, false, []), CancellationToken.None);

        (await _public.Handle(new GetVehicleTransparencyQuery(result.VehicleId),
            CancellationToken.None)).Value!.Records.Should().BeEmpty();

        (await _invoice.Handle(new GetSharedInvoiceQuery(result.VehicleId, _documentId),
            CancellationToken.None)).Error.Should().Be("Transparency.NotShared");
    }

    [Fact]
    public async Task CompartirLaIntervencionNoCompartLaFactura()
    {
        var record = await AddMaintenanceAsync("Vidange", 145_000, _documentId);
        var result = await SellAsync();

        // Se enseña que se hizo la revisión, pero no el papel.
        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, true, false,
            [new SharedRecordInput(record, true, false)]), CancellationToken.None);

        var visible = await _public.Handle(
            new GetVehicleTransparencyQuery(result.VehicleId), CancellationToken.None);
        visible.Value!.Records.Single().InvoiceDocumentId.Should().BeNull();

        var invoice = await _invoice.Handle(
            new GetSharedInvoiceQuery(result.VehicleId, _documentId), CancellationToken.None);
        invoice.Error.Should().Be("Transparency.NotShared");
    }

    [Fact]
    public async Task ApagarElHistorialDebeCerrarTambienLasFacturas()
    {
        var record = await AddMaintenanceAsync("Vidange", 145_000, _documentId);
        var result = await SellAsync();

        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, true, false,
            [new SharedRecordInput(record, true, true)]), CancellationToken.None);

        // El interruptor general manda sobre las selecciones concretas.
        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, false, true, false,
            [new SharedRecordInput(record, true, true)]), CancellationToken.None);

        (await _public.Handle(new GetVehicleTransparencyQuery(result.VehicleId),
            CancellationToken.None)).Value.Should().BeNull();

        (await _invoice.Handle(new GetSharedInvoiceQuery(result.VehicleId, _documentId),
            CancellationToken.None)).Error.Should().Be("Transparency.NotShared");
    }

    [Fact]
    public async Task LaEvolucionDelKilometrajeSaleDeLasLecturasRegistradas()
    {
        var first = await AddMaintenanceAsync("Vidange 2024", 130_000);
        var second = await AddMaintenanceAsync("Vidange 2025", 145_000);
        var result = await SellAsync();

        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, true, true,
            [new SharedRecordInput(first, true, false), new SharedRecordInput(second, true, false)]),
            CancellationToken.None);

        var visible = await _public.Handle(
            new GetVehicleTransparencyQuery(result.VehicleId), CancellationToken.None);

        visible.Value!.MileageEvolution.Should().HaveCount(2);
        visible.Value.MileageEvolution.Select(p => p.Mileage)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task NoDebePoderCompartirseElHistorialDeOtroVehiculo()
    {
        var result = await SellAsync();

        // Una intervención que no es de este coche se ignora al guardar.
        var alien = Guid.NewGuid();
        await _save.Handle(new SaveTransparencySettingsCommand(
            _userId, result.VehicleId, true, true, false,
            [new SharedRecordInput(alien, true, true)]), CancellationToken.None);

        (await _context.SharedMaintenanceRecords.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task NadieMasDebeConfigurarLaTransparencia()
    {
        var result = await SellAsync();

        (await _settings.Handle(new GetTransparencySettingsQuery(_otherUserId, result.VehicleId),
            CancellationToken.None)).Error.Should().Be("GarageVehicle.AccessDenied");

        (await _save.Handle(new SaveTransparencySettingsCommand(
            _otherUserId, result.VehicleId, true, true, true, []), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");
    }

    public void Dispose() => _context.Dispose();
}
