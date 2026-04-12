using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.PublicTracking.Queries.GetTrackingByCode;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using LogistiqueLesLions.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.PublicTracking;

public class GetTrackingByCodeQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetTrackingByCodeQueryHandler _handler;
    private readonly string _existingCode = "ABCD23EFGH45";

    public GetTrackingByCodeQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var audit    = new AuditInterceptor(currentUser.Object);
        var auditLog = new AuditLogInterceptor(currentUser.Object, new HttpContextAccessor());

        _context = new ApplicationDbContext(options, audit, auditLog);
        _handler = new GetTrackingByCodeQueryHandler(_context);

        SeedTestData();
    }

    [Fact]
    public async Task Handle_ConCodigoExistente_DevuelveProcesoConDatosPublicos()
    {
        var result = await _handler.Handle(new GetTrackingByCodeQuery(_existingCode), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TrackingCode.Should().Be(_existingCode);
        result.Value.Status.Should().Be("InProgress");
        result.Value.OriginCountry.Should().Be("DE");
        result.Value.DestinationCountry.Should().Be("ES");
        result.Value.VehicleTitle.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_NormalizaInput_LowercaseConEspacios()
    {
        var result = await _handler.Handle(
            new GetTrackingByCodeQuery($"  {_existingCode.ToLowerInvariant()}  "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TrackingCode.Should().Be(_existingCode);
    }

    [Fact]
    public async Task Handle_ConCodigoInexistente_DevuelveFailureGenerico()
    {
        var result = await _handler.Handle(
            new GetTrackingByCodeQuery("NOPENOPE9999"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Tracking.NotFound");
    }

    [Fact]
    public async Task Handle_ConCodigoDemasiadoCorto_DevuelveInvalidCode()
    {
        var result = await _handler.Handle(
            new GetTrackingByCodeQuery("ABC"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Tracking.InvalidCode");
    }

    [Fact]
    public void GenerateTrackingCode_ProduceCodigosUnicosDeLongitud12()
    {
        var codes = Enumerable.Range(0, 1000)
            .Select(_ => ImportExportProcess.GenerateTrackingCode())
            .ToList();

        codes.Should().AllSatisfy(c => c.Length.Should().Be(12));
        codes.Distinct().Count().Should().BeGreaterThan(995, "colisiones aceptables muy puntuales");
        codes.Should().AllSatisfy(c => c.Should().NotContain("0").And.NotContain("1").And.NotContain("O").And.NotContain("I"));
    }

    private void SeedTestData()
    {
        var make = new VehicleMake
        {
            Id = Guid.NewGuid(),
            Name = "BMW",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _context.VehicleMakes.Add(make);

        var vehicleId = Guid.NewGuid();
        _context.Vehicles.Add(new Vehicle
        {
            Id = vehicleId, Slug = "bmw-tracking-test", Title = "BMW 320d Tracking Test",
            MakeId = make.Id, Year = 2022, Price = 25000, Currency = "EUR",
            CountryOrigin = "DE", Condition = VehicleCondition.Used,
            Status = VehicleStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        _context.ImportExportProcesses.Add(new ImportExportProcess
        {
            Id = Guid.NewGuid(),
            TrackingCode = _existingCode,
            VehicleId = vehicleId,
            BuyerId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            OriginCountry = "DE",
            DestinationCountry = "ES",
            ProcessType = ProcessType.ImportExtraEu,
            Status = ProcessStatus.InProgress,
            CompletionPercent = 25,
            StartedAt = DateTimeOffset.UtcNow.AddDays(-3),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
            UpdatedAt = DateTimeOffset.UtcNow
        });

        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();
}
