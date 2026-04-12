using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.UploadVehicleImage;

public record UploadVehicleImageCommand(
    Guid VehicleId,
    string Url,
    string? ThumbnailUrl,
    bool IsPrimary,
    int SortOrder,
    string? AltText
) : IRequest<Result<Guid>>;
