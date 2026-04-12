using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetProcessStatus;

public class GetProcessStatusQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetProcessStatusQuery, Result<ProcessStatusDto>>
{
    public async Task<Result<ProcessStatusDto>> Handle(
        GetProcessStatusQuery request, CancellationToken cancellationToken)
    {
        var process = await context.ImportExportProcesses
            .AsNoTracking()
            .Include(p => p.Vehicle)
            .Include(p => p.Documents.OrderBy(d => d.RequiredByDate))
            .FirstOrDefaultAsync(p => p.Id == request.ProcessId, cancellationToken);

        if (process is null)
            return Result<ProcessStatusDto>.Failure("Proceso no encontrado.");

        var dto = new ProcessStatusDto(
            process.Id,
            process.VehicleId,
            process.Vehicle.Title,
            process.OriginCountry,
            process.DestinationCountry,
            process.ProcessType,
            process.Status,
            process.CompletionPercent,
            process.EstimatedCostEur,
            process.ActualCostEur,
            process.StartedAt,
            process.CompletedAt,
            process.CreatedAt,
            process.Documents.Select(d => new ProcessDocumentDto(
                d.Id,
                d.DocumentType.ToString(),
                d.Status,
                d.ResponsibleParty,
                d.RequiredByDate,
                d.FileUrl,
                d.TemplateUrl,
                d.OfficialUrl,
                d.InstructionsEs,
                d.EstimatedCostEur
            )).ToList().AsReadOnly()
        );

        return Result<ProcessStatusDto>.Success(dto);
    }
}
