namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetVehiclesAdmin;

public record VehicleAdminDto(
    Guid Id,
    string Title,
    string Slug,
    string Status,
    decimal Price,
    string Currency,
    string SellerEmail,
    string MakeName,
    int Year,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt
);
