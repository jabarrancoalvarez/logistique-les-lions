using System.Security.Cryptography;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Negotiations;

internal static class ContractWorkflow
{
    public static UserNotification Notify(
        IApplicationDbContext db, Guid userId, string title, string body, Guid negotiationId)
    {
        var notification = new UserNotification
        {
            UserId   = userId,
            Category = NotificationCategories.Contract,
            Title    = title,
            Body     = body,
            Link     = $"/mis-negociaciones/{negotiationId}"
        };
        db.UserNotifications.Add(notification);
        return notification;
    }

    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Alfabeto sin caracteres que se confunden al leerlos de un papel.</summary>
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>
    /// Código del QR de verificación: 16 caracteres aleatorios.
    /// </summary>
    /// <remarks>
    /// No se deriva de la referencia pública ni de ningún dato del contrato: quien
    /// conoce una referencia <c>#YC00125</c> no debe poder deducir el código con el que
    /// se consulta la venta.
    /// </remarks>
    public static string NewVerificationCode() =>
        RandomNumberGenerator.GetString(CodeAlphabet, 16);
}

public class CreateContractCommandHandler(
    IApplicationDbContext db,
    IPublicReferenceGenerator references)
    : IRequestHandler<CreateContractCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateContractCommand request, CancellationToken ct)
    {
        if (request.AgreedPrice <= 0) return Result<Guid>.Failure("Contract.InvalidPrice");

        var negotiation = await db.Negotiations
            .Include(n => n.Vehicle).ThenInclude(v => v.Make)
            .Include(n => n.Vehicle).ThenInclude(v => v.Model)
            .FirstOrDefaultAsync(n => n.Id == request.NegotiationId, ct);

        if (negotiation is null) return Result<Guid>.Failure("Negotiation.NotFound");
        if (!negotiation.Involves(request.UserId)) return Result<Guid>.Failure("Negotiation.AccessDenied");

        // Un solo contrato vivo por negociación: si ya hay uno, se edita, no se duplica.
        // Uno anulado sí permite redactar otro.
        var hasLiveContract = await db.Contracts
            .AnyAsync(c => c.NegotiationId == negotiation.Id
                        && c.Status != ContractStatus.Annule, ct);

        if (hasLiveContract) return Result<Guid>.Failure("Contract.AlreadyExists");

        var vehicle = negotiation.Vehicle;
        var reference = await references.NextContractReferenceAsync(ct);

        var contract = new Contract
        {
            PublicReference   = reference,
            NegotiationId     = negotiation.Id,
            VehicleId         = vehicle.Id,
            SellerId          = negotiation.SellerId,
            BuyerId           = negotiation.BuyerId,
            CreatedByUserId   = request.UserId,
            Status            = ContractStatus.Brouillon,

            // Datos del vehículo congelados en el momento del acuerdo.
            VehicleMake       = vehicle.Make.Name,
            VehicleModel      = vehicle.Model?.Name,
            VehicleVersion    = vehicle.Version,
            VehicleYear       = vehicle.Year,
            VehicleMileage    = vehicle.Mileage,
            VehicleVin        = vehicle.Vin,
            RegistrationPlate = ContractWorkflow.Clean(request.RegistrationPlate),
            VehicleReference  = vehicle.PublicReference,

            AgreedPrice       = request.AgreedPrice,
            SaleDate          = DateTimeOffset.UtcNow,

            SellerLegalName   = request.SellerLegalName.Trim(),
            SellerIdDocument  = ContractWorkflow.Clean(request.SellerIdDocument),
            SellerAddress     = ContractWorkflow.Clean(request.SellerAddress),
            BuyerLegalName    = request.BuyerLegalName.Trim(),
            BuyerIdDocument   = ContractWorkflow.Clean(request.BuyerIdDocument),
            BuyerAddress      = ContractWorkflow.Clean(request.BuyerAddress)
        };

        db.Contracts.Add(contract);

        var timeline = await OfferWorkflow.OpenTimelineAsync(db, negotiation.Id, ct);
        timeline.Add(NegotiationEventType.ContractCreated, request.UserId);

        negotiation.LastActivityAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(contract.Id);
    }
}

