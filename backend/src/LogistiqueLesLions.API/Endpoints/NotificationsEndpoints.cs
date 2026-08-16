using LogistiqueLesLions.Application.Features.Notifications.Commands.MarkNotificationRead;
using LogistiqueLesLions.Application.Features.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

public static class NotificationsEndpoints
{
    public static RouteGroupBuilder MapNotificationsEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        group.MapGet("/", async (
            // Anulables a propósito: un tipo de valor no anulable y sin valor por
            // defecto es un parámetro *obligatorio* en las minimal APIs, y la campana
            // llama solo con «take». Sin esto, el endpoint responde 400 con el cuerpo
            // vacío antes de llegar aquí.
            [FromQuery] bool? unreadOnly,
            [FromQuery] int? take,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMyNotificationsQuery(unreadOnly ?? false, take ?? 50), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).WithName("GetMyNotifications");

        group.MapPost("/{id:guid}/read", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new MarkNotificationReadCommand(id), ct);
            return result.IsSuccess ? Results.Ok(new { updated = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("MarkNotificationRead");

        group.MapPost("/read-all", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new MarkNotificationReadCommand(null, All: true), ct);
            return result.IsSuccess ? Results.Ok(new { updated = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("MarkAllNotificationsRead");

        return group;
    }
}
