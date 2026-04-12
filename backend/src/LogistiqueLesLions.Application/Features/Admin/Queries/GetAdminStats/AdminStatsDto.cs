namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetAdminStats;

public record AdminStatsDto(
    int TotalVehicles,
    int ActiveListings,
    int TotalUsers,
    int NewUsersThisMonth,
    int ActiveProcesses,
    int CompletedProcesses,
    int TotalConversations,
    decimal TotalListingValue
);
