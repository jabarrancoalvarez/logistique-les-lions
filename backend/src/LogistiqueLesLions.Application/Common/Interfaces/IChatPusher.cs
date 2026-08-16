namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>Un mensaje recién guardado, listo para avisar a quien lo recibe.</summary>
/// <param name="NegotiationId">
/// Hilo al que pertenece. Va en el aviso para que la pantalla abierta sepa si el mensaje
/// es de la conversación que se está mirando o de otra.
/// </param>
public record PushedChatMessage(
    Guid MessageId,
    Guid NegotiationId,
    Guid SenderId,
    Guid RecipientId,
    Guid VehicleId,
    string Body,
    DateTimeOffset CreatedAt);

/// <summary>
/// Empuja a la otra parte un mensaje ya persistido.
/// </summary>
/// <remarks>
/// Existe por el mismo motivo que <see cref="INotificationPusher"/>: el envío en vivo se
/// hace <b>después</b> de guardar y puede fallar sin arrastrar la operación de negocio.
///
/// ⚠️ Antes solo empujaba el propio hub, dentro de su método <c>SendMessage</c>. Como la
/// pantalla de la negociación envía por REST, el destinatario no se enteraba de nada
/// hasta recargar. El aviso tiene que salir de donde se guarda el mensaje, no del
/// transporte por el que llegó la petición.
/// </remarks>
public interface IChatPusher
{
    Task PushMessageAsync(PushedChatMessage message, CancellationToken ct = default);
}
