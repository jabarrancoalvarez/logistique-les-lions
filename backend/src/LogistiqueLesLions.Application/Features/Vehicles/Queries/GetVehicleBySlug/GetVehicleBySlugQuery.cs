using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;

/// <param name="RequesterId">
/// Quién la pide, salido del token. Permite que el dueño abra su propio borrador o su
/// anuncio pausado; para un visitante cualquiera es <c>null</c>.
/// </param>
/// <param name="IsAdmin">El backoffice necesita ver también lo que está oculto.</param>
public record GetVehicleBySlugQuery(
    string Slug,
    Guid? RequesterId = null,
    bool IsAdmin = false) : IRequest<Result<VehicleDetailDto>>;
