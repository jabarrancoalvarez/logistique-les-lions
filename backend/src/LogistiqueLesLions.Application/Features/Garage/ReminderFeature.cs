using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

// ─── Comandos ──────────────────────────────────────────────────────────────

/// <summary>
/// Datos de un recordatorio. Hace falta al menos una de las dos condiciones.
/// </summary>
public record ReminderInput(
    ReminderType Type,
    string Label,
    DateTimeOffset? DueDate,
    int? DueMileage,
    string? Notes
);

public record AddReminderCommand(Guid UserId, Guid GarageVehicleId, ReminderInput Reminder)
    : IRequest<Result<Guid>>;

public record UpdateReminderCommand(Guid UserId, Guid ReminderId, ReminderInput Reminder)
    : IRequest<Result>;

/// <summary>«Terminé» o «Annulé»: los dos estados que decide el usuario.</summary>
public record SetReminderStatusCommand(Guid UserId, Guid ReminderId, ReminderStatus Status)
    : IRequest<Result>;

public record DeleteReminderCommand(Guid UserId, Guid ReminderId) : IRequest<Result>;

// ─── Consultas ─────────────────────────────────────────────────────────────

/// <summary>Recordatorios de un vehículo.</summary>
public record GetVehicleRemindersQuery(Guid UserId, Guid GarageVehicleId)
    : IRequest<Result<IReadOnlyList<ReminderDto>>>;

/// <param name="MileageRemaining">
/// Kilómetros que faltan según la última lectura declarada. Negativo si ya se ha pasado.
/// </param>
public record ReminderDto(
    Guid Id,
    Guid GarageVehicleId,
    ReminderType Type,
    string Label,
    DateTimeOffset? DueDate,
    int? DueMileage,
    ReminderStatus Status,
    int? DaysRemaining,
    int? MileageRemaining,
    string? Notes,
    DateTimeOffset? CompletedAt
);

/// <summary>
/// «1 rappel à venir» del resumen de Mon Garage: los recordatorios abiertos de todos los
/// vehículos del usuario, del más urgente al menos.
/// </summary>
public record GetUpcomingRemindersQuery(Guid UserId, int Limit = 5)
    : IRequest<Result<IReadOnlyList<UpcomingReminderDto>>>;

public record UpcomingReminderDto(
    Guid Id,
    Guid GarageVehicleId,
    string VehicleTitle,
    ReminderType Type,
    string Label,
    DateTimeOffset? DueDate,
    int? DueMileage,
    ReminderStatus Status,
    int? DaysRemaining,
    int? MileageRemaining
);

// ─── Handlers ──────────────────────────────────────────────────────────────

internal static class ReminderWorkflow
{
    public static string? Validate(ReminderInput r)
    {
        if (string.IsNullOrWhiteSpace(r.Label)) return "Reminder.LabelRequired";

        // Sin fecha ni kilometraje no hay nada que vigilar.
        if (r.DueDate is null && r.DueMileage is null) return "Reminder.ConditionRequired";

        if (r.DueMileage is <= 0) return "Reminder.InvalidMileage";

        return null;
    }

    public static void Apply(VehicleReminder entity, ReminderInput r)
    {
        entity.Type       = r.Type;
        entity.Label      = r.Label.Trim();
        entity.DueDate    = r.DueDate;
        entity.DueMileage = r.DueMileage;
        entity.Notes      = GarageWorkflow.Clean(r.Notes);
    }

    public static async Task<(VehicleReminder? reminder, string? error)> LoadAsync(
        IApplicationDbContext db, Guid userId, Guid reminderId, CancellationToken ct)
    {
        var reminder = await db.VehicleReminders
            .Include(r => r.GarageVehicle)
            .FirstOrDefaultAsync(r => r.Id == reminderId, ct);

        if (reminder is null) return (null, "Reminder.NotFound");

        if (reminder.GarageVehicle.UserId != userId)
            return (null, "GarageVehicle.AccessDenied");

        return (reminder, null);
    }

    /// <summary>Días que faltan. <c>null</c> si el recordatorio no depende de una fecha.</summary>
    public static int? DaysRemaining(VehicleReminder r) =>
        r.DueDate is { } date ? (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalDays) : null;

    /// <summary>
    /// Kilómetros que faltan según la <b>última lectura declarada</b> por el usuario.
    /// </summary>
    /// <remarks>
    /// Si el vehículo no tiene kilometraje registrado no se devuelve nada: inventar
    /// cuánto ha rodado está prohibido por la especificación.
    /// </remarks>
    public static int? MileageRemaining(VehicleReminder r, int? currentMileage) =>
        r.DueMileage is { } target && currentMileage is { } current ? target - current : null;
}

public class AddReminderCommandHandler(IApplicationDbContext db, IReminderService reminders)
    : IRequestHandler<AddReminderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddReminderCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await GarageWorkflow.LoadAsync(
            db, request.UserId, request.GarageVehicleId, ct);
        if (error is not null) return Result<Guid>.Failure(error);

        var invalid = ReminderWorkflow.Validate(request.Reminder);
        if (invalid is not null) return Result<Guid>.Failure(invalid);

        var reminder = new VehicleReminder { GarageVehicleId = vehicle!.Id };
        ReminderWorkflow.Apply(reminder, request.Reminder);

        db.VehicleReminders.Add(reminder);
        await db.SaveChangesAsync(ct);

        // Un recordatorio puede nacer ya vencido: «revisión que debía hacerse en mayo».
        await reminders.EvaluateVehicleAsync(vehicle.Id, ct);

        return Result<Guid>.Success(reminder.Id);
    }
}

