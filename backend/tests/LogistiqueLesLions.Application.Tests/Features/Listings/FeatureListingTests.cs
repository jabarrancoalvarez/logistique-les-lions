using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Listings;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Listings;

/// <summary>
/// «Mettre en avant»: reglas del destacado del propietario. Solo anuncios activos,
/// niveles válidos (En vedette / À la une) y duraciones de 15 o 30 días.
/// </summary>
public class FeatureListingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FeatureListingCommandHandler _feature;
    private readonly UnfeatureListingCommandHandler _unfeature;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public FeatureListingTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(_ownerId);

        _context = new ApplicationDbContext(
            options,
            new Infrastructure.Persistence.Interceptors.AuditInterceptor(currentUser.Object),
            new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
                currentUser.Object, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        _feature = new FeatureListingCommandHandler(_context);
        _unfeature = new UnfeatureListingCommandHandler(_context);

        Seed(VehicleStatus.Actif);
    }

    private void Seed(VehicleStatus status)
    {
        _context.VehicleMakes.Add(new VehicleMake { Id = _makeId, Name = "Toyota" });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId, Slug = "toyota-rav4", Title = "Toyota RAV4",
            MakeId = _makeId, Year = 2019, Price = 8_900_000m, Currency = "XOF",
            SellerId = _ownerId, Status = status
        });
        _context.SaveChanges();
    }

    private Vehicle Reload() => _context.Vehicles.IgnoreQueryFilters().Single(v => v.Id == _vehicleId);

    [Theory]
    [InlineData(FeaturedTier.EnVedette, 15)]
    [InlineData(FeaturedTier.ALaUne, 30)]
    public async Task DestacarUnAnuncioActivoLoActivaConSuCaducidad(FeaturedTier tier, int days)
    {
        var before = DateTimeOffset.UtcNow;

        var result = await _feature.Handle(
            new FeatureListingCommand(_ownerId, _vehicleId, tier, days), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var v = Reload();
        v.FeaturedTier.Should().Be(tier);
        v.FeaturedUntil.Should().BeCloseTo(before.AddDays(days), TimeSpan.FromMinutes(1));
        v.FeaturedAt.Should().NotBeNull();
        v.IsFeaturedActive(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task NoSePuedeDestacarConUnaDuracionNoPermitida()
    {
        var result = await _feature.Handle(
            new FeatureListingCommand(_ownerId, _vehicleId, FeaturedTier.ALaUne, 20),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Listing.InvalidFeaturedDuration");
        Reload().FeaturedTier.Should().Be(FeaturedTier.Aucune);
    }

    [Fact]
    public async Task NoSePuedeUsarAucuneComoNivel()
    {
        var result = await _feature.Handle(
            new FeatureListingCommand(_ownerId, _vehicleId, FeaturedTier.Aucune, 30),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Listing.InvalidFeaturedTier");
    }

    [Fact]
    public async Task NoSePuedeDestacarUnAnuncioQueNoEstaActivo()
    {
        var vehicle = Reload();
        vehicle.Status = VehicleStatus.EnPause;
        await _context.SaveChangesAsync();

        var result = await _feature.Handle(
            new FeatureListingCommand(_ownerId, _vehicleId, FeaturedTier.ALaUne, 30),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Listing.MustBeActiveToFeature");
    }

    [Fact]
    public async Task NadieDestacaUnAnuncioAjeno()
    {
        var result = await _feature.Handle(
            new FeatureListingCommand(Guid.NewGuid(), _vehicleId, FeaturedTier.ALaUne, 30),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Vehicle.NotOwner");
    }

    [Fact]
    public async Task RetirarElDestacadoDevuelveElAnuncioANormal()
    {
        await _feature.Handle(
            new FeatureListingCommand(_ownerId, _vehicleId, FeaturedTier.ALaUne, 30),
            CancellationToken.None);

        var result = await _unfeature.Handle(
            new UnfeatureListingCommand(_ownerId, _vehicleId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var v = Reload();
        v.FeaturedTier.Should().Be(FeaturedTier.Aucune);
        v.FeaturedUntil.Should().BeNull();
        v.FeaturedAt.Should().BeNull();
    }

    public void Dispose() => _context.Dispose();
}
