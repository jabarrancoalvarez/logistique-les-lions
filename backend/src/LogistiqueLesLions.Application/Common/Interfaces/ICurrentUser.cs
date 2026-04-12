namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Provee información del usuario autenticado en el contexto HTTP actual.
/// Implementado en Infrastructure para no contaminar Application con AspNetCore.
/// </summary>
public interface ICurrentUser
{
    /// <summary>null si la request no está autenticada</summary>
    Guid? UserId { get; }
    string? Email { get; }
    string? Country { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
