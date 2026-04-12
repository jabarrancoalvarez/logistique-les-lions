using FluentValidation;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

public class GetVehiclesQueryValidator : AbstractValidator<GetVehiclesQuery>
{
    public GetVehiclesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.PriceFrom)
            .LessThanOrEqualTo(x => x.PriceTo)
            .When(x => x.PriceFrom.HasValue && x.PriceTo.HasValue)
            .WithMessage("PriceFrom no puede ser mayor que PriceTo.");
        RuleFor(x => x.YearFrom)
            .LessThanOrEqualTo(x => x.YearTo)
            .When(x => x.YearFrom.HasValue && x.YearTo.HasValue)
            .WithMessage("YearFrom no puede ser mayor que YearTo.");
    }
}
