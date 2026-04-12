using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.AskVehicleQuestion;

/// <summary>
/// Chat IA contextual: el cliente hace una pregunta sobre un vehículo concreto.
/// El handler carga la ficha + historial y delega al LLM.
/// </summary>
public record AskVehicleQuestionCommand(
    Guid VehicleId,
    string Question,
    IReadOnlyList<ChatTurnDto>? History = null
) : IRequest<Result<ChatAnswerDto>>;

public record ChatTurnDto(string Role, string Content);
public record ChatAnswerDto(string Answer, string Model);

public class AskVehicleQuestionCommandHandler(
    IApplicationDbContext db,
    IAiContentService ai)
    : IRequestHandler<AskVehicleQuestionCommand, Result<ChatAnswerDto>>
{
    public async Task<Result<ChatAnswerDto>> Handle(AskVehicleQuestionCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return Result<ChatAnswerDto>.Failure("Chat.QuestionRequired");
        if (request.Question.Length > 1000)
            return Result<ChatAnswerDto>.Failure("Chat.QuestionTooLong");

        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(v => v.Make)
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (vehicle is null)
            return Result<ChatAnswerDto>.Failure("Vehicle.NotFound");

        var context = new VehicleAiContext(
            Make:          vehicle.Make.Name,
            Model:         vehicle.Model?.Name,
            Year:          vehicle.Year,
            Mileage:       vehicle.Mileage,
            FuelType:      vehicle.FuelType?.ToString(),
            Transmission:  vehicle.Transmission?.ToString(),
            BodyType:      vehicle.BodyType?.ToString(),
            Color:         vehicle.Color,
            Condition:     vehicle.Condition.ToString(),
            Price:         vehicle.Price,
            Currency:      vehicle.Currency,
            CountryOrigin: vehicle.CountryOrigin,
            IsExportReady: vehicle.IsExportReady);

        var history = (request.History ?? [])
            .TakeLast(10)
            .Select(t => new AiChatTurn(t.Role, t.Content))
            .ToList()
            .AsReadOnly();

        var reply = await ai.AnswerVehicleQuestionAsync(context, history, request.Question.Trim(), ct);

        return Result<ChatAnswerDto>.Success(new ChatAnswerDto(reply.Answer, "claude"));
    }
}
