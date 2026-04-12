using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Messaging.Queries.GetConversations;

public record GetConversationsQuery(Guid UserId) : IRequest<Result<List<ConversationSummaryDto>>>;
