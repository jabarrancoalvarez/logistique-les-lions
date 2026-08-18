using Xunit;
using FluentAssertions;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFeaturedVehicles;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using LogistiqueLesLions.Application.Common.Interfaces;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

/// <summary>
/// Tests de integración para GetFeaturedVehiclesQueryHandler: la portada solo muestra
/// los «À la une» vigentes (no caducados, activos y no ocultados).
/// </summary>
public class GetFeaturedVehiclesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetFeaturedVehiclesQueryHandler _handler;

    public GetFeaturedVehiclesQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var auditInterceptor = new LogistiqueLesLions.Infrastructure.Persistence.Interceptors.AuditInterceptor(mockCurrentUser.Object);
        var auditLogInterceptor = new LogistiqueLesLions.Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
            mockCurrentUser.Object,
            new Microsoft.AspNetCore.Http.HttpContextAccessor());

        _context = new ApplicationDbContext(options, auditInterceptor, auditLogInterceptor);
        _handler = new GetFeaturedVehiclesQueryHandler(_context);

        SeedTestData();
    }

    [Fact]
    public async Task Handle_DeberiaDevolver_SoloALaUneVigentes()
    {
        var query = new GetFeaturedVehiclesQuery(Count: 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Solo los dos «À la une» activos y no caducados. En vedette, caducado y
        // borrado quedan fuera.
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(v => v.Title.StartsWith("A la une"));
    }

    [Fact]
    public async Task Handle_DeberiaRespetar_LimiteCount()
    {
        var query = new GetFeaturedVehiclesQuery(Count: 1);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoDebeDevolver_VehiculosEliminados()
    {
        var deletedVehicle = _context.Vehicles
            .IgnoreQueryFilters()
            .First(v => v.FeaturedTier == FeaturedTier.ALaUne && v.DeletedAt.HasValue);

        var result = await _handler.Handle(new GetFeaturedVehiclesQuery(), CancellationToken.None);

        result.Value!.Should().NotContain(v => v.Id == deletedVehicle.Id);
    }

    [Fact]
    public async Task Handle_NoDebeDevolver_EnVedetteNiCaducados()
    {
        var result = await _handler.Handle(new GetFeaturedVehiclesQuery(Count: 10), CancellationToken.None);

        result.Value!.Should().NotContain(v => v.Title.Contains("En vedette"));
        result.Value!.Should().NotContain(v => v.Title.Contains("caduca"));
    }

    private void SeedTestData()
    {
        var now = DateTimeOffset.UtcNow;
        var make = new VehicleMake
        {
            Id = Guid.NewGuid(), Name = "BMW",
            CreatedAt = now, UpdatedAt = now
        };
        _context.VehicleMakes.Add(make);

        Vehicle V(string slug, string title, FeaturedTier tier, DateTimeOffset? until,
                  VehicleStatus status = VehicleStatus.Actif, DateTimeOffset? deletedAt = null) =>
            new()
            {
                Id = Guid.NewGuid(), Slug = slug, Title = title,
                MakeId = make.Id, Year = 2022, Price = 8_900_000m, Currency = "XOF",
                CountryOrigin = "SN", Condition = VehicleCondition.Used,
                Status = status,
                FeaturedTier = tier,
                FeaturedAt = tier == FeaturedTier.Aucune ? null : now,
                FeaturedUntil = until,
                DeletedAt = deletedAt,
                CreatedAt = now, UpdatedAt = now
            };

        _context.Vehicles.AddRange(
            V("alu-1", "A la une 1", FeaturedTier.ALaUne, now.AddDays(20)),
            V("alu-2", "A la une 2", FeaturedTier.ALaUne, now.AddDays(5)),
            V("normal", "Normal sans mise en avant", FeaturedTier.Aucune, null),
            V("vedette", "En vedette (pas à la une)", FeaturedTier.EnVedette, now.AddDays(10)),
            V("caduc", "A la une caducado", FeaturedTier.ALaUne, now.AddDays(-1)),
            V("alu-del", "A la une supprimé", FeaturedTier.ALaUne, now.AddDays(20),
              deletedAt: now.AddDays(-1))
        );

        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();
}
