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
/// Mon Garage: alta de vehículos, ficha y tarjetas.
/// </summary>
/// <remarks>
/// Lo delicado es que el garaje es privado y que lo comprado en Yoon u Auto solo entra
/// una vez y solo en el garaje de quien compra.
/// </remarks>
public class GarageTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreateGarageVehicleCommandHandler _create;
    private readonly UpdateGarageVehicleCommandHandler _update;
    private readonly DeleteGarageVehicleCommandHandler _delete;
    private readonly AddGarageVehicleImageCommandHandler _addImage;
    private readonly DeleteGarageVehicleImageCommandHandler _deleteImage;
    private readonly GetMyGarageQueryHandler _garage;
    private readonly GetGarageVehicleQueryHandler _detail;
    private readonly GetGaragePrefillFromContractQueryHandler _prefill;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _otherMakeId = Guid.NewGuid();
    private readonly Guid _modelId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public GarageTests()
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

        _context.VehicleMakes.AddRange(
            new VehicleMake { Id = _makeId, Name = "Toyota", Country = "JP" },
            new VehicleMake { Id = _otherMakeId, Name = "Peugeot", Country = "FR" });
        _context.VehicleModels.Add(
            new VehicleModel { Id = _modelId, MakeId = _makeId, Name = "RAV4" });
        _context.UserProfiles.AddRange(
            new UserProfile { Id = _userId, DisplayName = "Mamadou Diop", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _otherUserId, DisplayName = "Auto Dakar", Phone = "+221770000002", PasswordHash = "x" });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId,
            PublicReference = "YU10001",
            Slug = "toyota-rav4-yu10001",
            Title = "Toyota RAV4 2019",
            MakeId = _makeId,
            ModelId = _modelId,
            Year = 2019,
            Mileage = 78_000,
            Price = 8_900_000m,
            SellerId = _otherUserId,
            FuelType = FuelType.Diesel,
            Transmission = TransmissionType.Automatique,
            BodyType = BodyType.Suv,
            Color = "Gris",
            Status = VehicleStatus.Vendu
        });
        _context.SaveChanges();

        _create      = new CreateGarageVehicleCommandHandler(_context);
        _update      = new UpdateGarageVehicleCommandHandler(_context);
        _delete      = new DeleteGarageVehicleCommandHandler(_context);
        _addImage    = new AddGarageVehicleImageCommandHandler(_context);
        _deleteImage = new DeleteGarageVehicleImageCommandHandler(_context);
        _garage      = new GetMyGarageQueryHandler(_context);
        _detail      = new GetGarageVehicleQueryHandler(_context);
        _prefill     = new GetGaragePrefillFromContractQueryHandler(_context);
    }

    private GarageVehicleInput Input(
        Guid? makeId = null, Guid? modelId = null, int year = 2019, int? mileage = 147_500) =>
        new(makeId ?? _makeId, modelId ?? _modelId, "2.0 D-4D", year, mileage,
            FuelType.Diesel, TransmissionType.Automatique, BodyType.Suv,
            150, 1998, "Gris", "dk-1234-ab", "jtmbfrev60d012345",
            new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero), 8_300_000m);

    private async Task<Guid> AddAsync(Guid? userId = null)
    {
        var result = await _create.Handle(
            new CreateGarageVehicleCommand(userId ?? _userId, Input()), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    /// <summary>Deja un contrato validado a nombre del usuario como comprador.</summary>
    private async Task<Guid> ValidatedContractAsync(Guid? buyerId = null)
    {
        var negotiation = new Negotiation
        {
            BuyerId = buyerId ?? _userId,
            SellerId = _otherUserId,
            VehicleId = _vehicleId,
            Status = NegotiationStatus.Terminee
        };
        _context.Negotiations.Add(negotiation);

        var contract = new Contract
        {
            PublicReference  = "YC00001",
            NegotiationId    = negotiation.Id,
            VehicleId        = _vehicleId,
            SellerId         = _otherUserId,
            BuyerId          = buyerId ?? _userId,
            CreatedByUserId  = _otherUserId,
            Status           = ContractStatus.Valide,
            VehicleMake      = "Toyota",
            VehicleModel     = "RAV4",
            VehicleVersion   = "2.0 D-4D Executive",
            VehicleYear      = 2019,
            VehicleMileage   = 147_500,
            VehicleVin       = "JTMBFREV60D012345",
            RegistrationPlate = "DK-1234-AB",
            VehicleReference = "YU10001",
            AgreedPrice      = 8_300_000m,
            SaleDate         = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero),
            SellerLegalName  = "Auto Dakar SARL",
            BuyerLegalName   = "Mamadou Diop",
            ValidatedAt      = DateTimeOffset.UtcNow,
            VerificationCode = "ABCDEFGHJKLMNPQR"
        };
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        return contract.Id;
    }

    // ─── Alta manual ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaDarDeAltaUnVehiculoDelUsuario()
    {
        var id = await AddAsync();

        var vehicle = await _context.GarageVehicles.SingleAsync(v => v.Id == id);
        vehicle.UserId.Should().Be(_userId);
        vehicle.Year.Should().Be(2019);
        vehicle.Mileage.Should().Be(147_500);
        // Matrícula y VIN se normalizan en mayúsculas.
        vehicle.RegistrationPlate.Should().Be("DK-1234-AB");
        vehicle.Vin.Should().Be("JTMBFREV60D012345");
        // Alta manual: no viene de ninguna compra en la plataforma.
        vehicle.BoughtOnYoonUAuto.Should().BeFalse();
    }

    [Fact]
    public async Task DeberiaPoderCrearseConLoMinimo()
    {
        // La especificación no exige completar toda la ficha para crear el vehículo.
        var minimal = new GarageVehicleInput(
            _makeId, null, null, 2015, null, null, null, null, null, null, null, null, null, null, null);

        var result = await _create.Handle(
            new CreateGarageVehicleCommand(_userId, minimal), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeberiaRechazarUnAnoImposible()
    {
        var result = await _create.Handle(
            new CreateGarageVehicleCommand(_userId, Input(year: 1750)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("GarageVehicle.InvalidYear");
    }

    [Fact]
    public async Task ElModeloDebePertenecerALaMarca()
    {
        var result = await _create.Handle(
            new CreateGarageVehicleCommand(_userId, Input(makeId: _otherMakeId, modelId: _modelId)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("VehicleModel.NotFound");
    }

    // ─── Ficha y kilometraje ───────────────────────────────────────────────

    [Fact]
    public async Task DeberiaActualizarElKilometraje()
    {
        var id = await AddAsync();

        var result = await _update.Handle(
            new UpdateGarageVehicleCommand(_userId, id, Input(mileage: 152_000)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.GarageVehicles.SingleAsync()).Mileage.Should().Be(152_000);
    }

    [Fact]
    public async Task ElKilometrajeNoDebePoderRetroceder()
    {
        var id = await AddAsync();

        var result = await _update.Handle(
            new UpdateGarageVehicleCommand(_userId, id, Input(mileage: 90_000)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("GarageVehicle.MileageWentBackwards");
    }

    [Fact]
    public async Task NadieMasDebePoderVerNiTocarElGaraje()
    {
        var id = await AddAsync();

        (await _detail.Handle(new GetGarageVehicleQuery(_otherUserId, id), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _update.Handle(new UpdateGarageVehicleCommand(_otherUserId, id, Input()), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _delete.Handle(new DeleteGarageVehicleCommand(_otherUserId, id), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");
    }

    [Fact]
    public async Task ElGarajeSoloDebeListarLosVehiculosDelUsuario()
    {
        await AddAsync();
        await AddAsync(_otherUserId);

        var result = await _garage.Handle(new GetMyGarageQuery(_userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VehicleCount.Should().Be(1);
        result.Value.Vehicles.Single().Title.Should().Be("Toyota RAV4 2.0 D-4D");
    }

    [Fact]
    public async Task RetirarUnVehiculoNoDebeBorrarloFisicamente()
    {
        var id = await AddAsync();

        await _delete.Handle(new DeleteGarageVehicleCommand(_userId, id), CancellationToken.None);

        (await _garage.Handle(new GetMyGarageQuery(_userId), CancellationToken.None))
            .Value!.VehicleCount.Should().Be(0);
        (await _context.GarageVehicles.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    // ─── Fotografías ───────────────────────────────────────────────────────

    [Fact]
    public async Task LaPrimeraFotografiaDebeSerLaPrincipal()
    {
        var id = await AddAsync();

        await _addImage.Handle(
            new AddGarageVehicleImageCommand(_userId, id, "/a.webp", "/a-thumb.webp", false, 0),
            CancellationToken.None);

        var image = await _context.GarageVehicleImages.SingleAsync();
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task SoloDebeHaberUnaFotografiaPrincipal()
    {
        var id = await AddAsync();
        await _addImage.Handle(
            new AddGarageVehicleImageCommand(_userId, id, "/a.webp", null, false, 0), CancellationToken.None);
        await _addImage.Handle(
            new AddGarageVehicleImageCommand(_userId, id, "/b.webp", null, true, 1), CancellationToken.None);

        var images = await _context.GarageVehicleImages.ToListAsync();
        images.Count(i => i.IsPrimary).Should().Be(1);
        images.Single(i => i.IsPrimary).Url.Should().Be("/b.webp");
    }

    [Fact]
    public async Task BorrarLaPrincipalDebeAscenderOtra()
    {
        var id = await AddAsync();
        var first = await _addImage.Handle(
            new AddGarageVehicleImageCommand(_userId, id, "/a.webp", null, false, 0), CancellationToken.None);
        await _addImage.Handle(
            new AddGarageVehicleImageCommand(_userId, id, "/b.webp", null, false, 1), CancellationToken.None);

        await _deleteImage.Handle(
            new DeleteGarageVehicleImageCommand(_userId, first.Value), CancellationToken.None);

        var remaining = await _context.GarageVehicleImages.SingleAsync();
        remaining.Url.Should().Be("/b.webp");
        remaining.IsPrimary.Should().BeTrue();
    }

    // ─── Vehículo comprado en Yoon u Auto ──────────────────────────────────

    [Fact]
    public async Task DeberiaPrecargarLaFichaDesdeElContrato()
    {
        var contractId = await ValidatedContractAsync();

        var result = await _prefill.Handle(
            new GetGaragePrefillFromContractQuery(_userId, contractId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AlreadyAdded.Should().BeFalse();
        result.Value.MakeName.Should().Be("Toyota");

        var v = result.Value.Vehicle;
        v.MakeId.Should().Be(_makeId);
        v.ModelId.Should().Be(_modelId);
        // Lo que las partes firmaron manda sobre el anuncio.
        v.Version.Should().Be("2.0 D-4D Executive");
        v.Mileage.Should().Be(147_500);
        v.Vin.Should().Be("JTMBFREV60D012345");
        v.RegistrationPlate.Should().Be("DK-1234-AB");
        v.PurchasePrice.Should().Be(8_300_000m);
        // Y lo técnico que el contrato no congela sale del anuncio.
        v.FuelType.Should().Be(FuelType.Diesel);
        v.Transmission.Should().Be(TransmissionType.Automatique);
        v.Color.Should().Be("Gris");
    }

    [Fact]
    public async Task SoloQuienCompraDebePoderIncorporarElVehiculo()
    {
        var contractId = await ValidatedContractAsync();

        (await _prefill.Handle(
            new GetGaragePrefillFromContractQuery(_otherUserId, contractId), CancellationToken.None))
            .Error.Should().Be("Contract.NotBuyer");

        // Quien vende tampoco puede forzarlo con el comando.
        var result = await _create.Handle(
            new CreateGarageVehicleCommand(_otherUserId, Input(), contractId), CancellationToken.None);
        result.Error.Should().Be("Contract.NotBuyer");
    }

    [Fact]
    public async Task UnContratoSinValidarNoDebeIncorporarNada()
    {
        var contractId = await ValidatedContractAsync();
        var contract = await _context.Contracts.SingleAsync();
        contract.Status = ContractStatus.AValider;
        await _context.SaveChangesAsync();

        var result = await _create.Handle(
            new CreateGarageVehicleCommand(_userId, Input(), contractId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotValidated");
    }

    [Fact]
    public async Task LaMismaCompraNoDebePoderIncorporarseDosVeces()
    {
        var contractId = await ValidatedContractAsync();
        await _create.Handle(
            new CreateGarageVehicleCommand(_userId, Input(), contractId), CancellationToken.None);

        var second = await _create.Handle(
            new CreateGarageVehicleCommand(_userId, Input(), contractId), CancellationToken.None);
        second.Error.Should().Be("GarageVehicle.AlreadyAdded");

        // Y la pantalla de incorporación lo refleja en lugar de volver a ofrecerlo.
        var prefill = await _prefill.Handle(
            new GetGaragePrefillFromContractQuery(_userId, contractId), CancellationToken.None);
        prefill.Value!.AlreadyAdded.Should().BeTrue();
        prefill.Value.ExistingGarageVehicleId.Should().NotBeNull();
    }

    [Fact]
    public async Task ElVehiculoIncorporadoDebeConservarSuOrigen()
    {
        var contractId = await ValidatedContractAsync();
        var result = await _create.Handle(
            new CreateGarageVehicleCommand(_userId, Input(), contractId), CancellationToken.None);

        var detail = await _detail.Handle(
            new GetGarageVehicleQuery(_userId, result.Value), CancellationToken.None);

        detail.Value!.BoughtOnYoonUAuto.Should().BeTrue();
        detail.Value.SourceVehicleId.Should().Be(_vehicleId);
    }

    public void Dispose() => _context.Dispose();
}
