using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Negotiations;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Negotiations;

/// <summary>
/// Contrato de venta y venta verificada.
/// </summary>
/// <remarks>
/// Lo delicado es quién puede hacer qué: redacta una parte y valida siempre la otra, y
/// los datos del contrato no pueden moverse cuando cambia el anuncio.
/// </remarks>
public class ContractTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MakeOfferCommandHandler _make;
    private readonly AcceptOfferCommandHandler _accept;
    private readonly CreateContractCommandHandler _create;
    private readonly UpdateContractCommandHandler _update;
    private readonly SendContractCommandHandler _send;
    private readonly ValidateContractCommandHandler _validate;
    private readonly RequestContractChangesCommandHandler _requestChanges;
    private readonly CancelContractCommandHandler _cancel;
    private readonly GetContractQueryHandler _query;
    private readonly GetContractDocumentQueryHandler _document;
    private readonly VerifyContractQueryHandler _verify;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    private int _sequence;

    public ContractTests()
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
            new UserProfile { Id = _buyerId, DisplayName = "Mamadou Diop", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000002", PasswordHash = "x" },
            new UserProfile { Id = _strangerId, DisplayName = "Fatou", Phone = "+221770000003", PasswordHash = "x" });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId,
            PublicReference = "YU10001",
            Slug = "toyota-rav4-yu10001",
            Title = "Toyota RAV4 2019",
            MakeId = _makeId,
            Year = 2019,
            Mileage = 78_000,
            Vin = "JTMBFREV60D012345",
            Price = 8_900_000m,
            SellerId = _sellerId,
            Status = VehicleStatus.Actif
        });
        _context.SaveChanges();

        var references = new Mock<IPublicReferenceGenerator>();
        references
            .Setup(r => r.NextContractReferenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"YC{++_sequence:D5}");

        _make           = new MakeOfferCommandHandler(_context);
        _accept         = new AcceptOfferCommandHandler(_context);
        _create         = new CreateContractCommandHandler(_context, references.Object);
        _update         = new UpdateContractCommandHandler(_context);
        _send           = new SendContractCommandHandler(_context);
        _validate       = new ValidateContractCommandHandler(_context);
        _requestChanges = new RequestContractChangesCommandHandler(_context);
        _cancel         = new CancelContractCommandHandler(_context);
        _query          = new GetContractQueryHandler(_context);
        _document       = new GetContractDocumentQueryHandler(_context);
        _verify         = new VerifyContractQueryHandler(_context);
    }

    /// <summary>Deja una negociación con una oferta aceptada, que es el punto de partida.</summary>
    private async Task<Guid> AgreeAsync(decimal amount = 8_300_000m)
    {
        var offer = await _make.Handle(
            new MakeOfferCommand(_buyerId, _vehicleId, amount, null), CancellationToken.None);

        await _accept.Handle(
            new AcceptOfferCommand(_sellerId, offer.Value!.OfferId!.Value), CancellationToken.None);

        return offer.Value!.NegotiationId;
    }

    private CreateContractCommand NewContract(Guid negotiationId, Guid author, decimal price = 8_300_000m) =>
        new(author, negotiationId, price, "DK-1234-AB",
            "Auto Dakar SARL", "SN0012345", "Sacré-Cœur 3, Dakar",
            "Mamadou Diop", "1234567890123", "Yoff, Dakar");

    private async Task<Guid> CreateAsync(Guid negotiationId, Guid author)
    {
        var result = await _create.Handle(NewContract(negotiationId, author), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    // ─── Crear ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaCongelarLosDatosDelVehiculoAlCrearElContrato()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);

        var contract = await _context.Contracts.SingleAsync(c => c.Id == contractId);
        contract.PublicReference.Should().Be("YC00001");
        contract.Status.Should().Be(ContractStatus.Brouillon);
        contract.VehicleMake.Should().Be("Toyota");
        contract.VehicleYear.Should().Be(2019);
        contract.VehicleMileage.Should().Be(78_000);
        contract.VehicleVin.Should().Be("JTMBFREV60D012345");
        contract.VehicleReference.Should().Be("YU10001");
        contract.AgreedPrice.Should().Be(8_300_000m);
        contract.RegistrationPlate.Should().Be("DK-1234-AB");
    }

    [Fact]
    public async Task LosDatosCongeladosNoDebenCambiarAunqueCambieElAnuncio()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);

        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Price = 9_500_000m;
        vehicle.Mileage = 82_000;
        await _context.SaveChangesAsync();

        var contract = await _context.Contracts.SingleAsync(c => c.Id == contractId);
        contract.AgreedPrice.Should().Be(8_300_000m);
        contract.VehicleMileage.Should().Be(78_000);
    }

    [Fact]
    public async Task UnTerceroNoDebePoderCrearElContrato()
    {
        var negotiationId = await AgreeAsync();

        var result = await _create.Handle(
            NewContract(negotiationId, _strangerId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Negotiation.AccessDenied");
    }

    [Fact]
    public async Task NoDebePoderHaberDosContratosVivosEnLaMismaNegociacion()
    {
        var negotiationId = await AgreeAsync();
        await CreateAsync(negotiationId, _sellerId);

        var result = await _create.Handle(
            NewContract(negotiationId, _buyerId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.AlreadyExists");
    }

    [Fact]
    public async Task UnContratoAnuladoDebePermitirRedactarOtro()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _cancel.Handle(new CancelContractCommand(_sellerId, contractId), CancellationToken.None);

        var result = await _create.Handle(
            NewContract(negotiationId, _buyerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ─── Editar y enviar ───────────────────────────────────────────────────

    [Fact]
    public async Task SoloElAutorDebePoderCorregirElContrato()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);

        var result = await _update.Handle(
            new UpdateContractCommand(_buyerId, contractId, 8_000_000m, "DK-1234-AB",
                "Auto Dakar SARL", null, null, "Mamadou Diop", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotAuthor");
    }

    [Fact]
    public async Task EnviarDebeDejarloAValidarYAvisarALaOtraParte()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);

        var result = await _send.Handle(
            new SendContractCommand(_sellerId, contractId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var contract = await _context.Contracts.SingleAsync();
        contract.Status.Should().Be(ContractStatus.AValider);
        contract.SentAt.Should().NotBeNull();
        // Quien valida es siempre la parte que no lo redactó.
        contract.ValidatorId.Should().Be(_buyerId);

        var notification = await _context.UserNotifications
            .SingleAsync(n => n.Category == NotificationCategories.Contract);
        notification.UserId.Should().Be(_buyerId);
        notification.Title.Should().Be("Contrat à valider");
    }

    // ─── Validar ───────────────────────────────────────────────────────────

    [Fact]
    public async Task QuienRedactaElContratoNoDebePoderValidarlo()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);

        var result = await _validate.Handle(
            new ValidateContractCommand(_sellerId, contractId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotValidator");
    }

    [Fact]
    public async Task NoDebePoderValidarseUnBorradorSinEnviar()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);

        var result = await _validate.Handle(
            new ValidateContractCommand(_buyerId, contractId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotAwaitingValidation");
    }

    [Fact]
    public async Task ValidarDebeCerrarLaVentaEnteraComoVerificada()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);

        var result = await _validate.Handle(
            new ValidateContractCommand(_buyerId, contractId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var contract = await _context.Contracts.SingleAsync();
        contract.Status.Should().Be(ContractStatus.Valide);
        contract.ValidatedAt.Should().NotBeNull();

        // El anuncio pasa a vendido y la negociación se cierra.
        var vehicle = await _context.Vehicles.SingleAsync();
        vehicle.Status.Should().Be(VehicleStatus.Vendu);
        vehicle.SoldAt.Should().NotBeNull();

        var negotiation = await _context.Negotiations.SingleAsync(n => n.Id == negotiationId);
        negotiation.Status.Should().Be(NegotiationStatus.Terminee);
        negotiation.ClosedAt.Should().NotBeNull();

        // +1 vente vérifiée para quien vende.
        var seller = await _context.UserProfiles.SingleAsync(u => u.Id == _sellerId);
        seller.VerifiedSalesCount.Should().Be(1);
    }

    [Fact]
    public async Task LaCronologiaDebeTerminarEnVentaVerificada()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);
        await _validate.Handle(new ValidateContractCommand(_buyerId, contractId), CancellationToken.None);

        // Se ordena por Sequence: los hitos de una misma operación comparten CreatedAt.
        var events = await _context.NegotiationEvents.OrderBy(e => e.Sequence).ToListAsync();

        events.Select(e => e.Type).Should().ContainInOrder(
            NegotiationEventType.ConversationStarted,
            NegotiationEventType.OfferMade,
            NegotiationEventType.OfferAccepted,
            NegotiationEventType.ContractCreated,
            NegotiationEventType.ContractValidated,
            NegotiationEventType.SaleVerified);

        events.Last().Type.Should().Be(NegotiationEventType.SaleVerified);
        events.Last().Amount.Should().Be(8_300_000m);
    }

    [Fact]
    public async Task UnContratoValidadoNoDebePoderModificarseNiAnularse()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);
        await _validate.Handle(new ValidateContractCommand(_buyerId, contractId), CancellationToken.None);

        var update = await _update.Handle(
            new UpdateContractCommand(_sellerId, contractId, 1_000_000m, null,
                "Auto Dakar SARL", null, null, "Mamadou Diop", null, null),
            CancellationToken.None);
        update.Error.Should().Be("Contract.NotEditable");

        var cancel = await _cancel.Handle(
            new CancelContractCommand(_sellerId, contractId), CancellationToken.None);
        cancel.Error.Should().Be("Contract.AlreadyValidated");
    }

    // ─── Pedir modificación ────────────────────────────────────────────────

    [Fact]
    public async Task PedirModificacionDebeDevolverElContratoAEditable()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);

        var result = await _requestChanges.Handle(
            new RequestContractChangesCommand(_buyerId, contractId, "Le kilométrage est erroné."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var contract = await _context.Contracts.SingleAsync();
        contract.Status.Should().Be(ContractStatus.ModificationDemandee);
        contract.IsEditable.Should().BeTrue();
        contract.ChangeRequestNotes.Should().Be("Le kilométrage est erroné.");

        // Y el autor puede corregirlo sin volver a crearlo.
        var update = await _update.Handle(
            new UpdateContractCommand(_sellerId, contractId, 8_300_000m, "DK-1234-AB",
                "Auto Dakar SARL", null, null, "Mamadou Diop", null, null),
            CancellationToken.None);
        update.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ElAutorNoDebePoderPedirseModificacionesASiMismo()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);

        var result = await _requestChanges.Handle(
            new RequestContractChangesCommand(_sellerId, contractId, "..."), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotValidator");
    }

    // ─── Pestaña «Contrat» ─────────────────────────────────────────────────

    [Fact]
    public async Task SinContratoLaPestanaDebeTraerLosDatosPrecargados()
    {
        var negotiationId = await AgreeAsync();

        var result = await _query.Handle(
            new GetContractQuery(_buyerId, negotiationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Contract.Should().BeNull();
        result.Value.CanCreate.Should().BeTrue();
        result.Value.IsSeller.Should().BeFalse();

        var prefill = result.Value.Prefill;
        prefill.VehicleMake.Should().Be("Toyota");
        prefill.VehicleReference.Should().Be("YU10001");
        // El importe sugerido es el de la oferta aceptada, no el del anuncio.
        prefill.SuggestedPrice.Should().Be(8_300_000m);
        prefill.SellerLegalName.Should().Be("Auto Dakar");
        prefill.BuyerLegalName.Should().Be("Mamadou Diop");
    }

    [Fact]
    public async Task LaPestanaDebeOfrecerCadaAccionSoloAQuienLeCorresponde()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);

        var forAuthor = (await _query.Handle(
            new GetContractQuery(_sellerId, negotiationId), CancellationToken.None)).Value!.Contract!;
        forAuthor.CreatedByMe.Should().BeTrue();
        forAuthor.CanValidate.Should().BeFalse();
        forAuthor.CanRequestChanges.Should().BeFalse();
        // Enviado, ya no admite correcciones hasta que la otra parte las pida.
        forAuthor.CanEdit.Should().BeFalse();

        var forValidator = (await _query.Handle(
            new GetContractQuery(_buyerId, negotiationId), CancellationToken.None)).Value!.Contract!;
        forValidator.CreatedByMe.Should().BeFalse();
        forValidator.CanValidate.Should().BeTrue();
        forValidator.CanRequestChanges.Should().BeTrue();
        forValidator.CanEdit.Should().BeFalse();
    }

    // ─── PDF y QR de verificación ──────────────────────────────────────────

    private async Task<Guid> ValidatedContractAsync()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);
        await _validate.Handle(new ValidateContractCommand(_buyerId, contractId), CancellationToken.None);
        return contractId;
    }

    [Fact]
    public async Task ValidarDebeGenerarElCodigoDeVerificacion()
    {
        await ValidatedContractAsync();

        var contract = await _context.Contracts.SingleAsync();
        contract.VerificationCode.Should().NotBeNullOrEmpty();
        contract.VerificationCode!.Length.Should().Be(16);
        // Sin caracteres que se confundan al leerlos de un papel.
        contract.VerificationCode.Should().NotContainAny("0", "1", "I", "O");
        // No se deriva de la referencia pública, que sí es adivinable.
        contract.VerificationCode.Should().NotContain(contract.PublicReference);
    }

    [Fact]
    public async Task UnContratoSinValidarNoDebePoderDescargarse()
    {
        var negotiationId = await AgreeAsync();
        var contractId = await CreateAsync(negotiationId, _sellerId);
        await _send.Handle(new SendContractCommand(_sellerId, contractId), CancellationToken.None);

        var result = await _document.Handle(
            new GetContractDocumentQuery(_buyerId, contractId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotValidated");
    }

    [Fact]
    public async Task UnTerceroNoDebePoderDescargarElContrato()
    {
        var contractId = await ValidatedContractAsync();

        var result = await _document.Handle(
            new GetContractDocumentQuery(_strangerId, contractId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Negotiation.AccessDenied");
    }

    [Fact]
    public async Task LasDosPartesDebenPoderDescargarElContrato()
    {
        var contractId = await ValidatedContractAsync();

        foreach (var userId in new[] { _sellerId, _buyerId })
        {
            var result = await _document.Handle(
                new GetContractDocumentQuery(userId, contractId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.PublicReference.Should().Be("YC00001");
            result.Value.VerificationCode.Should().NotBeNullOrEmpty();
            // El teléfono es el identificador de la cuenta y figura como contacto.
            result.Value.SellerPhone.Should().Be("+221770000002");
            result.Value.BuyerPhone.Should().Be("+221770000001");
        }
    }

    [Fact]
    public async Task ElCodigoDelQrDebeDevolverLaVentaVerificada()
    {
        await ValidatedContractAsync();
        var code = (await _context.Contracts.SingleAsync()).VerificationCode!;

        var result = await _verify.Handle(new VerifyContractQuery(code), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PublicReference.Should().Be("YC00001");
        result.Value.VehicleMake.Should().Be("Toyota");
        result.Value.VehicleReference.Should().Be("YU10001");
        result.Value.AgreedPrice.Should().Be(8_300_000m);
        result.Value.SellerLegalName.Should().Be("Auto Dakar SARL");
    }

    [Fact]
    public async Task ElCodigoDebeTolerarMayusculasYEspacios()
    {
        await ValidatedContractAsync();
        var code = (await _context.Contracts.SingleAsync()).VerificationCode!;

        var result = await _verify.Handle(
            new VerifyContractQuery($"  {code.ToLowerInvariant()} "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UnCodigoDesconocidoNoDebeVerificarNada()
    {
        await ValidatedContractAsync();

        var result = await _verify.Handle(
            new VerifyContractQuery("AAAABBBBCCCCDDDD"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotFound");
    }

    [Fact]
    public async Task UnContratoQueDejaDeEstarValidadoNoDebeVerificarse()
    {
        await ValidatedContractAsync();
        var contract = await _context.Contracts.SingleAsync();
        var code = contract.VerificationCode!;

        // Solo un administrador puede invalidarlo, pero el código sobrevive en la fila:
        // la comprobación pública debe mirar el estado, no la mera existencia del código.
        contract.Status = ContractStatus.Annule;
        await _context.SaveChangesAsync();

        var result = await _verify.Handle(new VerifyContractQuery(code), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Contract.NotFound");
    }

    [Fact]
    public void LaVerificacionPublicaNoDebeExponerDatosPersonales()
    {
        // La página del QR es pública: bloquea que un cambio futuro cuele en ella
        // documentos de identidad, direcciones o teléfonos.
        var exposed = typeof(ContractVerificationDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        exposed.Should().NotContain(new[]
        {
            nameof(ContractDocumentDto.SellerIdDocument),
            nameof(ContractDocumentDto.SellerAddress),
            nameof(ContractDocumentDto.SellerPhone),
            nameof(ContractDocumentDto.BuyerIdDocument),
            nameof(ContractDocumentDto.BuyerAddress),
            nameof(ContractDocumentDto.BuyerPhone)
        });
    }

    [Fact]
    public async Task UnTerceroNoDebePoderVerElContrato()
    {
        var negotiationId = await AgreeAsync();
        await CreateAsync(negotiationId, _sellerId);

        var result = await _query.Handle(
            new GetContractQuery(_strangerId, negotiationId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Negotiation.AccessDenied");
    }

    public void Dispose() => _context.Dispose();
}
