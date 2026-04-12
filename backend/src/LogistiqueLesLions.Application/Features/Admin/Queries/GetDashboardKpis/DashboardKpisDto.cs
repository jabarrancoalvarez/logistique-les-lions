namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetDashboardKpis;

/// <summary>
/// Datos para alimentar dashboards/charts de admin.
/// Cada bucket es serializable directamente como input de Chart.js / ngx-charts.
/// </summary>
public record DashboardKpisDto(
    IReadOnlyList<StatusBucket> ProcessesByStatus,
    IReadOnlyList<StatusBucket> VehiclesByStatus,
    IReadOnlyList<MonthBucket>  ProcessesPerMonth,
    double                       AverageLeadTimeDays,
    int                          OpenIncidents,
    int                          CompletedThisMonth);

public record StatusBucket(string Status, int Count);

public record MonthBucket(string Month, int Count);
