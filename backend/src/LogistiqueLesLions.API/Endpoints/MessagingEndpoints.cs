using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Messaging.Commands.SendMessage;
using LogistiqueLesLions.Application.Features.Messaging.Queries.GetConversations;
using LogistiqueLesLions.Application.Features.Messaging.Queries.GetMessages;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

public static class MessagingEndpoints
{
    public static RouteGroupBuilder MapMessagingEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        // GET /api/v1/messaging/conversations
        group.MapGet("/conversations", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (currentUser.UserId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetConversationsQuery(currentUser.UserId.Value), ct);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithSummary("Listar conversaciones del usuario autenticado");

        // GET /api/v1/messaging/conversations/{id}/messages
        group.MapGet("/conversations/{id:guid}/messages", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            if (currentUser.UserId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetMessagesQuery(id, currentUser.UserId.Value, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithSummary("Obtener mensajes de una conversación");

        // POST /api/v1/messaging/send
        group.MapPost("/send", async (
            [FromBody] SendMessageRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (currentUser.UserId is null) return Results.Unauthorized();
            var command = new SendMessageCommand(currentUser.UserId.Value, req.RecipientId, req.VehicleId, req.Body);
            var result  = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/messaging/messages/{result.Value}", result) : Results.BadRequest(result);
        })
        .WithSummary("Enviar un mensaje");

        return group;
    }
}

public record SendMessageRequest(Guid RecipientId, Guid VehicleId, string Body);
