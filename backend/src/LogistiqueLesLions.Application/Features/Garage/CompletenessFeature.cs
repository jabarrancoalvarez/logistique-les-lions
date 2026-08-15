using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

/// <summary>
/// «Complétude du dossier» de un vehículo de Mon Garage.
/// </summary>
/// <remarks>
/// ⚠️ <b>Regla fundamental de la especificación</b>: esto <b>no es un diagnóstico
/// mecánico ni una certificación del estado del vehículo</b>. Yoon u Auto no tiene
/// información para afirmar nada sobre la mecánica. Es únicamente un indicador de lo
/// completo y actualizado que está el <i>historial digital</i> del coche.
///
/// Se calcula con reglas, sin IA.
/// </remarks>
public record GetVehicleCompletenessQuery(Guid UserId, Guid GarageVehicleId)
    : IRequest<Result<CompletenessDto>>;

/// <summary>Apartados que suman puntuación. El frontend les pone nombre en francés.</summary>
public enum CompletenessCheck
{
    /// <summary>Informations principales.</summary>
    MainInformation = 1,
    /// <summary>Kilométrage actualisé.</summary>
    MileageUpToDate = 2,
    /// <summary>VIN enregistré.</summary>
    Vin = 3,
    /// <summary>Photographies.</summary>
    Photos = 4,
    /// <summary>Documents.</summary>
    Documents = 5,
    /// <summary>Historique d'entretien.</summary>
    MaintenanceHistory = 6,
    /// <summary>Rappels à jour.</summary>
    Reminders = 7,
    /// <summary>Factures liées aux entretiens.</summary>
    MaintenanceInvoices = 8
}

/// <summary>Cómo se pinta cada apartado: ✓, ⚠ o pendiente.</summary>
public enum CompletenessStatus { Missing = 0, Partial = 1, Complete = 2 }

/// <summary>Etiqueta global del porcentaje.</summary>
public enum CompletenessLevel { AComplete = 0, Correct = 1, TresBien = 2, Excellent = 3 }

/// <param name="Detail">
/// Dato suelto que el frontend intercala en su texto: «4 entretiens enregistrés».
/// </param>
public record CompletenessItemDto(
    CompletenessCheck Check,
    CompletenessStatus Status,
    int Points,
    int MaxPoints,
    int? Detail
);

public record CompletenessDto(
    int Score,
    CompletenessLevel Level,
    IReadOnlyList<CompletenessItemDto> Items
);

public class GetVehicleCompletenessQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVehicleCompletenessQuery, Result<CompletenessDto>>
{
    public async Task<Result<CompletenessDto>> Handle(
        GetVehicleCompletenessQuery request, CancellationToken ct)
    {
        var vehicle = await db.GarageVehicles
            .AsNoTracking()
            .Include(v => v.Images)
            .Include(v => v.Documents)
            .Include(v => v.MaintenanceRecords)
            .Include(v => v.Reminders)
            .FirstOrDefaultAsync(v => v.Id == request.GarageVehicleId, ct);

        if (vehicle is null) return Result<CompletenessDto>.Failure("GarageVehicle.NotFound");

        if (vehicle.UserId != request.UserId)
            return Result<CompletenessDto>.Failure("GarageVehicle.AccessDenied");

        return Result<CompletenessDto>.Success(CompletenessCalculator.For(vehicle));
    }
}

/// <summary>
/// Reglas de la complétude, en un solo sitio: las usan la ficha del vehículo y la tarjeta
/// de Mon Garage.
/// </summary>
/// <remarks>
/// Es público para poder probar cada regla con un vehículo construido a mano: algunos
/// casos —una fotografía de hace dos años— no se pueden montar contra la base de datos,
/// porque el interceptor de auditoría fija las fechas de creación al guardar.
/// </remarks>
public static class CompletenessCalculator
{
    // ─── Reglas de puntuación ───────────────────────────────────────────────
    // Los pesos suman 100. Están aquí, juntos y a la vista, en lugar de repartidos por
    // el cálculo: son la definición del indicador y deben poder leerse de un vistazo.
    private const int MainInformationPoints     = 15;
    private const int MileagePoints             = 15;
    private const int VinPoints                 = 10;
    private const int PhotosPoints              = 10;
    private const int DocumentsPoints           = 15;
    private const int MaintenancePoints         = 15;
    private const int RemindersPoints           = 10;
    private const int MaintenanceInvoicePoints  = 10;

    /// <summary>Meses tras los que el kilometraje deja de considerarse al día.</summary>
    private const int MileageFreshMonths = 6;

    /// <summary>Meses tras los que la fotografía principal se considera antigua.</summary>
    private const int PhotoFreshMonths = 12;

    /// <summary>Intervenciones a partir de las cuales el historial se considera completo.</summary>
    private const int MaintenanceTarget = 3;

    /// <summary>Documentos que no deberían faltar en ningún vehículo.</summary>
    private static readonly GarageDocumentType[] EssentialDocuments =
        [GarageDocumentType.CarteGrise, GarageDocumentType.Assurance];

    public static CompletenessDto For(GarageVehicle vehicle)
    {
        var now = DateTimeOffset.UtcNow;

        var items = new List<CompletenessItemDto>
        {
            MainInformation(vehicle),
            Mileage(vehicle, now),
            Vin(vehicle),
            Photos(vehicle, now),
            Documents(vehicle),
            Maintenance(vehicle),
            Reminders(vehicle),
            MaintenanceInvoices(vehicle)
        };

        var score = items.Sum(i => i.Points);

        return new CompletenessDto(score, Level(score), items);
    }

