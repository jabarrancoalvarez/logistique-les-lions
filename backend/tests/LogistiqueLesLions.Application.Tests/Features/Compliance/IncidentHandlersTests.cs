using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Compliance.Commands.CreateIncident;
using LogistiqueLesLions.Application.Features.Compliance.Commands.ResolveIncident;
using LogistiqueLesLions.Application.Features.Compliance.Queries.GetIncidents;
using LogistiqueLesLions.Application.Features.Compliance.Queries.SimulateCost;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using LogistiqueLesLions.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Compliance;

public class IncidentHandlersTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IWebhookPublisher> _webhooks = new();
    private readonly Guid _processId = Guid.NewGuid();

    public IncidentHandlersTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        _db = new ApplicationDbContext(
            options,
            new AuditInterceptor(currentUser.Object),
            new AuditLogInterceptor(currentUser.Object, new HttpContextAccessor()));

        Seed();
    }

    [Fact]
    public async Task CreateIncident_ConDatosValidos_PersisteYDisparaWebhook()
    {
        var handler = new CreateIncidentCommandHandler(_db, _webhooks.Object);
        var cmd = new CreateIncidentCommand(_processId, "Documento rechazado en aduana", "COC inválido", IncidentSeverity.High);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _db.ProcessIncidents.FirstAsync(i => i.Id == result.Value);
        saved.Title.Should().Be("Documento rechazado en aduana");
        saved.Status.Should().Be(IncidentStatus.Open);
        saved.Severity.Should().Be(IncidentSeverity.High);
        _webhooks.Verify(w => w.PublishAsync("incident.created", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncident_SinTitulo_DevuelveFailure()
    {
        var handler = new CreateIncidentCommandHandler(_db, _webhooks.Object);
        var result = await handler.Handle(new CreateIncidentCommand(_processId, "  ", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Incident.TitleRequired");
    }

    [Fact]
    public async Task CreateIncident_ProcesoInexistente_DevuelveFailure()
    {
        var handler = new CreateIncidentCommandHandler(_db, _webhooks.Object);
        var result = await handler.Handle(new CreateIncidentCommand(Guid.NewGuid(), "X", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Process.NotFound");
    }

    [Fact]
    public async Task ResolveIncident_MarcaResueltaConTimestamp()
    {
        var incident = new ProcessIncident
        {
            ProcessId = _processId, Title = "Test", Severity = IncidentSeverity.Low,
            Status = IncidentStatus.Open
        };
        _db.ProcessIncidents.Add(incident);
        await _db.SaveChangesAsync();

        var handler = new ResolveIncidentCommandHandler(_db);
        var result = await handler.Handle(new ResolveIncidentCommand(incident.Id, "OK"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var fresh = await _db.ProcessIncidents.FirstAsync(i => i.Id == incident.Id);
        fresh.Status.Should().Be(IncidentStatus.Resolved);
        fresh.ResolvedAt.Should().NotBeNull();
        fresh.Resolution.Should().Be("OK");
    }

    [Fact]
    public async Task GetIncidents_FiltraPorEstado()
    {
        _db.ProcessIncidents.AddRange(
            new ProcessIncident { ProcessId = _processId, Title = "A", Status = IncidentStatus.Open,     Severity = IncidentSeverity.High },
            new ProcessIncident { ProcessId = _processId, Title = "B", Status = IncidentStatus.Resolved, Severity = IncidentSeverity.Low });
        await _db.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(_db);
        var result = await handler.Handle(new GetIncidentsQuery(_processId, IncidentStatus.Open), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("A");
    }

    [Fact]
    public async Task SimulateCost_SinDatosNormativa_AplicaEstimacionBasica()
    {
        var handler = new SimulateCostQueryHandler(_db);
        var result = await handler.Handle(new SimulateCostQuery(20000m, "ES", "DE"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VehiclePriceEur.Should().Be(20000m);
        result.Value.TotalEstimatedEur.Should().BeGreaterThan(20000m);
        result.Value.LineItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SimulateCost_PrecioCero_DevuelveFailure()
    {
        var handler = new SimulateCostQueryHandler(_db);
        var result = await handler.Handle(new SimulateCostQuery(0, "ES", "DE"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Simulate.PriceRequired");
    }

    private void Seed()
    {
        var make = new VehicleMake { Id = Guid.NewGuid(), Name = "Audi", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        _db.VehicleMakes.Add(make);
        var vehicleId = Guid.NewGuid();
        _db.Vehicles.Add(new Vehicle
        {
            Id = vehicleId, Slug = "audi-x", Title = "Audi X", MakeId = make.Id, Year = 2023,
            Price = 30000, Currency = "EUR", CountryOrigin = "DE",
            Condition = VehicleCondition.Used, Status = VehicleStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.ImportExportProcesses.Add(new ImportExportProcess
        {
            Id = _processId, TrackingCode = "TESTCODE0001", VehicleId = vehicleId,
            BuyerId = Guid.NewGuid(), SellerId = Guid.NewGuid(),
            OriginCountry = "DE", DestinationCountry = "ES",
            ProcessType = ProcessType.ImportExtraEu, Status = ProcessStatus.InProgress,
            CompletionPercent = 10, StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();
}
