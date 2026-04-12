using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Commands.UploadDocument;

public class UploadDocumentCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UploadDocumentCommand, Result>
{
    public async Task<Result> Handle(
        UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var process = await context.ImportExportProcesses
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == request.ProcessId, cancellationToken);

        if (process is null)
            return Result.Failure("Proceso no encontrado.");

        var document = process.Documents
            .FirstOrDefault(d => d.Id == request.DocumentId);

        if (document is null)
            return Result.Failure("Documento no encontrado en este proceso.");

        document.FileUrl = request.FileUrl;
        document.Status  = request.NewStatus;

        process.RecalculateCompletion();
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
