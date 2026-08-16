using LogistiqueLesLions.Application.Common.Formatting;
using System.Globalization;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Services;

/// <inheritdoc />
public class ReminderService(
    IApplicationDbContext context,
    INotificationPusher? pusher = null) : IReminderService
{
    public async Task<int> EvaluateVehicleAsync(Guid garageVehicleId, CancellationToken ct = default)
    {
        var vehicle = await context.GarageVehicles
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Reminders)
            .FirstOrDefaultAsync(v => v.Id == garageVehicleId, ct);

        if (vehicle is null) return 0;

        var due = vehicle.Reminders
            .Where(r => r.Status == ReminderStatus.AVenir
                     && r.IsDue(DateTimeOffset.UtcNow, vehicle.Mileage))
            .ToList();

        return await MarkAsync(due, _ => vehicle, ct);
    }

    public async Task<int> EvaluateDueByDateAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Solo por fecha: el kilometraje no avanza solo, y comprobarlo aquí no aportaría
        // nada que no se haya comprobado ya al declararlo.
        var due = await context.VehicleReminders
            .Where(r => r.Status == ReminderStatus.AVenir
                     && r.DueDate != null
                     && r.DueDate <= now)
            .ToListAsync(ct);

        if (due.Count == 0) return 0;

        var vehicleIds = due.Select(r => r.GarageVehicleId).Distinct().ToList();
        var vehicles = await context.GarageVehicles
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Where(v => vehicleIds.Contains(v.Id))
            .ToListAsync(ct);

        var byId = vehicles.ToDictionary(v => v.Id);

        return await MarkAsync(
            due.Where(r => byId.ContainsKey(r.GarageVehicleId)).ToList(),
            r => byId[r.GarageVehicleId],
            ct);
    }

    /// <summary>
    /// Marca los recordatorios como «À faire», avisa a quien corresponde y empuja el
    /// aviso <b>después</b> de guardar.
    /// </summary>
    private async Task<int> MarkAsync(
        IReadOnlyList<VehicleReminder> due,
        Func<VehicleReminder, GarageVehicle> vehicleOf,
        CancellationToken ct)
    {
        if (due.Count == 0) return 0;

        var now = DateTimeOffset.UtcNow;
        var created = new List<UserNotification>();

        foreach (var reminder in due)
        {
            reminder.Status = ReminderStatus.AFaire;

            // Un recordatorio solo avisa una vez: si ya se notificó, no se repite aunque
            // el usuario lo devuelva a «À venir» y vuelva a vencer.
            if (reminder.NotifiedAt is not null) continue;

            reminder.NotifiedAt = now;

            var vehicle = vehicleOf(reminder);
            var notification = new UserNotification
            {
                UserId   = vehicle.UserId,
                Category = NotificationCategories.Reminder,
                Title    = reminder.Label,
                Body     = Body(reminder, vehicle),
                Link     = $"/mi-garaje/{vehicle.Id}"
            };

            context.UserNotifications.Add(notification);
            created.Add(notification);
        }

        await context.SaveChangesAsync(ct);
        await pusher.PushAsync(created, ct);

        return due.Count;
    }

    private static string Body(VehicleReminder reminder, GarageVehicle vehicle)
    {
        var name = string.Join(' ', new[] { vehicle.Make.Name, vehicle.Model?.Name }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        // Se dice por qué toca: la fecha, el kilometraje o lo que se haya cumplido antes.
        var reason = reminder.DueMileage is { } mileage && vehicle.Mileage >= mileage
            ? FcfaFormat.Kilometres(mileage)
            : reminder.DueDate?.ToString("dd/MM/yyyy") ?? string.Empty;

        return string.IsNullOrEmpty(reason)
            ? $"Rappel pour votre {name}."
            : $"{name} — échéance : {reason}.";
    }
}
