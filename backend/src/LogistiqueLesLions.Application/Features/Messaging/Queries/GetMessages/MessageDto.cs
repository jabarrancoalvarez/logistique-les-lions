namespace LogistiqueLesLions.Application.Features.Messaging.Queries.GetMessages;

public record MessageDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string? SenderAvatar,
    string Body,
    bool IsRead,
    DateTimeOffset CreatedAt
);