    private static CompletenessLevel Level(int score) => score switch
    {
        >= 90 => CompletenessLevel.Excellent,
        >= 75 => CompletenessLevel.TresBien,
        >= 50 => CompletenessLevel.Correct,
        _     => CompletenessLevel.AComplete
    };

    /// <summary>Reparte los puntos en proporción a lo conseguido.</summary>
    private static CompletenessItemDto Item(
        CompletenessCheck check, int achieved, int total, int maxPoints, int? detail = null)
    {
        var points = total == 0 ? 0 : (int)Math.Round((decimal)achieved / total * maxPoints);

        var status = achieved == 0 ? CompletenessStatus.Missing
                   : achieved >= total ? CompletenessStatus.Complete
                   : CompletenessStatus.Partial;

        return new CompletenessItemDto(check, status, points, maxPoints, detail);
    }

    // ─── Apartados ──────────────────────────────────────────────────────────

    /// <summary>Los datos con los que se describe el coche a cualquiera.</summary>
    private static CompletenessItemDto MainInformation(GarageVehicle v)
    {
        var fields = new[]
        {
            v.ModelId is not null,
            !string.IsNullOrWhiteSpace(v.Version),
            v.FuelType is not null,
            v.Transmission is not null,
            v.BodyType is not null,
            !string.IsNullOrWhiteSpace(v.Color)
        };

        return Item(CompletenessCheck.MainInformation,
            fields.Count(f => f), fields.Length, MainInformationPoints);
    }

    /// <summary>
    /// El kilometraje cuenta doble: estar puesto y estar reciente.
    /// </summary>
    /// <remarks>
    /// Un cuentakilómetros declarado hace dos años no dice nada del coche de hoy, y es lo
    /// que sostiene los rappels por kilómetros.
    /// </remarks>
    private static CompletenessItemDto Mileage(GarageVehicle v, DateTimeOffset now)
    {
        if (v.Mileage is null) return Item(CompletenessCheck.MileageUpToDate, 0, 2, MileagePoints);

        var declaredAt = v.MileageUpdatedAt ?? v.CreatedAt;
        var fresh = declaredAt >= now.AddMonths(-MileageFreshMonths);

        return Item(CompletenessCheck.MileageUpToDate, fresh ? 2 : 1, 2, MileagePoints);
    }

    private static CompletenessItemDto Vin(GarageVehicle v) =>
        Item(CompletenessCheck.Vin, string.IsNullOrWhiteSpace(v.Vin) ? 0 : 1, 1, VinPoints);

    private static CompletenessItemDto Photos(GarageVehicle v, DateTimeOffset now)
    {
        var primary = v.Images.FirstOrDefault(i => i.IsPrimary) ?? v.Images.FirstOrDefault();
        if (primary is null) return Item(CompletenessCheck.Photos, 0, 2, PhotosPoints);

        var fresh = primary.CreatedAt >= now.AddMonths(-PhotoFreshMonths);

        return Item(CompletenessCheck.Photos, fresh ? 2 : 1, 2, PhotosPoints, v.Images.Count);
    }

    private static CompletenessItemDto Documents(GarageVehicle v)
    {
        var present = EssentialDocuments.Count(t => v.Documents.Any(d => d.Type == t));

        return Item(CompletenessCheck.Documents,
            present, EssentialDocuments.Length, DocumentsPoints, v.Documents.Count);
    }

    private static CompletenessItemDto Maintenance(GarageVehicle v)
    {
        var count = v.MaintenanceRecords.Count;

        return Item(CompletenessCheck.MaintenanceHistory,
            Math.Min(count, MaintenanceTarget), MaintenanceTarget, MaintenancePoints, count);
    }

    /// <summary>
    /// Rappels atendidos: penaliza los vencidos, no el hecho de tener rappels.
    /// </summary>
    /// <remarks>
    /// Sin ninguno se dan los puntos: no haber programado avisos no es descuidar el
    /// vehículo. Lo que resta es tenerlos vencidos sin hacer nada.
    /// </remarks>
    private static CompletenessItemDto Reminders(GarageVehicle v)
    {
        var overdue = v.Reminders.Count(r => r.Status == ReminderStatus.AFaire);

        return overdue == 0
            ? new CompletenessItemDto(
                CompletenessCheck.Reminders, CompletenessStatus.Complete,
                RemindersPoints, RemindersPoints, 0)
            : new CompletenessItemDto(
                CompletenessCheck.Reminders, CompletenessStatus.Missing,
                0, RemindersPoints, overdue);
    }

    /// <summary>
    /// Proporción de intervenciones con factura enlazada.
    /// </summary>
    /// <remarks>
    /// Sin historial no se penaliza aquí: ya lo hace el apartado del historial, y
    /// descontar dos veces por lo mismo daría un porcentaje injustamente bajo.
    /// </remarks>
    private static CompletenessItemDto MaintenanceInvoices(GarageVehicle v)
    {
        var total = v.MaintenanceRecords.Count;
        if (total == 0)
            return new CompletenessItemDto(
                CompletenessCheck.MaintenanceInvoices, CompletenessStatus.Complete,
                MaintenanceInvoicePoints, MaintenanceInvoicePoints, 0);

        var withInvoice = v.MaintenanceRecords.Count(r => r.DocumentId is not null);

        return Item(CompletenessCheck.MaintenanceInvoices,
            withInvoice, total, MaintenanceInvoicePoints, withInvoice);
    }
}
