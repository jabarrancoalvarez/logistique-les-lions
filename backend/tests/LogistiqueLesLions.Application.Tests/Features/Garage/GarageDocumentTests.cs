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
/// Historial documental de Mon Garage.
/// </summary>
/// <remarks>
/// Lo delicado aquí es la privacidad: la documentación puede llevar datos personales y
/// la especificación exige que ningún otro usuario acceda a ella.
/// </remarks>
public class GarageDocumentTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStorageService> _storage = new();

    private readonly AddGarageDocumentCommandHandler _add;
    private readonly UpdateGarageDocumentCommandHandler _update;
    private readonly DeleteGarageDocumentCommandHandler _delete;
    private readonly GetGarageDocumentsQueryHandler _list;
    private readonly GetGarageDocumentFileQueryHandler _file;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public GarageDocumentTests()
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
        _context.GarageVehicles.Add(new GarageVehicle
        {
            Id = _vehicleId, UserId = _userId, MakeId = _makeId, Year = 2019, Mileage = 147_500
        });
        _context.SaveChanges();

        _add    = new AddGarageDocumentCommandHandler(_context);
        _update = new UpdateGarageDocumentCommandHandler(_context);
        _delete = new DeleteGarageDocumentCommandHandler(_context, _storage.Object);
        _list   = new GetGarageDocumentsQueryHandler(_context);
        _file   = new GetGarageDocumentFileQueryHandler(_context);
    }

    private async Task<Guid> AddAsync(
        GarageDocumentType type = GarageDocumentType.CarteGrise,
        string name = "Carte grise 2024",
        DateTimeOffset? date = null,
        string key = "garage/doc-1.pdf")
    {
        var result = await _add.Handle(new AddGarageDocumentCommand(
            _userId, _vehicleId, type, name, date, key,
            "carte-grise.pdf", "application/pdf", 240_000, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    // ─── Alta y listado ────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaGuardarElDocumentoConSuClasificacion()
    {
        var id = await AddAsync();

        var document = await _context.GarageDocuments.SingleAsync(d => d.Id == id);
        document.Type.Should().Be(GarageDocumentType.CarteGrise);
        document.Name.Should().Be("Carte grise 2024");
        document.FileName.Should().Be("carte-grise.pdf");
        document.SizeBytes.Should().Be(240_000);
    }

    [Fact]
    public async Task ElNombreEsObligatorio()
    {
        var result = await _add.Handle(new AddGarageDocumentCommand(
            _userId, _vehicleId, GarageDocumentType.Autre, "   ", null, "k",
            "x.pdf", "application/pdf", 10, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("GarageDocument.NameRequired");
    }

    [Fact]
    public async Task ElHistorialDebeOrdenarsePorLaFechaDelDocumento()
    {
        // Una factura de 2019 puede subirse hoy: manda la fecha del documento.
        await AddAsync(name: "Facture 2019",
            date: new DateTimeOffset(2019, 5, 10, 0, 0, 0, TimeSpan.Zero), key: "k1");
        await AddAsync(name: "Facture 2024",
            date: new DateTimeOffset(2024, 2, 3, 0, 0, 0, TimeSpan.Zero), key: "k2");
        await AddAsync(name: "Facture 2021",
            date: new DateTimeOffset(2021, 8, 1, 0, 0, 0, TimeSpan.Zero), key: "k3");

        var result = await _list.Handle(
            new GetGarageDocumentsQuery(_userId, _vehicleId), CancellationToken.None);

        result.Value!.Select(d => d.Name).Should().ContainInOrder(
            "Facture 2024", "Facture 2021", "Facture 2019");
    }

    [Fact]
    public async Task SinFechaDelDocumentoDebeUsarseLaDeSubida()
    {
        await AddAsync(name: "Sans date", date: null, key: "k1");

        var result = await _list.Handle(
            new GetGarageDocumentsQuery(_userId, _vehicleId), CancellationToken.None);

        var document = result.Value!.Single();
        document.DocumentDate.Should().BeNull();
        document.UploadedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task DeberiaPoderCorregirseLaClasificacion()
    {
        var id = await AddAsync(type: GarageDocumentType.Autre, name: "scan.pdf");

        var result = await _update.Handle(new UpdateGarageDocumentCommand(
            _userId, id, GarageDocumentType.Assurance, "Assurance 2025",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), "Renouvelée en janvier"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var document = await _context.GarageDocuments.SingleAsync();
        document.Type.Should().Be(GarageDocumentType.Assurance);
        document.Name.Should().Be("Assurance 2025");
        document.Notes.Should().Be("Renouvelée en janvier");
        // El archivo no se toca al reclasificar.
        document.FileName.Should().Be("carte-grise.pdf");
    }

    // ─── Privacidad ────────────────────────────────────────────────────────

    [Fact]
    public async Task NingunOtroUsuarioDebeAccederALaDocumentacion()
    {
        var id = await AddAsync();

        (await _list.Handle(new GetGarageDocumentsQuery(_otherUserId, _vehicleId), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _file.Handle(new GetGarageDocumentFileQuery(_otherUserId, id), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _update.Handle(new UpdateGarageDocumentCommand(
            _otherUserId, id, GarageDocumentType.Autre, "x", null, null), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _delete.Handle(new DeleteGarageDocumentCommand(_otherUserId, id), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");
    }

    [Fact]
    public async Task NadieDebePoderSubirDocumentosAlVehiculoDeOtro()
    {
        var result = await _add.Handle(new AddGarageDocumentCommand(
            _otherUserId, _vehicleId, GarageDocumentType.Autre, "x", null, "k",
            "x.pdf", "application/pdf", 10, null), CancellationToken.None);

        result.Error.Should().Be("GarageVehicle.AccessDenied");
    }

    [Fact]
    public void ElListadoNoDebeExponerLaClaveDelAlmacenamiento()
    {
        // Si la clave saliera en la API, el archivo dejaría de estar protegido por el
        // endpoint que comprueba de quién es.
        typeof(GarageDocumentDto).GetProperties().Select(p => p.Name)
            .Should().NotContain(nameof(GarageDocument.StorageKey));
    }

    [Fact]
    public async Task DescargarDebeDevolverElArchivoOriginal()
    {
        var id = await AddAsync(key: "garage/abc.pdf");

        var result = await _file.Handle(
            new GetGarageDocumentFileQuery(_userId, id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StorageKey.Should().Be("garage/abc.pdf");
        result.Value.FileName.Should().Be("carte-grise.pdf");
        result.Value.ContentType.Should().Be("application/pdf");
    }

    // ─── Borrado ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BorrarDebeQuitarElArchivoDeVerdad()
    {
        var id = await AddAsync(key: "garage/abc.pdf");

        var result = await _delete.Handle(
            new DeleteGarageDocumentCommand(_userId, id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // La fila sobrevive por trazabilidad…
        (await _context.GarageDocuments.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await _context.GarageDocuments.CountAsync()).Should().Be(0);

        // …pero el archivo con datos personales no: el usuario ha pedido retirarlo.
        _storage.Verify(s => s.DeletePrivateAsync("garage/abc.pdf", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnDocumentoBorradoDesapareceDelHistorial()
    {
        var id = await AddAsync(key: "k1");
        await AddAsync(name: "Assurance", key: "k2");

        await _delete.Handle(new DeleteGarageDocumentCommand(_userId, id), CancellationToken.None);

        var result = await _list.Handle(
            new GetGarageDocumentsQuery(_userId, _vehicleId), CancellationToken.None);

        result.Value!.Should().ContainSingle().Which.Name.Should().Be("Assurance");
    }

    public void Dispose() => _context.Dispose();
}
