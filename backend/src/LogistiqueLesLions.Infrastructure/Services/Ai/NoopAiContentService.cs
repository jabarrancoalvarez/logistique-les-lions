using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogistiqueLesLions.Infrastructure.Services.Ai;

/// <summary>
/// Fallback usado cuando no hay API key configurada. Devuelve placeholders
/// determinísticos para no romper desarrollo local ni tests.
/// </summary>
public class NoopAiContentService(ILogger<NoopAiContentService> logger) : IAiContentService
{
    public Task<AiVehicleDescription> GenerateVehicleDescriptionAsync(
        VehicleAiContext c, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("AI service no configurado — devolviendo descripción placeholder para {Make} {Model}", c.Make, c.Model);

        var es = $"{c.Make} {c.Model} de {c.Year}. {c.Condition}, {c.Mileage?.ToString() ?? "?"} km. " +
                 $"Origen {c.CountryOrigin}. Precio {c.Price:N0} {c.Currency}.";
        var en = $"{c.Year} {c.Make} {c.Model}. {c.Condition}, {c.Mileage?.ToString() ?? "?"} km. " +
                 $"From {c.CountryOrigin}. Price {c.Price:N0} {c.Currency}.";

        return Task.FromResult(new AiVehicleDescription(es, en));
    }

    public Task<AiDocumentExtraction> ExtractVehicleDocumentAsync(
        byte[] imageBytes, string mediaType, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("AI service no configurado — devolviendo extracción vacía");
        return Task.FromResult(new AiDocumentExtraction(null, null, null, null, null, null, null, null, null));
    }

    public Task<AiChatReply> AnswerVehicleQuestionAsync(
        VehicleAiContext context,
        IReadOnlyList<AiChatTurn> history,
        string question,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning("AI service no configurado — chat contextual deshabilitado");
        var reply = $"Sobre el {context.Make} {context.Model} ({context.Year}): el servicio de IA no está configurado. " +
                    "Contacta con un asesor humano para responder tu pregunta.";
        return Task.FromResult(new AiChatReply(reply));
    }
}