public class UpdateReminderCommandHandler(IApplicationDbContext db, IReminderService reminders)
    : IRequestHandler<UpdateReminderCommand, Result>
{
    public async Task<Result> Handle(UpdateReminderCommand request, CancellationToken ct)
    {
        var (reminder, error) = await ReminderWorkflow.LoadAsync(db, request.UserId, request.ReminderId, ct);
        if (error is not null) return Result.Failure(error);

        var invalid = ReminderWorkflow.Validate(request.Reminder);
        if (invalid is not null) return Result.Failure(invalid);

        ReminderWorkflow.Apply(reminder!, request.Reminder);

        // Aplazarlo lo devuelve a «À venir»: la condición vuelve a estar por cumplirse.
        if (reminder!.Status == ReminderStatus.AFaire
            && !reminder.IsDue(DateTimeOffset.UtcNow, reminder.GarageVehicle.Mileage))
        {
            reminder.Status = ReminderStatus.AVenir;
        }

        await db.SaveChangesAsync(ct);
        await reminders.EvaluateVehicleAsync(reminder.GarageVehicleId, ct);

        return Result.Success();
    }
}

public class SetReminderStatusCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SetReminderStatusCommand, Result>
{
    public async Task<Result> Handle(SetReminderStatusCommand request, CancellationToken ct)
    {
        var (reminder, error) = await ReminderWorkflow.LoadAsync(db, request.UserId, request.ReminderId, ct);
        if (error is not null) return Result.Failure(error);

        // «À faire» lo decide el sistema al vencer la condición, no el usuario.
        if (request.Status == ReminderStatus.AFaire)
            return Result.Failure("Reminder.StatusNotAllowed");

        reminder!.Status = request.Status;
        reminder.CompletedAt = request.Status == ReminderStatus.Termine
            ? DateTimeOffset.UtcNow
            : null;

        // Reabrirlo permite que vuelva a avisar cuando toque.
        if (request.Status == ReminderStatus.AVenir) reminder.NotifiedAt = null;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class DeleteReminderCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteReminderCommand, Result>
{
    public async Task<Result> Handle(DeleteReminderCommand request, CancellationToken ct)
    {
        var (reminder, error) = await ReminderWorkflow.LoadAsync(db, request.UserId, request.ReminderId, ct);
        if (error is not null) return Result.Failure(error);

        reminder!.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

public class GetVehicleRemindersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVehicleRemindersQuery, Result<IReadOnlyList<ReminderDto>>>
{
    public async Task<Result<IReadOnlyList<ReminderDto>>> Handle(
        GetVehicleRemindersQuery request, CancellationToken ct)
    {
        var vehicle = await db.GarageVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.GarageVehicleId, ct);

        if (vehicle is null)
            return Result<IReadOnlyList<ReminderDto>>.Failure("GarageVehicle.NotFound");

        if (vehicle.UserId != request.UserId)
            return Result<IReadOnlyList<ReminderDto>>.Failure("GarageVehicle.AccessDenied");

        var reminders = await db.VehicleReminders
            .AsNoTracking()
            .Where(r => r.GarageVehicleId == request.GarageVehicleId)
            .ToListAsync(ct);

        // Primero lo que toca ya, luego lo que está por venir, y al final lo cerrado.
        var ordered = reminders
            .OrderBy(r => r.Status switch
            {
                ReminderStatus.AFaire => 0,
                ReminderStatus.AVenir => 1,
                _ => 2
            })
            .ThenBy(r => r.DueDate ?? DateTimeOffset.MaxValue)
            .ThenBy(r => r.DueMileage ?? int.MaxValue)
            .Select(r => new ReminderDto(
                r.Id, r.GarageVehicleId, r.Type, r.Label, r.DueDate, r.DueMileage, r.Status,
                ReminderWorkflow.DaysRemaining(r),
                ReminderWorkflow.MileageRemaining(r, vehicle.Mileage),
                r.Notes, r.CompletedAt))
            .ToList();

        return Result<IReadOnlyList<ReminderDto>>.Success(ordered);
    }
}

public class GetUpcomingRemindersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUpcomingRemindersQuery, Result<IReadOnlyList<UpcomingReminderDto>>>
{
    public async Task<Result<IReadOnlyList<UpcomingReminderDto>>> Handle(
        GetUpcomingRemindersQuery request, CancellationToken ct)
    {
        var open = await db.VehicleReminders
            .AsNoTracking()
            .Include(r => r.GarageVehicle).ThenInclude(v => v.Make)
            .Include(r => r.GarageVehicle).ThenInclude(v => v.Model)
            .Where(r => r.GarageVehicle.UserId == request.UserId
                     && (r.Status == ReminderStatus.AFaire || r.Status == ReminderStatus.AVenir))
            .ToListAsync(ct);

        var ordered = open
            .OrderBy(r => r.Status == ReminderStatus.AFaire ? 0 : 1)
            .ThenBy(r => r.DueDate ?? DateTimeOffset.MaxValue)
            .ThenBy(r => ReminderWorkflow.MileageRemaining(r, r.GarageVehicle.Mileage) ?? int.MaxValue)
            .Take(request.Limit)
            .Select(r => new UpcomingReminderDto(
                r.Id, r.GarageVehicleId,
                GarageTitle.For(r.GarageVehicle.Make.Name, r.GarageVehicle.Model?.Name, r.GarageVehicle.Version),
                r.Type, r.Label, r.DueDate, r.DueMileage, r.Status,
                ReminderWorkflow.DaysRemaining(r),
                ReminderWorkflow.MileageRemaining(r, r.GarageVehicle.Mileage)))
            .ToList();

        return Result<IReadOnlyList<UpcomingReminderDto>>.Success(ordered);
    }
}
