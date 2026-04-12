using FluentValidation;

namespace LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RecipientId).NotEqual(x => x.SenderId)
            .WithMessage("No puedes enviarte un mensaje a ti mismo.");
    }
}
