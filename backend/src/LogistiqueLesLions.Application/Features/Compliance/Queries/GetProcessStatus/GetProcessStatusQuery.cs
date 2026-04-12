using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetProcessStatus;

public record GetProcessStatusQuery(Guid ProcessId) : IRequest<Result<ProcessStatusDto>>;
