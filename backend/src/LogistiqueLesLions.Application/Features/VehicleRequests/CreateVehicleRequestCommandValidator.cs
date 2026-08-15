using FluentValidation;

namespace LogistiqueLesLions.Application.Features.VehicleRequests;

public class CreateVehicleRequestCommandValidator : AbstractValidator<CreateVehicleRequestCommand>
{
    public CreateVehicleRequestCommandValidator()
    {
        RuleFor(x => x.MakeName)
            .NotEmpty().WithMessage("La marque est obligatoire.")
            .MaximumLength(100);

        RuleFor(x => x.ModelName).MaximumLength(100);
        RuleFor(x => x.Version).MaximumLength(100);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.ImportantEquipment).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.YearFrom)
            .InclusiveBetween(1950, DateTime.UtcNow.Year + 1).When(x => x.YearFrom.HasValue);
        RuleFor(x => x.YearTo)
            .InclusiveBetween(1950, DateTime.UtcNow.Year + 1).When(x => x.YearTo.HasValue);

        RuleFor(x => x)
            .Must(x => x.YearFrom <= x.YearTo)
            .When(x => x.YearFrom.HasValue && x.YearTo.HasValue)
            .WithMessage("L'année minimale ne peut pas être supérieure à l'année maximale.");

        RuleFor(x => x.MaxMileage)
            .GreaterThan(0).When(x => x.MaxMileage.HasValue);

        RuleFor(x => x.MaxBudget)
            .GreaterThan(0).When(x => x.MaxBudget.HasValue)
            .WithMessage("Le budget doit être supérieur à 0.");

        RuleFor(x => x.Origin).IsInEnum();
    }
}
