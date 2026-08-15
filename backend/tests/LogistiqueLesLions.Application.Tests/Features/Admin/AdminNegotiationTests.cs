using System.Reflection;
using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Contracts;
using LogistiqueLesLions.Application.Features.Admin.Negotiations;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Gestion des négociations» y «Gestion des contrats et ventes».
/// </summary>
/// <remarks>
/// Dos reglas mandan aquí: el administrador <b>no lee conversaciones privadas</b> sin un
/// motivo que queda registrado, y <b>no valida contratos</b> en nombre de nadie.
/// </remarks>
public class AdminNegotiationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetAdminNegotiationsQueryHandler _list;
    private readonly GetAdminNegotiationQueryHandler _detail;
    private readonly AccessNegotiationContentCommandHandler _content;
    private readonly GetAdminContractsQueryHandler _contracts;
    private readonly GetAdminContractQueryHandler _contract;
    private readonly InvalidateContractCommandHandler _invalidate;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();
    private readonly Guid _negotiationId = Guid.NewGuid();

    public AdminNegotiationTests()
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
            new UserProfile
            {
                Id = _adminId, DisplayName = "Admin", Phone = "+221770000000",
                PasswordHash = "x", Role = UserRole.Admin
            },
            new UserProfile
            {
                Id = _buyerId, DisplayName = "Mamadou Diop", Phone = "+221770000001", PasswordHash = "x"
            },
            new UserProfile
            {
                Id = _sellerId, DisplayName = "Auto Dakar", Phone = "+221770000002",
                PasswordHash = "x", VerifiedSalesCount = 1
            });
        _context.Vehicles.Add(new Vehicle
        {
            Id = _vehicleId, PublicReference = "YU10001", Slug = "toyota-rav4-yu10001",
            Title = "Toyota RAV4 2019", MakeId = _makeId, Year = 2019, Price = 8_900_000m,
            SellerId = _sellerId, Status = VehicleStatus.Actif,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-5)
        });
        _context.SaveChanges();

        _list       = new GetAdminNegotiationsQueryHandler(_context);
        _detail     = new GetAdminNegotiationQueryHandler(_context);
        _content    = new AccessNegotiationContentCommandHandler(_context);
        _contracts  = new GetAdminContractsQueryHandler(_context);
        _contract   = new GetAdminContractQueryHandler(_context);
        _invalidate = new InvalidateContractCommandHandler(_context);
    }

    private async Task<Negotiation> NegotiationAsync(
        NegotiationStatus status = NegotiationStatus.EnCours, Guid? id = null)
    {
        var negotiation = new Negotiation
        {
            Id = id ?? _negotiationId,
            BuyerId = _buyerId,
            SellerId = _sellerId,
            VehicleId = _vehicleId,
            Status = status,
            LastActivityAt = DateTimeOffset.UtcNow.AddHours(-2)
        };
        _context.Negotiations.Add(negotiation);
        await _context.SaveChangesAsync();
        return negotiation;
    }

    private async Task<Contract> ContractAsync(
        Guid negotiationId, ContractStatus status = ContractStatus.Valide)
    {
        var contract = new Contract
        {
            PublicReference  = "YC00001",
            NegotiationId    = negotiationId,
            VehicleId        = _vehicleId,
            SellerId         = _sellerId,
            BuyerId          = _buyerId,
            CreatedByUserId  = _sellerId,
            Status           = status,
            VehicleMake      = "Toyota",
            VehicleModel     = "RAV4",
            VehicleYear      = 2019,
            VehicleReference = "YU10001",
            AgreedPrice      = 8_300_000m,
            SellerLegalName  = "Auto Dakar SARL",
            BuyerLegalName   = "Mamadou Diop",
            ValidatedAt      = status == ContractStatus.Valide ? DateTimeOffset.UtcNow : null,
            VerificationCode = status == ContractStatus.Valide ? "ABCDEFGHJKLMNPQR" : null
        };
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        return contract;
    }

    // ─── Privacidad de las conversaciones ──────────────────────────────────

    [Fact]
    public async Task ElListadoNoDebeExponerElContenidoDeLosMensajes()
    {
        var negotiation = await NegotiationAsync();
        _context.Messages.Add(new Message
        {
            NegotiationId = negotiation.Id, SenderId = _buyerId, Body = "Bonjour, c'est négociable ?"
        });
        await _context.SaveChangesAsync();

        var result = await _list.Handle(new GetAdminNegotiationsQuery(), CancellationToken.None);

        // Se sabe que hay un mensaje, no lo que dice.
        result.Value!.Items.Single().MessagesCount.Should().Be(1);

        typeof(AdminNegotiationRowDto).GetProperties().Select(p => p.Name)
            .Should().NotContain("Body");
    }

    [Fact]
    public void LaFichaEstructuralNoDebeTraerMensajes()
    {
        // Si el DTO de la ficha llevara los mensajes, el registro de acceso no serviría
        // de nada: bastaría con abrir la ficha.
        typeof(AdminNegotiationDetailDto).GetProperties()
            .Select(p => p.PropertyType)
            .Should().NotContain(t =>
                t.IsGenericType && t.GetGenericArguments().Contains(typeof(AdminMessageDto)));
    }

    [Fact]
    public async Task LeerUnaConversacionExigeExplicarPorQue()
    {
        var negotiation = await NegotiationAsync();

        var result = await _content.Handle(new AccessNegotiationContentCommand(
            _adminId, negotiation.Id, ContentAccessReason.Dispute, "   "), CancellationToken.None);

        result.Error.Should().Be("Admin.ReasonRequired");
        (await _context.AdminActions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LeerUnaConversacionDebeQuedarRegistrado()
    {
        var negotiation = await NegotiationAsync();

        // Se guardan por separado, como ocurre de verdad: el interceptor de auditoría
        // fija una sola marca de tiempo por guardado, y dos mensajes escritos «a la vez»
        // no tendrían orden.
        _context.Messages.Add(new Message
        {
            NegotiationId = negotiation.Id, SenderId = _buyerId, Body = "Bonjour"
        });
        await _context.SaveChangesAsync();

        await Task.Delay(10);
        _context.Messages.Add(new Message
        {
            NegotiationId = negotiation.Id, SenderId = _sellerId, Body = "Bonsoir"
        });
        await _context.SaveChangesAsync();

        var result = await _content.Handle(new AccessNegotiationContentCommand(
            _adminId, negotiation.Id, ContentAccessReason.FraudInvestigation,
            "Signalement reçu le 12/08"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value[0].FromBuyer.Should().BeTrue();

        // El acceso deja huella con el supuesto y el porqué concreto.
        var action = await _context.AdminActions.SingleAsync();
        action.Type.Should().Be(AdminActionType.NegotiationContentAccessed);
        action.TargetType.Should().Be(AdminTargetType.Negotiation);
        action.Reason.Should().Contain("FraudInvestigation");
        action.Reason.Should().Contain("Signalement reçu");
    }

    [Fact]
    public async Task LosAccesosDebenVerseEnLaFicha()
    {
        var negotiation = await NegotiationAsync();
        await _content.Handle(new AccessNegotiationContentCommand(
            _adminId, negotiation.Id, ContentAccessReason.SupportRequested, "Demande du vendeur"),
            CancellationToken.None);

        var detail = await _detail.Handle(
            new GetAdminNegotiationQuery(negotiation.Id), CancellationToken.None);

        detail.Value!.Actions.Should().ContainSingle()
            .Which.Type.Should().Be(AdminActionType.NegotiationContentAccessed);
    }

    // ─── Datos estructurales ───────────────────────────────────────────────

    [Fact]
    public async Task ElListadoDebeMostrarElContratoAsociado()
    {
        var negotiation = await NegotiationAsync(NegotiationStatus.Terminee);
        await ContractAsync(negotiation.Id);

        var result = await _list.Handle(new GetAdminNegotiationsQuery(), CancellationToken.None);

        var row = result.Value!.Items.Single();
        row.ContractReference.Should().Be("YC00001");
        row.ContractStatus.Should().Be(ContractStatus.Valide);
    }

    [Fact]
    public async Task DeberiaPoderFiltrarseLoQueNoTieneContrato()
    {
        var withContract = await NegotiationAsync(NegotiationStatus.Terminee, Guid.NewGuid());
        await ContractAsync(withContract.Id);
        var without = await NegotiationAsync(NegotiationStatus.EnCours, Guid.NewGuid());

        var result = await _list.Handle(
            new GetAdminNegotiationsQuery(WithContract: false), CancellationToken.None);

        result.Value!.Items.Should().ContainSingle().Which.Id.Should().Be(without.Id);
    }

    [Fact]
    public async Task LaFichaDebeTraerOfertasYCronologia()
    {
        var negotiation = await NegotiationAsync();
        _context.Offers.Add(new Offer
        {
            NegotiationId = negotiation.Id, FromUserId = _buyerId,
            Amount = 8_300_000m, ListedPrice = 8_900_000m
        });
        _context.NegotiationEvents.AddRange(
            new NegotiationEvent { NegotiationId = negotiation.Id, Sequence = 1,
                                   Type = NegotiationEventType.ConversationStarted },
            new NegotiationEvent { NegotiationId = negotiation.Id, Sequence = 2,
                                   Type = NegotiationEventType.OfferMade, Amount = 8_300_000m });
        await _context.SaveChangesAsync();

        var detail = await _detail.Handle(
            new GetAdminNegotiationQuery(negotiation.Id), CancellationToken.None);

        detail.Value!.Offers.Should().ContainSingle()
            .Which.FromBuyer.Should().BeTrue();
        detail.Value.Timeline.Should().HaveCount(2);
        detail.Value.Timeline[1].Type.Should().Be(NegotiationEventType.OfferMade);
    }

    // ─── Contratos ─────────────────────────────────────────────────────────

    [Fact]
    public async Task NoDebeExistirNingunaViaParaValidarUnContrato()
    {
        // La validación pertenece a las partes: ningún comando del backoffice puede
        // dejar un contrato en «Validé».
        var adminCommands = typeof(InvalidateContractCommand).Assembly
            .GetTypes()
            .Where(t => t.Namespace is not null
                     && t.Namespace.StartsWith("LogistiqueLesLions.Application.Features.Admin")
                     && t.Name.EndsWith("CommandHandler"))
            .ToList();

        adminCommands.Should().NotBeEmpty();

        foreach (var handler in adminCommands)
        {
            var source = handler.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(m => m.Name == "Handle");
            source.Should().BeTrue();
        }

        // Y el único comando sobre contratos es el de invalidar.
        typeof(InvalidateContractCommand).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "LogistiqueLesLions.Application.Features.Admin.Contracts"
                     && t.Name.EndsWith("Command"))
            .Select(t => t.Name)
            .Should().BeEquivalentTo([nameof(InvalidateContractCommand)]);
    }

    [Fact]
    public async Task InvalidarExigeMotivo()
    {
        var negotiation = await NegotiationAsync(NegotiationStatus.Terminee);
        var contract = await ContractAsync(negotiation.Id);

        var result = await _invalidate.Handle(
            new InvalidateContractCommand(_adminId, contract.Id, "  "), CancellationToken.None);

        result.Error.Should().Be("Admin.ReasonRequired");
    }

    [Fact]
    public async Task InvalidarUnaVentaVerificadaDebeDeshacerLaReputacion()
    {
        var negotiation = await NegotiationAsync(NegotiationStatus.Terminee);
        var contract = await ContractAsync(negotiation.Id);

        var result = await _invalidate.Handle(new InvalidateContractCommand(
            _adminId, contract.Id, "Fraude avérée"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var invalidated = await _context.Contracts.SingleAsync();
        invalidated.Status.Should().Be(ContractStatus.Annule);
        invalidated.CancelledAt.Should().NotBeNull();

        // La reputación no puede sostenerse sobre un contrato invalidado.
        (await _context.UserProfiles.SingleAsync(u => u.Id == _sellerId))
            .VerifiedSalesCount.Should().Be(0);

        // Las dos partes se enteran.
        var notifications = await _context.UserNotifications.ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Select(n => n.UserId).Should().BeEquivalentTo([_sellerId, _buyerId]);
    }

    [Fact]
    public async Task InvalidarUnBorradorNoDebeTocarLaReputacion()
    {
        var negotiation = await NegotiationAsync();
        var contract = await ContractAsync(negotiation.Id, ContractStatus.Brouillon);

        await _invalidate.Handle(new InvalidateContractCommand(
            _adminId, contract.Id, "Doublon"), CancellationToken.None);

        // No había venta verificada que deshacer.
        (await _context.UserProfiles.SingleAsync(u => u.Id == _sellerId))
            .VerifiedSalesCount.Should().Be(1);
    }

    [Fact]
    public async Task NoDebeInvalidarseDosVeces()
    {
        var negotiation = await NegotiationAsync(NegotiationStatus.Terminee);
        var contract = await ContractAsync(negotiation.Id);

        await _invalidate.Handle(new InvalidateContractCommand(
            _adminId, contract.Id, "Fraude"), CancellationToken.None);

        var second = await _invalidate.Handle(new InvalidateContractCommand(
            _adminId, contract.Id, "Fraude"), CancellationToken.None);

        second.Error.Should().Be("Contract.AlreadyCancelled");
        // Y la reputación no baja dos veces.
        (await _context.UserProfiles.SingleAsync(u => u.Id == _sellerId))
            .VerifiedSalesCount.Should().Be(0);
    }

    [Fact]
    public async Task LaFichaDelContratoDebeTraerElCodigoDeVerificacion()
    {
        var negotiation = await NegotiationAsync(NegotiationStatus.Terminee);
        var contract = await ContractAsync(negotiation.Id);

        var detail = await _contract.Handle(
            new GetAdminContractQuery(contract.Id), CancellationToken.None);

        detail.Value!.VerificationCode.Should().Be("ABCDEFGHJKLMNPQR");
        detail.Value.Contract.IsVerifiedSale.Should().BeTrue();
    }

    [Fact]
    public async Task DeberiaPoderListarseSoloLasVentasVerificadas()
    {
        var first = await NegotiationAsync(NegotiationStatus.Terminee, Guid.NewGuid());
        await ContractAsync(first.Id);

        var second = await NegotiationAsync(NegotiationStatus.EnCours, Guid.NewGuid());
        var draft = await ContractAsync(second.Id, ContractStatus.Brouillon);
        draft.PublicReference = "YC00002";
        await _context.SaveChangesAsync();

        var result = await _contracts.Handle(
            new GetAdminContractsQuery(VerifiedSalesOnly: true), CancellationToken.None);

        result.Value!.Items.Should().ContainSingle()
            .Which.PublicReference.Should().Be("YC00001");
    }

    public void Dispose() => _context.Dispose();
}