/// <summary>Comprobaciones comunes de los comandos que operan sobre un contrato.</summary>
public abstract class ContractHandlerBase(IApplicationDbContext db)
{
    protected async Task<(Contract? contract, string? error)> LoadAsync(
        Guid userId, Guid contractId, CancellationToken ct)
    {
        var contract = await db.Contracts
            .Include(c => c.Negotiation)
            .Include(c => c.Vehicle)
            .FirstOrDefaultAsync(c => c.Id == contractId, ct);

        if (contract is null) return (null, "Contract.NotFound");

        if (!contract.Negotiation.Involves(userId))
            return (null, "Negotiation.AccessDenied");

        return (contract, null);
    }
}

public class UpdateContractCommandHandler(IApplicationDbContext db)
    : ContractHandlerBase(db), IRequestHandler<UpdateContractCommand, Result>
{
    public async Task<Result> Handle(UpdateContractCommand request, CancellationToken ct)
    {
        var (contract, error) = await LoadAsync(request.UserId, request.ContractId, ct);
        if (error is not null) return Result.Failure(error);

        // Un contrato validado ya no puede modificarse.
        if (!contract!.IsEditable) return Result.Failure("Contract.NotEditable");

        if (contract.CreatedByUserId != request.UserId)
            return Result.Failure("Contract.NotAuthor");

        if (request.AgreedPrice <= 0) return Result.Failure("Contract.InvalidPrice");

        contract.AgreedPrice       = request.AgreedPrice;
        contract.RegistrationPlate = ContractWorkflow.Clean(request.RegistrationPlate);
        contract.SellerLegalName   = request.SellerLegalName.Trim();
        contract.SellerIdDocument  = ContractWorkflow.Clean(request.SellerIdDocument);
        contract.SellerAddress     = ContractWorkflow.Clean(request.SellerAddress);
        contract.BuyerLegalName    = request.BuyerLegalName.Trim();
        contract.BuyerIdDocument   = ContractWorkflow.Clean(request.BuyerIdDocument);
        contract.BuyerAddress      = ContractWorkflow.Clean(request.BuyerAddress);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class SendContractCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : ContractHandlerBase(db), IRequestHandler<SendContractCommand, Result>
{
    public async Task<Result> Handle(SendContractCommand request, CancellationToken ct)
    {
        var (contract, error) = await LoadAsync(request.UserId, request.ContractId, ct);
        if (error is not null) return Result.Failure(error);

        if (!contract!.IsEditable) return Result.Failure("Contract.NotEditable");
        if (contract.CreatedByUserId != request.UserId) return Result.Failure("Contract.NotAuthor");

        var now = DateTimeOffset.UtcNow;

        contract.Status            = ContractStatus.AValider;
        contract.SentAt            = now;
        contract.ChangeRequestNotes = null;

        // Mientras espera validación, la negociación queda a la espera.
        contract.Negotiation.Status         = NegotiationStatus.EnAttente;
        contract.Negotiation.LastActivityAt = now;

        var notification = ContractWorkflow.Notify(db, contract.ValidatorId,
            "Contrat à valider",
            $"Le contrat #{contract.PublicReference} attend votre validation.",
            contract.NegotiationId);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}

public class RequestContractChangesCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : ContractHandlerBase(db), IRequestHandler<RequestContractChangesCommand, Result>
{
    public async Task<Result> Handle(RequestContractChangesCommand request, CancellationToken ct)
    {
        var (contract, error) = await LoadAsync(request.UserId, request.ContractId, ct);
        if (error is not null) return Result.Failure(error);

        if (!contract!.AwaitsValidation) return Result.Failure("Contract.NotAwaitingValidation");

        // Solo pide cambios quien tiene que validarlo.
        if (contract.ValidatorId != request.UserId) return Result.Failure("Contract.NotValidator");

        var notes = request.Notes.Trim();
        if (string.IsNullOrEmpty(notes)) return Result.Failure("Contract.ChangeNotesRequired");

        var now = DateTimeOffset.UtcNow;

        contract.Status             = ContractStatus.ModificationDemandee;
        contract.ChangeRequestNotes = notes;
        contract.ChangeRequestedAt  = now;

        var timeline = await OfferWorkflow.OpenTimelineAsync(db, contract.NegotiationId, ct);
        timeline.Add(NegotiationEventType.ContractChangeRequested, request.UserId);

        contract.Negotiation.Status         = NegotiationStatus.EnCours;
        contract.Negotiation.LastActivityAt = now;

        var notification = ContractWorkflow.Notify(db, contract.CreatedByUserId,
            "Modification demandée",
            $"Des corrections ont été demandées sur le contrat #{contract.PublicReference}.",
            contract.NegotiationId);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}

public class ValidateContractCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : ContractHandlerBase(db), IRequestHandler<ValidateContractCommand, Result>
{
    public async Task<Result> Handle(ValidateContractCommand request, CancellationToken ct)
    {
        var (contract, error) = await LoadAsync(request.UserId, request.ContractId, ct);
        if (error is not null) return Result.Failure(error);

        if (!contract!.AwaitsValidation) return Result.Failure("Contract.NotAwaitingValidation");

        // La validación pertenece a las partes, y solo a quien no lo redactó.
        if (contract.ValidatorId != request.UserId) return Result.Failure("Contract.NotValidator");

        var now = DateTimeOffset.UtcNow;

        contract.Status      = ContractStatus.Valide;
        contract.ValidatedAt = now;
        // El contrato definitivo lleva su código de verificación: es ahora cuando el
        // documento se puede descargar y enseñar.
        contract.VerificationCode ??= ContractWorkflow.NewVerificationCode();

        var timeline = await OfferWorkflow.OpenTimelineAsync(db, contract.NegotiationId, ct);
        timeline.Add(NegotiationEventType.ContractValidated, request.UserId);
        timeline.Add(NegotiationEventType.SaleVerified, request.UserId, contract.AgreedPrice);

        // ─── Venta verificada ──────────────────────────────────────────────
        // El anuncio pasa a vendido y la negociación se cierra.
        contract.Vehicle.Status = VehicleStatus.Vendu;
        contract.Vehicle.SoldAt = now;

        contract.Negotiation.Status         = NegotiationStatus.Terminee;
        contract.Negotiation.ClosedAt       = now;
        contract.Negotiation.LastActivityAt = now;

        // +1 vente vérifiée para quien vende, con sus puntos de fidelización.
        var seller = await db.UserProfiles.FirstOrDefaultAsync(u => u.Id == contract.SellerId, ct);
        if (seller is not null)
        {
            seller.VerifiedSalesCount++;

            LoyaltyPointsService.Add(db, seller,
                await LoyaltyPointsService.PointsPerSaleAsync(db, ct),
                LoyaltyPointOrigin.VenteVerifiee,
                contract.Id, contract.PublicReference);
        }

        var authorNotice = ContractWorkflow.Notify(db, contract.CreatedByUserId,
            "Contrat validé",
            $"Le contrat #{contract.PublicReference} a été validé. Vente vérifiée.",
            contract.NegotiationId);

        var validatorNotice = ContractWorkflow.Notify(db, request.UserId,
            "Vente vérifiée",
            $"Le contrat #{contract.PublicReference} est validé.",
            contract.NegotiationId);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([authorNotice, validatorNotice], ct);

        return Result.Success();
    }
}

public class CancelContractCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : ContractHandlerBase(db), IRequestHandler<CancelContractCommand, Result>
{
    public async Task<Result> Handle(CancelContractCommand request, CancellationToken ct)
    {
        var (contract, error) = await LoadAsync(request.UserId, request.ContractId, ct);
        if (error is not null) return Result.Failure(error);

        // Un contrato validado es definitivo: no se anula desde la aplicación.
        if (contract!.Status == ContractStatus.Valide)
            return Result.Failure("Contract.AlreadyValidated");

        if (contract.Status == ContractStatus.Annule) return Result.Success();

        contract.Status      = ContractStatus.Annule;
        contract.CancelledAt = DateTimeOffset.UtcNow;

        contract.Negotiation.Status         = NegotiationStatus.EnCours;
        contract.Negotiation.LastActivityAt = DateTimeOffset.UtcNow;

        var other = contract.CreatedByUserId == request.UserId
            ? contract.ValidatorId
            : contract.CreatedByUserId;

        var notification = ContractWorkflow.Notify(db, other,
            "Contrat annulé",
            $"Le contrat #{contract.PublicReference} a été annulé.",
            contract.NegotiationId);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}
