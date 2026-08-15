using FluentValidation;
using LogistiqueLesLions.Application.Common;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Le numéro de téléphone est obligatoire.")
            .Must(SenegalPhone.IsValid!)
            .WithMessage("Numéro de téléphone sénégalais invalide (ex. +221 77 123 45 67).");

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Le nom est obligatoire.")
            .MaximumLength(150);

        RuleFor(x => x.AccountType).IsInEnum();

        RuleFor(x => x.Region).MaximumLength(10).When(x => x.Region is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);

        RuleFor(x => x.Email)
            .EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
