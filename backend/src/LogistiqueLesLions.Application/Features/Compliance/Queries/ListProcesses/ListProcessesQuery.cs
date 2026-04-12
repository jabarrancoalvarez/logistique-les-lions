using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.ListProcesses;

public record ListProcessesQuery : IRequest<Result<IReadOnlyList<ProcessListItemDto>>>;

public record ProcessListItemDto(
    Guid Id,
    Guid VehicleId,
    string VehicleTitle,
    string OriginCountry,
    string DestinationCountry,
    string ProcessType,
    string Status,
    int CompletionPercent,
    decimal EstimatedCostEur,
    DateTimeOffset? StartedAt,
    DateTimeOffset CreatedAt);
