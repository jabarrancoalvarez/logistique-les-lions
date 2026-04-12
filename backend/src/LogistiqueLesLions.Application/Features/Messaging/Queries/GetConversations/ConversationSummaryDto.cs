namespace LogistiqueLesLions.Application.Features.Messaging.Queries.GetConversations;

public record ConversationSummaryDto(
    Guid Id,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatar,
    Guid VehicleId,
    string VehicleTitle,
    string? VehicleThumbnail,
    string? LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount
);
