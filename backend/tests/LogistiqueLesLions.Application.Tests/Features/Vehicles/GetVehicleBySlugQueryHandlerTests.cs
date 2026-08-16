using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

/// <summary>
/// Quién puede abrir la ficha de un anuncio por su enlace. Un anuncio vendido sigue
/// teniendo página —de ella cuelgan el contrato y los favoritos—, pero el borrador, el
/// pausado, el archivado y sobre todo el ocultado por moderación no se abren desde
/// fuera: si bastara el enlace, ocultar no serviría de nada.
/// </summary>
public class GetVehicleBySlugQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetVehicleBySlugQueryHandler _handler;

    private readonly Guid _make = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _stranger = Guid.NewGuid();

    public GetVehicleBySlugQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(_owner);

        _context = new ApplicationDbContext(
            options,
            new Infrastructure.Persistence.Interceptors.AuditInterceptor(mockCurrentUser.Object),
            new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
                mockCurrentUser.Object, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        // El indicador de precio no es lo que se prueba aquí: sin comparables.
        var priceIndicator = new Mock<IPriceIndicatorService>();
        priceIndicator
            .Setup(p => p.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PriceIndicatorResult.NotEnoughData(0));

        _handler = new GetVehicleBySlugQueryHandler(_context, priceIndicator.Object);
        Seed();
    }

    private void Seed()
    {
        _context.VehicleMakes.Add(new VehicleMake { Id = _make, Name = "Toyota", Country = "JP" });

        // El vendedor es obligatorio en la ficha: sin él, el Include lo descartaría.
        _context.UserProfiles.Add(new UserProfile
        {
            Id = _owner, DisplayName = "Mamadou Diop", Phone = "+221770000001",
            PasswordHash = "x", City = "Dakar", AccountType = AccountType.Particulier
        });

        _context.Vehicles.AddRange(
            V("actif", VehicleStatus.Actif),
            V("reserve", VehicleStatus.Reserve),
            V("vendu", VehicleStatus.Vendu),
            V("brouillon", VehicleStatus.Brouillon),
            V("en-pause", VehicleStatus.EnPause),
            V("archive", VehicleStatus.Archive),
            V("masque", VehicleStatus.Actif, adminHidden: true));

        _context.SaveChanges();
    }

    private Vehicle V(string slug, VehicleStatus status, bool adminHidden = false) => new()
    {
        Id = Guid.NewGuid(),
        PublicReference = $"YU{Random.Shared.Next(10000, 99999)}",
        Slug = slug,
        Title = $"Toyota {slug}",
        MakeId = _make,
        Price = 8_900_000m,
        Year = 2019,
        Region = "DK",
        SellerId = _owner,
        Status = status,
        AdminHiddenAt = adminHidden ? DateTimeOffset.UtcNow : null
    };

    private async Task<bool> PuedeVer(string slug, Guid? requesterId = null, bool isAdmin = false)
    {
        var result = await _handler.Handle(
            new GetVehicleBySlugQuery(slug, requesterId, isAdmin), CancellationToken.None);

        if (!result.IsSuccess)
            result.Error.Should().Be("Vehicle.NotFound");

        return result.IsSuccess;
    }

    [Theory]
    [InlineData("actif")]
    [InlineData("reserve")]
    [InlineData("vendu")]
    public async Task UnVisitanteDeberiaAbrirLosAnunciosConPaginaPublica(string slug)
    {
        (await PuedeVer(slug)).Should().BeTrue();
    }

    [Theory]
    [InlineData("brouillon")]
    [InlineData("en-pause")]
    [InlineData("archive")]
    [InlineData("masque")]
    public async Task UnVisitanteNoDeberiaAbrirLosAnunciosSinPaginaPublica(string slug)
    {
        (await PuedeVer(slug)).Should().BeFalse();
    }

    [Theory]
    [InlineData("brouillon")]
    [InlineData("en-pause")]
    [InlineData("archive")]
    [InlineData("masque")]
    public async Task UnaCuentaAjenaTampocoDeberiaAbrirlos(string slug)
    {
        (await PuedeVer(slug, requesterId: _stranger)).Should().BeFalse();
    }

    [Theory]
    [InlineData("brouillon")]
    [InlineData("en-pause")]
    [InlineData("archive")]
    public async Task ElDuenoDeberiaAbrirSusPropiosAnunciosSinPaginaPublica(string slug)
    {
        (await PuedeVer(slug, requesterId: _owner)).Should().BeTrue();
    }

    [Fact]
    public async Task ElDuenoDeberiaSeguirViendoElSuyoAunqueLoHayaOcultadoElAdministrador()
    {
        // Ocultar es una medida del backoffice: el vendedor debe poder ver de qué se
        // trata para pedir la corrección, aunque nadie más lo abra.
        (await PuedeVer("masque", requesterId: _owner)).Should().BeTrue();
    }

    [Theory]
    [InlineData("brouillon")]
    [InlineData("en-pause")]
    [InlineData("archive")]
    [InlineData("masque")]
    public async Task ElAdministradorDeberiaAbrirCualquiera(string slug)
    {
        (await PuedeVer(slug, requesterId: _stranger, isAdmin: true)).Should().BeTrue();
    }

    [Fact]
    public async Task DeberiaFallarSiElSlugNoExiste()
    {
        (await PuedeVer("no-existe")).Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
