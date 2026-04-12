using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.PublicTracking.Queries.GetTrackingByCode;

public record GetTrackingByCodeQuery(string Code) : IRequest<Result<PublicTrackingDto>>;
