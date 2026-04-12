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
/// Tests de integración para GetFeaturedVehiclesQueryHandler.
/// Usa InMemory database para validar la lógica de consulta.
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

        _context = new ApplicationDbContext(options, auditInterceptor);
        _handler = new GetFeaturedVehiclesQueryHandler(_context);

        SeedTestData();
    }

    [Fact]
    public async Task Handle_DeberiaDevolver_SoloVehiculosDestacadosActivos()
    {
        // Arrange
        var query = new GetFeaturedVehiclesQuery(Count: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2, "solo hay 2 vehículos activos y destacados en el seed");
        result.Value.Should().AllSatisfy(v => v.Title.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task Handle_DeberiaRespetar_LimiteCount()
    {
        // Arrange
        var query = new GetFeaturedVehiclesQuery(Count: 1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoDebeDevolver_VehiculosEliminados()
    {
        // Arrange
        var deletedVehicle = _context.Vehicles
            .IgnoreQueryFilters()
            .First(v => v.IsFeatured && v.DeletedAt.HasValue);

        var query = new GetFeaturedVehiclesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value!.Should().NotContain(v => v.Id == deletedVehicle.Id);
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

        // Vehículo activo y destacado ✓
        _context.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(), Slug = "bmw-test-1", Title = "BMW Test 1",
            MakeId = make.Id, Year = 2022, Price = 30000, Currency = "EUR",
            CountryOrigin = "DE", Condition = VehicleCondition.Used,
            Status = VehicleStatus.Active, IsFeatured = true,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        // Vehículo activo y destacado ✓
        _context.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(), Slug = "bmw-test-2", Title = "BMW Test 2",
            MakeId = make.Id, Year = 2021, Price = 25000, Currency = "EUR",
            CountryOrigin = "FR", Condition = VehicleCondition.Used,
            Status = VehicleStatus.Active, IsFeatured = true,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        // Vehículo NO destacado (no debería aparecer)
        _context.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(), Slug = "bmw-test-3", Title = "BMW Test 3",
            MakeId = make.Id, Year = 2020, Price = 20000, Currency = "EUR",
            CountryOrigin = "ES", Condition = VehicleCondition.Used,
            Status = VehicleStatus.Active, IsFeatured = false,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        // Vehículo destacado pero BORRADO (no debería aparecer)
        _context.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(), Slug = "bmw-test-4", Title = "BMW Test Deleted",
            MakeId = make.Id, Year = 2019, Price = 15000, Currency = "EUR",
            CountryOrigin = "IT", Condition = VehicleCondition.Used,
            Status = VehicleStatus.Active, IsFeatured = true,
            DeletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();
}
