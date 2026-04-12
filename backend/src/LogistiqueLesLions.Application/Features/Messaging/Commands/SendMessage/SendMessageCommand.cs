using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;

public record SendMessageCommand(
    Guid SenderId,
    Guid RecipientId,
    Guid VehicleId,
    string Body
) : IRequest<Result<Guid>>;
