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
/// Complétude du dossier.
/// </summary>
/// <remarks>
/// Mide lo completo y actualizado que está el <b>historial digital</b> del vehículo.
/// Nunca su estado mecánico: Yoon u Auto no tiene información para afirmar nada sobre la
/// mecánica, y la especificación lo prohíbe expresamente.
/// </remarks>
public class CompletenessTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetVehicleCompletenessQueryHandler _query;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _modelId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public CompletenessTests()
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
        _context.SaveChanges();

        _query = new GetVehicleCompletenessQueryHandler(_context);
    }

    /// <summary>Ficha mínima: solo marca y año, como permite el alta.</summary>
    private async Task<GarageVehicle> BareVehicleAsync()
    {
        var vehicle = new GarageVehicle
        {
            Id = _vehicleId, UserId = _userId, MakeId = _makeId, Year = 2019
        };
        _context.GarageVehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    /// <summary>Ficha completa: todo relleno, todo al día.</summary>
    private async Task<GarageVehicle> FullVehicleAsync()
    {
        var vehicle = new GarageVehicle
        {
            Id = _vehicleId, UserId = _userId, MakeId = _makeId, ModelId = _modelId,
            Year = 2019, Version = "2.0 D-4D", Mileage = 147_500,
            MileageUpdatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            FuelType = FuelType.Diesel, Transmission = TransmissionType.Automatique,
            BodyType = BodyType.Suv, Color = "Gris", Vin = "JTMBFREV60D012345"
        };
        _context.GarageVehicles.Add(vehicle);

        _context.GarageVehicleImages.Add(new GarageVehicleImage
        {
            GarageVehicleId = _vehicleId, Url = "/a.webp", IsPrimary = true
        });

        var invoice = new GarageDocument
        {
            GarageVehicleId = _vehicleId, Type = GarageDocumentType.FactureEntretien,
            Name = "Facture", StorageKey = "k0", FileName = "f.pdf", ContentType = "application/pdf"
        };
        _context.GarageDocuments.AddRange(
            invoice,
            new GarageDocument
            {
                GarageVehicleId = _vehicleId, Type = GarageDocumentType.CarteGrise,
                Name = "Carte grise", StorageKey = "k1", FileName = "cg.pdf", ContentType = "application/pdf"
            },
            new GarageDocument
            {
                GarageVehicleId = _vehicleId, Type = GarageDocumentType.Assurance,
                Name = "Assurance", StorageKey = "k2", FileName = "as.pdf", ContentType = "application/pdf"
            });

        for (var i = 0; i < 3; i++)
        {
            _context.MaintenanceRecords.Add(new MaintenanceRecord
            {
                GarageVehicleId = _vehicleId,
                Type = MaintenanceType.Vidange,
                PerformedAt = DateTimeOffset.UtcNow.AddMonths(-(i + 1)),
                Description = $"Entretien {i}",
                DocumentId = invoice.Id
            });
        }

        await _context.SaveChangesAsync();
        return vehicle;
    }

    private async Task<CompletenessDto> ScoreAsync()
    {
        var result = await _query.Handle(
            new GetVehicleCompletenessQuery(_userId, _vehicleId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    private static CompletenessItemDto Item(CompletenessDto dto, CompletenessCheck check) =>
        dto.Items.Single(i => i.Check == check);

    // ─── Puntuación global ─────────────────────────────────────────────────

    [Fact]
    public async Task UnaFichaCompletaDebeLlegarACien()
    {
        await FullVehicleAsync();

        var dto = await ScoreAsync();

        dto.Score.Should().Be(100);
        dto.Level.Should().Be(CompletenessLevel.Excellent);
    }

    [Fact]
    public async Task UnaFichaReciennCreadaDebePuntuarBajo()
    {
        await BareVehicleAsync();

        var dto = await ScoreAsync();

        // Solo se salvan los apartados que no penalizan por estar vacíos.
        dto.Score.Should().BeLessThan(50);
        dto.Level.Should().Be(CompletenessLevel.AComplete);
    }

    [Fact]
    public async Task LosPesosDebenSumarCien()
    {
        await BareVehicleAsync();

        var dto = await ScoreAsync();

        dto.Items.Sum(i => i.MaxPoints).Should().Be(100);
        dto.Items.Should().HaveCount(8);
    }

    // ─── Apartados ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UnKilometrajeAntiguoNoDebePuntuarComoActualizado()
    {
        var vehicle = await FullVehicleAsync();
        vehicle.MileageUpdatedAt = DateTimeOffset.UtcNow.AddMonths(-10);
        await _context.SaveChangesAsync();

        var item = Item(await ScoreAsync(), CompletenessCheck.MileageUpToDate);

        // Está puesto, pero no al día: media puntuación.
        item.Status.Should().Be(CompletenessStatus.Partial);
        item.Points.Should().BeLessThan(item.MaxPoints);
        item.Points.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SinKilometrajeNoDebePuntuarNada()
    {
        var vehicle = await FullVehicleAsync();
        vehicle.Mileage = null;
        await _context.SaveChangesAsync();

        var item = Item(await ScoreAsync(), CompletenessCheck.MileageUpToDate);

        item.Status.Should().Be(CompletenessStatus.Missing);
        item.Points.Should().Be(0);
    }

    [Fact]
    public void UnaFotografiaAntiguaDebeAvisarSinLlegarACero()
    {
        // Se calcula sobre un vehículo construido a mano: el interceptor de auditoría
        // fija CreatedAt al guardar, así que una foto de hace dos años no puede montarse
        // contra la base de datos.
        var vehicle = new GarageVehicle
        {
            UserId = _userId, MakeId = _makeId, Year = 2019,
            Images =
            [
                new GarageVehicleImage
                {
                    Url = "/vieille.webp", IsPrimary = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMonths(-20)
                }
            ]
        };

        var item = CompletenessCalculator.For(vehicle).Items
            .Single(i => i.Check == CompletenessCheck.Photos);

        // «⚠ Photo principale ancienne»: hay foto, pero está vieja.
        item.Status.Should().Be(CompletenessStatus.Partial);
        item.Points.Should().BeGreaterThan(0);
        item.Points.Should().BeLessThan(item.MaxPoints);
    }

    [Fact]
    public void SinFotografiaNoDebePuntuarNada()
    {
        var vehicle = new GarageVehicle { UserId = _userId, MakeId = _makeId, Year = 2019 };

        var item = CompletenessCalculator.For(vehicle).Items
            .Single(i => i.Check == CompletenessCheck.Photos);

        item.Status.Should().Be(CompletenessStatus.Missing);
        item.Points.Should().Be(0);
    }

    [Fact]
    public async Task DebeAvisarDeLosDocumentosEsencialesQueFaltan()
    {
        await FullVehicleAsync();
        var assurance = await _context.GarageDocuments
            .SingleAsync(d => d.Type == GarageDocumentType.Assurance);
        assurance.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        var item = Item(await ScoreAsync(), CompletenessCheck.Documents);

        // Está la carte grise pero falta el seguro: «⚠ Assurance à ajouter».
        item.Status.Should().Be(CompletenessStatus.Partial);
    }

    [Fact]
    public async Task DebeContarLasIntervencionesRegistradas()
    {
        await FullVehicleAsync();

        var item = Item(await ScoreAsync(), CompletenessCheck.MaintenanceHistory);

        item.Status.Should().Be(CompletenessStatus.Complete);
        item.Detail.Should().Be(3);
    }

    [Fact]
    public async Task UnRappelVencidoDebeRestarPuntos()
    {
        await FullVehicleAsync();
        _context.VehicleReminders.Add(new VehicleReminder
        {
            GarageVehicleId = _vehicleId, Type = ReminderType.Assurance, Label = "Assurance",
            DueDate = DateTimeOffset.UtcNow.AddDays(-5), Status = ReminderStatus.AFaire
        });
        await _context.SaveChangesAsync();

        var dto = await ScoreAsync();
        var item = Item(dto, CompletenessCheck.Reminders);

        item.Status.Should().Be(CompletenessStatus.Missing);
        item.Detail.Should().Be(1);
        dto.Score.Should().Be(90);
    }

    [Fact]
    public async Task NoTenerRappelsNoDebePenalizar()
    {
        await FullVehicleAsync();

        // No haber programado avisos no es descuidar el vehículo.
        var item = Item(await ScoreAsync(), CompletenessCheck.Reminders);

        item.Status.Should().Be(CompletenessStatus.Complete);
        item.Detail.Should().Be(0);
    }

    [Fact]
    public async Task LasIntervencionesSinFacturaDebenRestarSoloUnaVez()
    {
        var vehicle = await FullVehicleAsync();

        foreach (var record in await _context.MaintenanceRecords.ToListAsync())
            record.DocumentId = null;
        await _context.SaveChangesAsync();

        var dto = await ScoreAsync();

        // El historial sigue completo; lo que baja es el apartado de las facturas.
        Item(dto, CompletenessCheck.MaintenanceHistory).Status
            .Should().Be(CompletenessStatus.Complete);
        Item(dto, CompletenessCheck.MaintenanceInvoices).Points.Should().Be(0);
    }

    [Fact]
    public async Task SinHistorialLasFacturasNoDebenPenalizarPorSeparado()
    {
        await BareVehicleAsync();

        // Descontar dos veces por lo mismo daría un porcentaje injustamente bajo.
        var item = Item(await ScoreAsync(), CompletenessCheck.MaintenanceInvoices);

        item.Status.Should().Be(CompletenessStatus.Complete);
    }

    // ─── Privacidad ────────────────────────────────────────────────────────

    [Fact]
    public async Task NadieMasDebeConsultarLaComplétude()
    {
        await FullVehicleAsync();

        var result = await _query.Handle(
            new GetVehicleCompletenessQuery(_otherUserId, _vehicleId), CancellationToken.None);

        result.Error.Should().Be("GarageVehicle.AccessDenied");
    }

    public void Dispose() => _context.Dispose();
}
