using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Infrastructure.Services;

/// <inheritdoc />
public class PublicReferenceGenerator(ApplicationDbContext db) : IPublicReferenceGenerator
{
    /// <summary>Secuencia creada en la migración del modelo de vehículo.</summary>
    private const string VehicleSequence = "vehicles.vehicle_reference_seq";
    /// <summary>Secuencia creada en la migración de «Trouvez-moi une voiture».</summary>
    private const string RequestSequence = "vehicles.vehicle_request_reference_seq";
    /// <summary>Secuencia creada en la migración de contratos.</summary>
    private const string ContractSequence = "messaging.contract_reference_seq";
    /// <summary>Secuencia creada en la migración de moderación.</summary>
    private const string ReportSequence = "messaging.report_reference_seq";

    public async Task<string> NextVehicleReferenceAsync(CancellationToken ct = default) =>
        $"YU{await NextAsync(VehicleSequence, ct):D5}";

    public async Task<string> NextRequestReferenceAsync(CancellationToken ct = default) =>
        $"YD{await NextAsync(RequestSequence, ct):D5}";

    public async Task<string> NextContractReferenceAsync(CancellationToken ct = default) =>
        $"YC{await NextAsync(ContractSequence, ct):D5}";

    public async Task<string> NextReportReferenceAsync(CancellationToken ct = default) =>
        $"SG{await NextAsync(ReportSequence, ct):D5}";

    private Task<long> NextAsync(string sequence, CancellationToken ct) =>
        db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{sequence}') AS \"Value\"")
            .SingleAsync(ct);
}
