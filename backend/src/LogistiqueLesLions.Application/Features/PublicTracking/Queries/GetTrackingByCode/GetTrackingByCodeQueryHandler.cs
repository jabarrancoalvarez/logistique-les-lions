using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.PublicTracking.Queries.GetTrackingByCode;

public class GetTrackingByCodeQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTrackingByCodeQuery, Result<PublicTrackingDto>>
{
    public async Task<Result<PublicTrackingDto>> Handle(
        GetTrackingByCodeQuery request,
        CancellationToken ct)
    {
        // Normalizar input: el usuario puede escribirlo con espacios o minúsculas
        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();

        if (code.Length is < 6 or > 16)
        {
            return Result<PublicTrackingDto>.Failure("Tracking.InvalidCode");
        }

        var process = await db.ImportExportProcesses
            .AsNoTracking()
            .Where(p => p.TrackingCode == code)
            .Select(p => new
            {
                p.TrackingCode,
                p.Status,
                p.CompletionPercent,
                p.OriginCountry,
                p.DestinationCountry,
                p.ProcessType,
                p.StartedAt,
                p.CompletedAt,
                p.UpdatedAt,
                VehicleTitle = p.Vehicle.Title
            })
            .FirstOrDefaultAsync(ct);

        if (process is null)
        {
            // Mensaje genérico — no revelar si el código existe o no
            return Result<PublicTrackingDto>.Failure("Tracking.NotFound");
        }

        return Result<PublicTrackingDto>.Success(new PublicTrackingDto(
            process.TrackingCode,
            process.Status.ToString(),
            process.CompletionPercent,
            process.OriginCountry,
            process.DestinationCountry,
            process.ProcessType.ToString(),
            process.StartedAt,
            process.CompletedAt,
            process.UpdatedAt,
            process.VehicleTitle));
    }
}
