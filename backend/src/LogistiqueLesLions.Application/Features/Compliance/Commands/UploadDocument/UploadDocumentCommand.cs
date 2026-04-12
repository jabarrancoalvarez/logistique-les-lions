using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Compliance.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid ProcessId,
    Guid DocumentId,
    string FileUrl,
    DocumentStatus NewStatus = DocumentStatus.Uploaded
) : IRequest<Result>;
