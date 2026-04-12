using FluentValidation;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(200).WithMessage("El título no puede superar 200 caracteres.");

        RuleFor(x => x.MakeId)
            .NotEmpty().WithMessage("La marca es obligatoria.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .WithMessage($"El año debe estar entre 1900 y {DateTime.UtcNow.Year + 1}.");

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).When(x => x.Mileage.HasValue)
            .WithMessage("El kilometraje no puede ser negativo.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor que 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().Length(3).WithMessage("La divisa debe ser un código ISO 4217 de 3 caracteres.");

        RuleFor(x => x.CountryOrigin)
            .NotEmpty().Length(2).WithMessage("El país de origen debe ser un código ISO2 de 2 caracteres.");

        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("El vendedor es obligatorio.");

        RuleFor(x => x.Vin)
            .Length(17).When(x => !string.IsNullOrEmpty(x.Vin))
            .WithMessage("El VIN debe tener exactamente 17 caracteres.");
    }
}
