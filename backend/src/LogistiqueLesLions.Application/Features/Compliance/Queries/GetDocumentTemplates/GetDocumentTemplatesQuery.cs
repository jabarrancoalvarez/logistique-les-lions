using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetDocumentTemplates;

public record GetDocumentTemplatesQuery(
    string Country,
    string? DocumentType = null
) : IRequest<Result<IEnumerable<DocumentTemplateDto>>>;
