using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Marketplace.Queries.GetPartners;

public record GetPartnersQuery(PartnerType? Type = null, string? Country = null)
    : IRequest<Result<IReadOnlyList<PartnerDto>>>;

public record PartnerDto(
    Guid Id, string Type, string Name, string Slug, string? Description,
    IReadOnlyList<string> Countries, string? Website, string? LogoUrl,
    decimal Rating, int ReviewsCount, decimal? BasePriceEur, bool IsVerified);

public class GetPartnersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPartnersQuery, Result<IReadOnlyList<PartnerDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerDto>>> Handle(GetPartnersQuery request, CancellationToken ct)
    {
        var q = db.ServicePartners.AsNoTracking().Where(p => p.IsActive);

        if (request.Type.HasValue)
            q = q.Where(p => p.Type == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var iso = request.Country.Trim().ToUpperInvariant();
            q = q.Where(p => p.CountriesCsv.Contains(iso));
        }

        var rows = await q
            .OrderByDescending(p => p.IsVerified)
            .ThenByDescending(p => p.Rating)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        var items = rows.Select(p => new PartnerDto(
            p.Id, p.Type.ToString(), p.Name, p.Slug, p.Description,
            p.CountriesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            p.Website, p.LogoUrl, p.Rating, p.ReviewsCount, p.BasePriceEur, p.IsVerified
        )).ToList();

        return Result<IReadOnlyList<PartnerDto>>.Success(items);
    }
}
