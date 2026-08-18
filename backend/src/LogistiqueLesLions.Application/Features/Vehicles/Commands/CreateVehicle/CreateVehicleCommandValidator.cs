using FluentValidation;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Le titre est obligatoire.")
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Version).MaximumLength(100);

        RuleFor(x => x.MakeId)
            .NotEmpty().WithMessage("La marque est obligatoire.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .WithMessage($"L'année doit être comprise entre 1900 et {DateTime.UtcNow.Year + 1}.");

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).When(x => x.Mileage.HasValue)
            .WithMessage("Le kilométrage ne peut pas être négatif.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Le prix doit être supérieur à 0.");

        RuleFor(x => x.SellerId)
            .NotEmpty();

        RuleFor(x => x.Vin)
            .Length(17).When(x => !string.IsNullOrEmpty(x.Vin))
            .WithMessage("Le VIN doit contenir exactement 17 caractères.");

        RuleFor(x => x.PowerCv)
            .InclusiveBetween(1, 2000).When(x => x.PowerCv.HasValue);

        RuleFor(x => x.EngineDisplacementCc)
            .InclusiveBetween(1, 10000).When(x => x.EngineDisplacementCc.HasValue);

        RuleFor(x => x.Doors)
            .InclusiveBetween(1, 7).When(x => x.Doors.HasValue);

        RuleFor(x => x.Seats)
            .InclusiveBetween(1, 20).When(x => x.Seats.HasValue);

        RuleFor(x => x.Region).MaximumLength(10);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.District).MaximumLength(100);
    }
}
