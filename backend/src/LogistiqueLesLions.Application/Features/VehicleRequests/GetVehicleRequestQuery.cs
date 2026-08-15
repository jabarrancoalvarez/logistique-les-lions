using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.VehicleRequests;

/// <summary>Detalle de una solicitud, con su hilo y sus propuestas.</summary>
public record GetVehicleRequestQuery(Guid UserId, Guid RequestId) : IRequest<Result<VehicleRequestDetailDto>>;

public record VehicleRequestDetailDto(
    Guid Id,
    string PublicReference,
    VehicleRequestStatus Status,
    bool CanBeCancelled,

    string MakeName,
    string? ModelName,
    string? Version,
    int? YearFrom,
    int? YearTo,
    int? MaxMileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    string? Color,
    string? ImportantEquipment,
    decimal? MaxBudget,
    VehicleRequestOrigin Origin,
    string? Notes,

    DateTimeOffset CreatedAt,
    IReadOnlyList<VehicleRequestMessageDto> Messages,
    IReadOnlyList<VehicleRequestProposalDto> Proposals
);

public record VehicleRequestMessageDto(
    Guid Id,
    bool IsFromAdmin,
    string Body,
    DateTimeOffset CreatedAt
);

/// <param name="VehicleSlug">Presente si la propuesta es un anuncio de Yoon u Auto.</param>
public record VehicleRequestProposalDto(
    Guid Id,
    bool IsInternal,
    string? VehicleSlug,
    string? VehicleTitle,
    decimal? VehiclePrice,
    string? MakeModel,
    int? Year,
    int? Mileage,
    decimal? EstimatedPrice,
    string? CountryOfOrigin,
    IReadOnlyList<string> PhotoUrls,
    string? ExternalUrl,
    string? Comments,
    DateTimeOffset CreatedAt
);

public class GetVehicleRequestQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehicleRequestQuery, Result<VehicleRequestDetailDto>>
{
    public async Task<Result<VehicleRequestDetailDto>> Handle(
        GetVehicleRequestQuery request, CancellationToken ct)
    {
        var r = await context.VehicleRequests
            .AsNoTracking()
            .Include(x => x.Messages)
            .Include(x => x.Proposals).ThenInclude(p => p.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == request.RequestId && x.UserId == request.UserId, ct);

        if (r is null)
            return Result<VehicleRequestDetailDto>.Failure("VehicleRequest.NotFound");

        // ⚠️ Las notas internas del administrador nunca llegan al usuario.
        var messages = r.Messages
            .Where(m => !m.IsInternalNote)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new VehicleRequestMessageDto(m.Id, m.IsFromAdmin, m.Body, m.CreatedAt))
            .ToList();

        var proposals = r.Proposals
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new VehicleRequestProposalDto(
                p.Id,
                p.IsInternal,
                p.Vehicle?.Slug,
                p.Vehicle?.Title,
                p.Vehicle?.Price,
                p.MakeModel,
                p.Year,
                p.Mileage,
                p.EstimatedPrice,
                p.CountryOfOrigin,
                SplitPhotos(p.PhotoUrls),
                p.ExternalUrl,
                p.Comments,
                p.CreatedAt))
            .ToList();

        var dto = new VehicleRequestDetailDto(
            r.Id, r.PublicReference, r.Status, r.CanBeCancelled,
            r.MakeName, r.ModelName, r.Version,
            r.YearFrom, r.YearTo, r.MaxMileage,
            r.FuelType, r.Transmission, r.BodyType, r.Color, r.ImportantEquipment,
            r.MaxBudget, r.Origin, r.Notes,
            r.CreatedAt, messages, proposals);

        return Result<VehicleRequestDetailDto>.Success(dto);
    }

    private static IReadOnlyList<string> SplitPhotos(string? urls) =>
        string.IsNullOrWhiteSpace(urls)
            ? []
            : urls.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>
/// Marca como vistas las propuestas de una solicitud, para que deje de aparecer el
/// aviso «Nous avons trouvé un véhicule pour vous».
/// </summary>
public record MarkProposalsSeenCommand(Guid UserId, Guid RequestId) : IRequest<Result>;

public class MarkProposalsSeenCommandHandler(IApplicationDbContext context)
    : IRequestHandler<MarkProposalsSeenCommand, Result>
{
    public async Task<Result> Handle(MarkProposalsSeenCommand request, CancellationToken ct)
    {
        var proposals = await context.VehicleRequestProposals
            .Where(p => p.RequestId == request.RequestId
                     && !p.IsSeenByUser
                     && p.Request.UserId == request.UserId)
            .ToListAsync(ct);

        if (proposals.Count == 0) return Result.Success();

        foreach (var proposal in proposals) proposal.IsSeenByUser = true;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
