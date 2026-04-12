using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetRequirements;

public class GetRequirementsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetRequirementsQuery, Result<CountryRequirementDto>>
{
    public async Task<Result<CountryRequirementDto>> Handle(
        GetRequirementsQuery request, CancellationToken cancellationToken)
    {
        var req = await context.CountryRequirements
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.OriginCountry == request.OriginCountry &&
                r.DestinationCountry == request.DestinationCountry,
                cancellationToken);

        if (req is null)
            return Result<CountryRequirementDto>.Failure(
                $"No se encontró normativa para {request.OriginCountry} → {request.DestinationCountry}.");

        var dto = new CountryRequirementDto(
            req.OriginCountry,
            req.DestinationCountry,
            req.GetDocumentTypes(),
            req.HomologationRequired,
            req.CustomsRatePercent,
            req.VatRatePercent,
            req.TechnicalInspectionRequired,
            req.EstimatedProcessingCostEur,
            req.EstimatedDays,
            req.NotesEs,
            req.NotesEn,
            req.LastUpdatedAt
        );

        return Result<CountryRequirementDto>.Success(dto);
    }
}
