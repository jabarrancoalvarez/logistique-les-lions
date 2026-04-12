using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Countries.Queries.GetSupportedCountries;

public class GetSupportedCountriesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSupportedCountriesQuery, Result<IEnumerable<SupportedCountryDto>>>
{
    public async Task<Result<IEnumerable<SupportedCountryDto>>> Handle(
        GetSupportedCountriesQuery request,
        CancellationToken cancellationToken)
    {
        var countries = await context.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.NameEs)
            .Select(c => new SupportedCountryDto(
                c.Code,
                c.NameEs,
                c.NameEn,
                c.FlagEmoji,
                c.Currency,
                c.IsEuMember,
                c.SupportsImport,
                c.SupportsExport,
                c.DisplayOrder
            ))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<SupportedCountryDto>>.Success(countries);
    }
}
