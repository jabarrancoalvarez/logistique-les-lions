using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetDocumentTemplates;

public class GetDocumentTemplatesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetDocumentTemplatesQuery, Result<IEnumerable<DocumentTemplateDto>>>
{
    public async Task<Result<IEnumerable<DocumentTemplateDto>>> Handle(
        GetDocumentTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = context.DocumentTemplates
            .AsNoTracking()
            .Where(t => t.Country == request.Country);

        if (!string.IsNullOrWhiteSpace(request.DocumentType))
            query = query.Where(t => t.DocumentType.ToString() == request.DocumentType);

        var templates = await query
            .OrderBy(t => t.DocumentType)
            .Select(t => new DocumentTemplateDto(
                t.Id, t.Country, t.DocumentType,
                t.TemplateUrl, t.InstructionsEs, t.InstructionsEn,
                t.OfficialUrl, t.IssuingAuthority,
                t.EstimatedCostEur, t.EstimatedDays))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<DocumentTemplateDto>>.Success(templates);
    }
}
