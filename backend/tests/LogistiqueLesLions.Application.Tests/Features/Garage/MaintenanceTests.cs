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
/// Entretien: historial de mantenimiento de un vehículo de Mon Garage.
/// </summary>
/// <remarks>
/// El historial se construye a mano y con el tiempo se convierte en el registro real del
/// vehículo, así que lo importante es que se ordene bien y que nadie más lo toque.
/// </remarks>
public class MaintenanceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStorageService> _storage = new();

    private readonly AddMaintenanceRecordCommandHandler _add;
    private readonly UpdateMaintenanceRecordCommandHandler _update;
    private readonly DeleteMaintenanceRecordCommandHandler _delete;
    private readonly AddMaintenanceImageCommandHandler _addImage;
    private readonly DeleteMaintenanceImageCommandHandler _deleteImage;
    private readonly GetMaintenanceHistoryQueryHandler _history;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();
    private readonly Guid _otherVehicleId = Guid.NewGuid();
    private readonly Guid _documentId = Guid.NewGuid();
    private readonly Guid _otherDocumentId = Guid.NewGuid();

    public MaintenanceTests()
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
            new UserProfile { Id = _userId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _otherUserId, DisplayName = "Fatou", Phone = "+221770000002", PasswordHash = "x" });
        _context.GarageVehicles.AddRange(
            new GarageVehicle { Id = _vehicleId, UserId = _userId, MakeId = _makeId, Year = 2019, Mileage = 138_400 },
            new GarageVehicle { Id = _otherVehicleId, UserId = _userId, MakeId = _makeId, Year = 2016 });
        _context.GarageDocuments.AddRange(
            new GarageDocument
            {
                Id = _documentId, GarageVehicleId = _vehicleId, Type = GarageDocumentType.FactureEntretien,
                Name = "Facture vidange", StorageKey = "k1", FileName = "f.pdf", ContentType = "application/pdf"
            },
            new GarageDocument
            {
                Id = _otherDocumentId, GarageVehicleId = _otherVehicleId, Type = GarageDocumentType.Autre,
                Name = "Autre", StorageKey = "k2", FileName = "g.pdf", ContentType = "application/pdf"
            });
        _context.SaveChanges();

        _add         = new AddMaintenanceRecordCommandHandler(_context);
        _update      = new UpdateMaintenanceRecordCommandHandler(_context);
        _delete      = new DeleteMaintenanceRecordCommandHandler(_context, _storage.Object);
        _addImage    = new AddMaintenanceImageCommandHandler(_context);
        _deleteImage = new DeleteMaintenanceImageCommandHandler(_context, _storage.Object);
        _history     = new GetMaintenanceHistoryQueryHandler(_context);
    }

    private static DateTimeOffset On(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private MaintenanceInput Input(
        MaintenanceType type = MaintenanceType.Vidange,
        DateTimeOffset? performedAt = null,
        int? mileage = 145_320,
        string description = "Vidange + filtre à huile",
        decimal? cost = 35_000m,
        Guid? documentId = null) =>
        new(type, performedAt ?? On(2026, 6, 12), mileage, description,
            cost, "Garage Ndiaye", null, documentId);

    private async Task<Guid> AddAsync(MaintenanceInput? input = null, Guid? vehicleId = null)
    {
        var result = await _add.Handle(
            new AddMaintenanceRecordCommand(_userId, vehicleId ?? _vehicleId, input ?? Input()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    // ─── Registrar ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaRegistrarLaIntervencion()
    {
        var id = await AddAsync();

        var record = await _context.MaintenanceRecords.SingleAsync(r => r.Id == id);
        record.Type.Should().Be(MaintenanceType.Vidange);
        record.Description.Should().Be("Vidange + filtre à huile");
        record.Mileage.Should().Be(145_320);
        record.Cost.Should().Be(35_000m);
        record.Workshop.Should().Be("Garage Ndiaye");
        record.HasInvoice.Should().BeFalse();
    }

    [Fact]
    public async Task LaDescripcionEsObligatoria()
    {
        var result = await _add.Handle(
            new AddMaintenanceRecordCommand(_userId, _vehicleId, Input(description: "  ")),
            CancellationToken.None);

        result.Error.Should().Be("Maintenance.DescriptionRequired");
    }

    [Fact]
    public async Task NoDebeRegistrarseUnaIntervencionFutura()
    {
        // Lo que está por venir es un rappel, no una intervención hecha.
        var result = await _add.Handle(
            new AddMaintenanceRecordCommand(_userId, _vehicleId,
                Input(performedAt: DateTimeOffset.UtcNow.AddMonths(2))),
            CancellationToken.None);

        result.Error.Should().Be("Maintenance.DateInFuture");
    }

    [Fact]
    public async Task LaFacturaEnlazadaDebeSerDelMismoVehiculo()
    {
        var result = await _add.Handle(
            new AddMaintenanceRecordCommand(_userId, _vehicleId,
                Input(documentId: _otherDocumentId)),
            CancellationToken.None);

        result.Error.Should().Be("GarageDocument.NotFound");
    }

    [Fact]
    public async Task ConFacturaEnlazadaDebeMarcarseComoDisponible()
    {
        await AddAsync(Input(documentId: _documentId));

        var result = await _history.Handle(
            new GetMaintenanceHistoryQuery(_userId, _vehicleId), CancellationToken.None);

        var record = result.Value!.Years.Single().Records.Single();
        record.HasInvoice.Should().BeTrue();
        record.DocumentId.Should().Be(_documentId);
    }

    // ─── Kilometraje ───────────────────────────────────────────────────────

    [Fact]
    public async Task UnaIntervencionMasRecienteDebePonerAlDiaElKilometraje()
    {
        // Es la lectura más reciente que tenemos del vehículo.
        await AddAsync(Input(mileage: 145_320));

        (await _context.GarageVehicles.SingleAsync(v => v.Id == _vehicleId))
            .Mileage.Should().Be(145_320);
    }

    [Fact]
    public async Task UnaIntervencionAntiguaNoDebeHacerRetrocederElKilometraje()
    {
        await AddAsync(Input(performedAt: On(2023, 1, 10), mileage: 90_000));

        (await _context.GarageVehicles.SingleAsync(v => v.Id == _vehicleId))
            .Mileage.Should().Be(138_400);
    }

    // ─── Historial ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ElHistorialDebeAgruparsePorAnoDeMasRecienteAMasAntiguo()
    {
        await AddAsync(Input(MaintenanceType.Vidange, On(2026, 6, 12), 145_320, "Vidange"));
        await AddAsync(Input(MaintenanceType.Pneus, On(2026, 1, 20), 138_400, "Pneus avant"));
        await AddAsync(Input(MaintenanceType.Batterie, On(2025, 9, 3), 126_000, "Batterie"));
        await AddAsync(Input(MaintenanceType.RevisionGenerale, On(2025, 2, 14), 118_500, "Révision générale"));

        var result = await _history.Handle(
            new GetMaintenanceHistoryQuery(_userId, _vehicleId), CancellationToken.None);

        var years = result.Value!.Years;
        years.Select(y => y.Year).Should().ContainInOrder(2026, 2025);

        years[0].Records.Select(r => r.Description).Should().ContainInOrder("Vidange", "Pneus avant");
        years[1].Records.Select(r => r.Description).Should().ContainInOrder("Batterie", "Révision générale");
    }

    [Fact]
    public async Task ElResumenDebeContarIntervencionesCosteYUltimoKilometraje()
    {
        await AddAsync(Input(MaintenanceType.Vidange, On(2026, 6, 12), 145_320, "Vidange", 35_000m));
        await AddAsync(Input(MaintenanceType.Pneus, On(2026, 1, 20), 138_400, "Pneus", 120_000m));
        // Sin coste: no debe romper la suma.
        await AddAsync(Input(MaintenanceType.Filtres, On(2025, 5, 1), 120_000, "Filtres", null));

        var result = await _history.Handle(
            new GetMaintenanceHistoryQuery(_userId, _vehicleId), CancellationToken.None);

        result.Value!.RecordCount.Should().Be(3);
        result.Value.TotalCost.Should().Be(155_000m);
        // El de la intervención más reciente.
        result.Value.LastMileage.Should().Be(145_320);
    }

    [Fact]
    public async Task CorregirDebeConservarLaFechaDeCreacion()
    {
        var id = await AddAsync();
        var created = (await _context.MaintenanceRecords.SingleAsync()).CreatedAt;

        await Task.Delay(10);
        await _update.Handle(new UpdateMaintenanceRecordCommand(
            _userId, id, Input(description: "Vidange + filtre à air", cost: 40_000m)),
            CancellationToken.None);

        var record = await _context.MaintenanceRecords.SingleAsync();
        record.Description.Should().Be("Vidange + filtre à air");
        record.Cost.Should().Be(40_000m);
        // La trazabilidad se mantiene: se sabe cuándo se creó y cuándo se corrigió.
        record.CreatedAt.Should().Be(created);
        record.UpdatedAt.Should().BeAfter(created);
    }

    // ─── Privacidad ────────────────────────────────────────────────────────

    [Fact]
    public async Task NadieMasDebeVerNiTocarElHistorial()
    {
        var id = await AddAsync();

        (await _history.Handle(new GetMaintenanceHistoryQuery(_otherUserId, _vehicleId), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _add.Handle(new AddMaintenanceRecordCommand(_otherUserId, _vehicleId, Input()), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _update.Handle(new UpdateMaintenanceRecordCommand(_otherUserId, id, Input()), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _delete.Handle(new DeleteMaintenanceRecordCommand(_otherUserId, id), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");
    }

    // ─── Fotografías y borrado ─────────────────────────────────────────────

    [Fact]
    public async Task BorrarLaIntervencionDebeLlevarseSusFotosPeroNoLaFactura()
    {
        var id = await AddAsync(Input(documentId: _documentId));
        await _addImage.Handle(new AddMaintenanceImageCommand(
            _userId, id, "garage/maintenance/a.webp", "a.webp", "image/webp", 1000),
            CancellationToken.None);

        await _delete.Handle(new DeleteMaintenanceRecordCommand(_userId, id), CancellationToken.None);

        // La intervención sale del historial…
        (await _context.MaintenanceRecords.CountAsync()).Should().Be(0);
        (await _context.MaintenanceRecordImages.CountAsync()).Should().Be(0);
        _storage.Verify(s => s.DeletePrivateAsync("garage/maintenance/a.webp", It.IsAny<CancellationToken>()),
            Times.Once);

        // …pero la factura vive en Documents y puede seguir haciendo falta.
        (await _context.GarageDocuments.AnyAsync(d => d.Id == _documentId)).Should().BeTrue();
        _storage.Verify(s => s.DeletePrivateAsync("k1", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BorrarUnaFotoDebeQuitarlaDelAlmacenamiento()
    {
        var id = await AddAsync();
        var image = await _addImage.Handle(new AddMaintenanceImageCommand(
            _userId, id, "garage/maintenance/b.webp", "b.webp", "image/webp", 500),
            CancellationToken.None);

        var result = await _deleteImage.Handle(
            new DeleteMaintenanceImageCommand(_userId, image.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _storage.Verify(s => s.DeletePrivateAsync("garage/maintenance/b.webp", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ElHistorialNoDebeExponerLaClaveDelAlmacenamiento()
    {
        typeof(MaintenanceImageDto).GetProperties().Select(p => p.Name)
            .Should().NotContain(nameof(MaintenanceRecordImage.StorageKey));
    }

    public void Dispose() => _context.Dispose();
}
