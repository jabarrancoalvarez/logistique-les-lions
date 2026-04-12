using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.UploadVehicleImage;

public class UploadVehicleImageCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UploadVehicleImageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        UploadVehicleImageCommand request, CancellationToken cancellationToken)
    {
        var vehicleExists = await context.Vehicles
            .AnyAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (!vehicleExists)
            return Result<Guid>.Failure("Vehículo no encontrado.");

        // Si la nueva imagen es primaria, desmarcar las actuales
        if (request.IsPrimary)
        {
            var current = await context.VehicleImages
                .Where(i => i.VehicleId == request.VehicleId && i.IsPrimary)
                .ToListAsync(cancellationToken);
            foreach (var img in current)
                img.IsPrimary = false;
        }

        var image = new VehicleImage
        {
            VehicleId    = request.VehicleId,
            Url          = request.Url,
            ThumbnailUrl = request.ThumbnailUrl,
            IsPrimary    = request.IsPrimary,
            SortOrder    = request.SortOrder,
            AltText      = request.AltText
        };

        context.VehicleImages.Add(image);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(image.Id);
    }
}
