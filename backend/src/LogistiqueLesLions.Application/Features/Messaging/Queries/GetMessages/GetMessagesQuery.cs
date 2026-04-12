using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Messaging.Queries.GetMessages;

public record GetMessagesQuery(Guid ConversationId, Guid RequesterId, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<MessageDto>>>;
