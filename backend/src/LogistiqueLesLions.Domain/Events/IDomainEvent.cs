using MediatR;

namespace LogistiqueLesLions.Domain.Events;

/// <summary>
/// Marcador para eventos de dominio. Se publican vía MediatR tras persistir.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId => Guid.NewGuid();
    DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;
}
