using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.VehicleRequests;

public class CreateVehicleRequestCommandHandler(
    IApplicationDbContext context,
    IPublicReferenceGenerator references)
    : IRequestHandler<CreateVehicleRequestCommand, Result<VehicleRequestCreatedDto>>
{
    /// <summary>Solicitudes abiertas simultáneas por usuario.</summary>
    private const int MaxOpenPerUser = 10;

    public async Task<Result<VehicleRequestCreatedDto>> Handle(
        CreateVehicleRequestCommand request, CancellationToken ct)
    {
        var openCount = await context.VehicleRequests
            .CountAsync(r => r.UserId == request.UserId
                          && r.Status != VehicleRequestStatus.Terminee
                          && r.Status != VehicleRequestStatus.Annulee, ct);

        if (openCount >= MaxOpenPerUser)
            return Result<VehicleRequestCreatedDto>.Failure("VehicleRequest.TooManyOpen");

        var reference = await references.NextRequestReferenceAsync(ct);

        var entity = new VehicleRequest
        {
            PublicReference    = reference,
            UserId             = request.UserId,
            MakeId             = request.MakeId,
            MakeName           = request.MakeName.Trim(),
            ModelName          = Clean(request.ModelName),
            Version            = Clean(request.Version),
            YearFrom           = request.YearFrom,
            YearTo             = request.YearTo,
            MaxMileage         = request.MaxMileage,
            FuelType           = request.FuelType,
            Transmission       = request.Transmission,
            BodyType           = request.BodyType,
            Color              = Clean(request.Color),
            ImportantEquipment = Clean(request.ImportantEquipment),
            MaxBudget          = request.MaxBudget,
            Origin             = request.Origin,
            Notes              = Clean(request.Notes),
            Status             = VehicleRequestStatus.NouvelleDemande
        };

        context.VehicleRequests.Add(entity);

        // La solicitud debe llegar al panel de administración, no por correo.
        await NotifyAdminsAsync(context, entity, ct);

        await context.SaveChangesAsync(ct);

        return Result<VehicleRequestCreatedDto>.Success(
            new VehicleRequestCreatedDto(entity.Id, entity.PublicReference));
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task NotifyAdminsAsync(
        IApplicationDbContext context, VehicleRequest request, CancellationToken ct)
    {
        var adminIds = await context.UserProfiles
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var vehicle = string.Join(' ', new[] { request.MakeName, request.ModelName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        foreach (var adminId in adminIds)
        {
            context.UserNotifications.Add(new UserNotification
            {
                UserId   = adminId,
                Category = NotificationCategories.Admin,
                Title    = $"Nouvelle demande #{request.PublicReference}",
                Body     = vehicle,
                Link     = $"/admin/demandes/{request.Id}"
            });
        }
    }
}

public class AddVehicleRequestMessageCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddVehicleRequestMessageCommand, Result>
{
    public async Task<Result> Handle(AddVehicleRequestMessageCommand request, CancellationToken ct)
    {
        var body = request.Body.Trim();
        if (string.IsNullOrEmpty(body))
            return Result.Failure("VehicleRequest.EmptyMessage");

        // El filtro por UserId es la comprobación de propiedad.
        var exists = await context.VehicleRequests
            .AnyAsync(r => r.Id == request.RequestId && r.UserId == request.UserId, ct);

        if (!exists) return Result.Failure("VehicleRequest.NotFound");

        context.VehicleRequestMessages.Add(new VehicleRequestMessage
        {
            RequestId      = request.RequestId,
            SenderId       = request.UserId,
            IsFromAdmin    = false,
            IsInternalNote = false,
            Body           = body
        });

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class CancelVehicleRequestCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CancelVehicleRequestCommand, Result>
{
    public async Task<Result> Handle(CancelVehicleRequestCommand request, CancellationToken ct)
    {
        var entity = await context.VehicleRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.UserId == request.UserId, ct);

        if (entity is null) return Result.Failure("VehicleRequest.NotFound");

        if (!entity.CanBeCancelled)
            return Result.Failure("VehicleRequest.AlreadyClosed");

        // Permanece en el histórico: se cambia el estado, nunca se borra.
        entity.Status   = VehicleRequestStatus.Annulee;
        entity.ClosedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
