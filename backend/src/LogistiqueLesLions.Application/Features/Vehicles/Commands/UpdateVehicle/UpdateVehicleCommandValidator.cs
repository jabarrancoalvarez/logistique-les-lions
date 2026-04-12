using FluentValidation;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;

public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.MakeId).NotEmpty();

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1);

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).When(x => x.Mileage.HasValue);

        RuleFor(x => x.Price).GreaterThan(0);

        RuleFor(x => x.Currency).NotEmpty().Length(3);

        RuleFor(x => x.CountryOrigin).NotEmpty().Length(2);

        RuleFor(x => x.Vin)
            .Length(17).When(x => !string.IsNullOrEmpty(x.Vin));
    }
}
