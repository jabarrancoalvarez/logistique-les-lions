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
/// «Mes annonces»: estados, acciones rápidas, duplicado, fotos y calidad del anuncio.
/// </summary>
public class ListingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<INewVehicleAlertService> _newVehicleAlerts = new();
    private readonly Mock<IPriceDropAlertService> _priceDropAlerts = new();

    private readonly ChangeListingStatusCommandHandler _status;
    private readonly UpdateListingPriceCommandHandler _price;
    private readonly UpdateListingMileageCommandHandler _mileage;
    private readonly DuplicateListingCommandHandler _duplicate;
    private readonly ReorderListingImagesCommandHandler _reorder;
    private readonly GetMyListingsQueryHandler _listings;
    private readonly GetListingQualityQueryHandler _quality;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    private int _sequence;

    public ListingTests()
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
            new UserProfile { Id = _userId, DisplayName = "Auto Dakar", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _otherUserId, DisplayName = "Fatou", Phone = "+221770000002", PasswordHash = "x" });
        _context.SaveChanges();

        var references = new Mock<IPublicReferenceGenerator>();
        references
            .Setup(r => r.NextVehicleReferenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"YU{20000 + ++_sequence}");

        _status    = new ChangeListingStatusCommandHandler(_context, _newVehicleAlerts.Object);
        _price     = new UpdateListingPriceCommandHandler(_context, _priceDropAlerts.Object);
        _mileage   = new UpdateListingMileageCommandHandler(_context);
        _duplicate = new DuplicateListingCommandHandler(_context, references.Object);
        _reorder   = new ReorderListingImagesCommandHandler(_context);
        _listings  = new GetMyListingsQueryHandler(_context);
        _quality   = new GetListingQualityQueryHandler(_context);
    }

    private async Task<Vehicle> ListingAsync(
        VehicleStatus status = VehicleStatus.Brouillon,
        decimal price = 8_900_000m,
        Guid? id = null,
        Guid? sellerId = null,
        DateTimeOffset? publishedAt = null)
    {
        var vehicle = new Vehicle
        {
            Id = id ?? _vehicleId,
            PublicReference = $"YU1000{++_sequence}",
            Slug = $"toyota-rav4-yu1000{_sequence}",
            Title = "Toyota RAV4 2019",
            MakeId = _makeId,
            Year = 2019,
            Mileage = 147_500,
            Price = price,
            SellerId = sellerId ?? _userId,
            Status = status,
            PublishedAt = publishedAt
        };
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    // ─── Estados ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PublicarDebeFijarLaFechaYAvisarALasBusquedasGuardadas()
    {
        await ListingAsync();

        var result = await _status.Handle(
            new ChangeListingStatusCommand(_userId, _vehicleId, VehicleStatus.Actif),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var listing = await _context.Vehicles.SingleAsync();
        listing.Status.Should().Be(VehicleStatus.Actif);
        listing.PublishedAt.Should().NotBeNull();

        // La novedad es la publicación, no la creación del borrador.
        _newVehicleAlerts.Verify(
            s => s.NotifyMatchingSearchesAsync(_vehicleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivarTrasUnaPausaNoDebeVolverAAvisar()
    {
        await ListingAsync(VehicleStatus.Actif, publishedAt: DateTimeOffset.UtcNow.AddDays(-3));

        await _status.Handle(new ChangeListingStatusCommand(_userId, _vehicleId, VehicleStatus.EnPause),
            CancellationToken.None);
        await _status.Handle(new ChangeListingStatusCommand(_userId, _vehicleId, VehicleStatus.Actif),
            CancellationToken.None);

        // Ya se había publicado: no vuelve a ser una novedad.
        _newVehicleAlerts.Verify(
            s => s.NotifyMatchingSearchesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NoDebePublicarseUnAnuncioSinPrecio()
    {
        await ListingAsync(price: 0m);

        var result = await _status.Handle(
            new ChangeListingStatusCommand(_userId, _vehicleId, VehicleStatus.Actif),
            CancellationToken.None);

        result.Error.Should().Be("Listing.PriceRequired");
    }

    [Fact]
    public async Task MarcarComoVendidoDebeFijarLaFecha()
    {
        await ListingAsync(VehicleStatus.Actif, publishedAt: DateTimeOffset.UtcNow.AddDays(-3));

        await _status.Handle(new ChangeListingStatusCommand(_userId, _vehicleId, VehicleStatus.Vendu),
            CancellationToken.None);

        (await _context.Vehicles.SingleAsync()).SoldAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnAnuncioVendidoNoDebeVolverAActivarse()
    {
        await ListingAsync(VehicleStatus.Vendu);

        var result = await _status.Handle(
            new ChangeListingStatusCommand(_userId, _vehicleId, VehicleStatus.Actif),
            CancellationToken.None);

        // Su ficha sostiene contratos, favoritos y comparaciones: reabrirlo cambiaría
        // el pasado. Para volver a venderlo, se duplica.
        result.Error.Should().Be("Listing.InvalidTransition");
    }

    [Fact]
    public async Task DesarchivarDebeDevolverloABorrador()
    {
        await ListingAsync(VehicleStatus.Archive, publishedAt: DateTimeOffset.UtcNow.AddMonths(-2));

        await _status.Handle(
            new ChangeListingStatusCommand(_userId, _vehicleId, VehicleStatus.Brouillon),
            CancellationToken.None);

        var listing = await _context.Vehicles.SingleAsync();
        listing.Status.Should().Be(VehicleStatus.Brouillon);
        // Se vuelve a publicar a mano, y entonces contará como novedad otra vez.
        listing.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task NadieMasDebeGestionarElAnuncioDeOtro()
    {
        await ListingAsync(VehicleStatus.Actif);

        (await _status.Handle(new ChangeListingStatusCommand(_otherUserId, _vehicleId, VehicleStatus.EnPause),
            CancellationToken.None)).Error.Should().Be("Vehicle.NotOwner");

        (await _price.Handle(new UpdateListingPriceCommand(_otherUserId, _vehicleId, 1_000_000m),
            CancellationToken.None)).Error.Should().Be("Vehicle.NotOwner");

        (await _duplicate.Handle(new DuplicateListingCommand(_otherUserId, _vehicleId),
            CancellationToken.None)).Error.Should().Be("Vehicle.NotOwner");
    }

    // ─── Precio y kilometraje ──────────────────────────────────────────────

    [Fact]
    public async Task BajarElPrecioDebeDejarRastroYAvisarALosFavoritos()
    {
        await ListingAsync(VehicleStatus.Actif, price: 8_900_000m);

        await _price.Handle(new UpdateListingPriceCommand(_userId, _vehicleId, 8_400_000m),
            CancellationToken.None);

        (await _context.Vehicles.SingleAsync()).Price.Should().Be(8_400_000m);

        var history = await _context.VehiclePriceHistories.SingleAsync();
        history.Price.Should().Be(8_400_000m);

        _priceDropAlerts.Verify(s => s.NotifyPriceDropAsync(
            _vehicleId, 8_900_000m, 8_400_000m, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ElMismoPrecioNoDebeGenerarHistorial()
    {
        await ListingAsync(VehicleStatus.Actif, price: 8_900_000m);

        await _price.Handle(new UpdateListingPriceCommand(_userId, _vehicleId, 8_900_000m),
            CancellationToken.None);

        (await _context.VehiclePriceHistories.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ElKilometrajeDelAnuncioNoDebeRetroceder()
    {
        await ListingAsync(VehicleStatus.Actif);

        var result = await _mileage.Handle(
            new UpdateListingMileageCommand(_userId, _vehicleId, 100_000), CancellationToken.None);

        result.Error.Should().Be("Listing.MileageWentBackwards");
    }

    // ─── Duplicar ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicarDebeCrearUnBorradorSinElPasadoDelOriginal()
    {
        var original = await ListingAsync(VehicleStatus.Vendu);
        original.Vin = "JTMBFREV60D012345";
        original.ViewsCount = 340;
        original.FavoritesCount = 12;
        _context.VehicleImages.Add(new VehicleImage
        {
            VehicleId = original.Id, Url = "/a.webp", IsPrimary = true
        });
        await _context.SaveChangesAsync();

        var result = await _duplicate.Handle(
            new DuplicateListingCommand(_userId, _vehicleId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var copy = await _context.Vehicles
            .IgnoreQueryFilters()
            .Include(v => v.Images)
            .SingleAsync(v => v.Id == result.Value);

        copy.Status.Should().Be(VehicleStatus.Brouillon);
        copy.Title.Should().Be(original.Title);
        copy.Price.Should().Be(original.Price);
        copy.Images.Should().HaveCount(1);

        // Ni el VIN ni los contadores son de la copia.
        copy.Vin.Should().BeNull();
        copy.ViewsCount.Should().Be(0);
        copy.FavoritesCount.Should().Be(0);
        copy.PublicReference.Should().NotBe(original.PublicReference);
        copy.Slug.Should().NotBe(original.Slug);
    }

    // ─── Fotografías ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReordenarDebeHacerPrincipalALaPrimera()
    {
        await ListingAsync(VehicleStatus.Actif);
        var first = new VehicleImage { VehicleId = _vehicleId, Url = "/a.webp", IsPrimary = true, SortOrder = 0 };
        var second = new VehicleImage { VehicleId = _vehicleId, Url = "/b.webp", SortOrder = 1 };
        _context.VehicleImages.AddRange(first, second);
        await _context.SaveChangesAsync();

        var result = await _reorder.Handle(
            new ReorderListingImagesCommand(_userId, _vehicleId, [second.Id, first.Id]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var images = await _context.VehicleImages.OrderBy(i => i.SortOrder).ToListAsync();
        images[0].Url.Should().Be("/b.webp");
        images[0].IsPrimary.Should().BeTrue();
        images[1].IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task UnOrdenIncompletoDebeRechazarse()
    {
        await ListingAsync(VehicleStatus.Actif);
        var first = new VehicleImage { VehicleId = _vehicleId, Url = "/a.webp", IsPrimary = true };
        _context.VehicleImages.AddRange(first, new VehicleImage { VehicleId = _vehicleId, Url = "/b.webp" });
        await _context.SaveChangesAsync();

        var result = await _reorder.Handle(
            new ReorderListingImagesCommand(_userId, _vehicleId, [first.Id]), CancellationToken.None);

        result.Error.Should().Be("Listing.ImageSetMismatch");
    }

    // ─── Listado y calidad ─────────────────────────────────────────────────

    [Fact]
    public async Task ElListadoDebeIncluirBorradoresYArchivados()
    {
        await ListingAsync(VehicleStatus.Brouillon, id: Guid.NewGuid());
        await ListingAsync(VehicleStatus.Actif, id: Guid.NewGuid());
        await ListingAsync(VehicleStatus.Archive, id: Guid.NewGuid());
        await ListingAsync(VehicleStatus.Actif, id: Guid.NewGuid(), sellerId: _otherUserId);

        var result = await _listings.Handle(new GetMyListingsQuery(_userId), CancellationToken.None);

        result.Value!.Listings.Should().HaveCount(3);
        result.Value.CountByStatus[VehicleStatus.Brouillon].Should().Be(1);
        result.Value.CountByStatus[VehicleStatus.Archive].Should().Be(1);
    }

    [Fact]
    public async Task ElListadoDebeContarLasNegociacionesAbiertas()
    {
        await ListingAsync(VehicleStatus.Actif);
        _context.Negotiations.AddRange(
            new Negotiation { BuyerId = _otherUserId, SellerId = _userId, VehicleId = _vehicleId,
                              Status = NegotiationStatus.EnCours },
            new Negotiation { BuyerId = Guid.NewGuid(), SellerId = _userId, VehicleId = _vehicleId,
                              Status = NegotiationStatus.Terminee });
        await _context.SaveChangesAsync();

        var result = await _listings.Handle(new GetMyListingsQuery(_userId), CancellationToken.None);

        // Las terminadas no cuentan: ya no hay nada que atender.
        result.Value!.Listings.Single().NegotiationCount.Should().Be(1);
    }

    [Fact]
    public async Task UnAnuncioVacioDebePuntuarBajoEnCalidad()
    {
        await ListingAsync(price: 0m);
        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Mileage = null;
        await _context.SaveChangesAsync();

        var result = await _quality.Handle(
            new GetListingQualityQuery(_userId, _vehicleId), CancellationToken.None);

        result.Value!.Score.Should().BeLessThan(20);
        result.Value.Items.Sum(i => i.MaxPoints).Should().Be(100);
    }

    [Fact]
    public async Task UnAnuncioCompletoDebeLlegarACien()
    {
        await ListingAsync(VehicleStatus.Actif);
        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Description   = new string('a', 250);
        vehicle.CustomsStatus = CustomsStatus.Dedouane;
        vehicle.Region        = "DK";
        vehicle.City          = "Dakar";
        vehicle.FuelType      = FuelType.Diesel;
        vehicle.Transmission  = TransmissionType.Automatique;
        vehicle.BodyType      = BodyType.Suv;
        vehicle.PowerCv       = 150;

        for (var i = 0; i < 5; i++)
            _context.VehicleImages.Add(new VehicleImage
            {
                VehicleId = _vehicleId, Url = $"/{i}.webp", IsPrimary = i == 0, SortOrder = i
            });

        for (var i = 0; i < 5; i++)
        {
            var equipment = new VehicleEquipment { Code = $"eq-{i}", Name = $"Equipement {i}" };
            _context.VehicleEquipments.Add(equipment);
            _context.VehicleEquipmentLinks.Add(new VehicleEquipmentLink
            {
                VehicleId = _vehicleId, EquipmentId = equipment.Id
            });
        }

        await _context.SaveChangesAsync();

        var result = await _quality.Handle(
            new GetListingQualityQuery(_userId, _vehicleId), CancellationToken.None);

        result.Value!.Score.Should().Be(100);
    }

    public void Dispose() => _context.Dispose();
}
