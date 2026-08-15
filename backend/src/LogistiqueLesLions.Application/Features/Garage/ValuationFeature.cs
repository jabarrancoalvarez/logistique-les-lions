using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

/// <summary>
/// «Valeur estimée» y «Évolution de la valeur» de un vehículo de Mon Garage.
/// </summary>
public record GetVehicleValuationQuery(Guid UserId, Guid GarageVehicleId)
    : IRequest<Result<VehicleValuationDto>>;

/// <param name="HasEstimate">
/// <c>false</c> → la interfaz muestra «Pas assez de données pour estimer la valeur de ce
/// véhicule» y ninguna cifra.
/// </param>
/// <param name="Criteria">En qué se ha basado la muestra, para poder explicarlo.</param>
public record VehicleValuationDto(
    bool HasEstimate,
    decimal? EstimatedValue,
    decimal? LowValue,
    decimal? HighValue,
    int ComparableCount,
    ValuationCriteria Criteria,
    ValuationEvolutionDto? Evolution
);

/// <param name="ChangeAmount">Diferencia respecto a la estimación más antigua del periodo.</param>
/// <param name="ChangePercent">La misma diferencia en porcentaje.</param>
public record ValuationEvolutionDto(
    IReadOnlyList<ValuationPointDto> Points,
    int MonthsCovered,
    decimal? ChangeAmount,
    decimal? ChangePercent
);

public record ValuationPointDto(DateTimeOffset CapturedAt, decimal EstimatedValue, int? Mileage);

public class GetVehicleValuationQueryHandler(
    IApplicationDbContext db,
    IVehicleValuationService valuation)
    : IRequestHandler<GetVehicleValuationQuery, Result<VehicleValuationDto>>
{
    /// <summary>Ventana de la evolución: «6 derniers mois».</summary>
    private const int EvolutionMonths = 6;

    public async Task<Result<VehicleValuationDto>> Handle(
        GetVehicleValuationQuery request, CancellationToken ct)
    {
        var vehicle = await db.GarageVehicles
            .FirstOrDefaultAsync(v => v.Id == request.GarageVehicleId, ct);

        if (vehicle is null) return Result<VehicleValuationDto>.Failure("GarageVehicle.NotFound");

        if (vehicle.UserId != request.UserId)
            return Result<VehicleValuationDto>.Failure("GarageVehicle.AccessDenied");

        var estimate = await valuation.EstimateAsync(vehicle.Id, ct);

        if (estimate.HasEstimate)
            await CaptureSnapshotIfDueAsync(vehicle, estimate, ct);

        var evolution = await BuildEvolutionAsync(vehicle.Id, ct);

        var dto = new VehicleValuationDto(
            estimate.HasEstimate,
            estimate.EstimatedValue, estimate.LowValue, estimate.HighValue,
            estimate.ComparableCount, estimate.Criteria,
            evolution);

        return Result<VehicleValuationDto>.Success(dto);
    }

    /// <summary>
    /// Guarda la estimación de hoy si ha pasado el intervalo desde la última.
    /// </summary>
    /// <remarks>
    /// La evolución se construye sola a base de consultas: no hace falta un proceso que
    /// recorra todos los vehículos, y solo se guarda historial de los que alguien mira.
    /// </remarks>
    private async Task CaptureSnapshotIfDueAsync(
        GarageVehicle vehicle, VehicleValuationResult estimate, CancellationToken ct)
    {
        var settings = await db.VehicleValuationSettings.AsNoTracking().FirstOrDefaultAsync(ct)
                    ?? new VehicleValuationSettings();

        var last = await db.VehicleValuationSnapshots
            .AsNoTracking()
            .Where(s => s.GarageVehicleId == vehicle.Id)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(ct);

        var now = DateTimeOffset.UtcNow;

        if (last is not null && (now - last.CapturedAt).TotalDays < settings.SnapshotIntervalDays)
            return;

        db.VehicleValuationSnapshots.Add(new VehicleValuationSnapshot
        {
            GarageVehicleId = vehicle.Id,
            EstimatedValue  = estimate.EstimatedValue!.Value,
            LowValue        = estimate.LowValue!.Value,
            HighValue       = estimate.HighValue!.Value,
            ComparableCount = estimate.ComparableCount,
            Mileage         = vehicle.Mileage,
            CapturedAt      = now
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Evolución de los últimos meses. <c>null</c> con una sola instantánea: un punto no
    /// es una evolución.
    /// </summary>
    private async Task<ValuationEvolutionDto?> BuildEvolutionAsync(
        Guid garageVehicleId, CancellationToken ct)
    {
        var since = DateTimeOffset.UtcNow.AddMonths(-EvolutionMonths);

        var snapshots = await db.VehicleValuationSnapshots
            .AsNoTracking()
            .Where(s => s.GarageVehicleId == garageVehicleId && s.CapturedAt >= since)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(ct);

        if (snapshots.Count < 2) return null;

        var first = snapshots[0];
        var last = snapshots[^1];

        var change = last.EstimatedValue - first.EstimatedValue;
        var percent = first.EstimatedValue == 0
            ? (decimal?)null
            : Math.Round(change / first.EstimatedValue * 100m, 1);

        var points = snapshots
            .Select(s => new ValuationPointDto(s.CapturedAt, s.EstimatedValue, s.Mileage))
            .ToList();

        return new ValuationEvolutionDto(points, EvolutionMonths, change, percent);
    }
}
