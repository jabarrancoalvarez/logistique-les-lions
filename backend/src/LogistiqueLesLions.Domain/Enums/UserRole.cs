namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Roles del sistema. La especificación funcional define únicamente tres actores:
/// Visitante (no autenticado, sin fila en base de datos), Usuario y Administrador.
/// </summary>
/// <remarks>
/// No existen roles Buyer/Seller/Dealer/Moderator: cualquier usuario autenticado puede
/// comprar, vender, negociar y usar Mon Garage, todo gratuito y sin límites.
/// </remarks>
public enum UserRole
{
    /// <summary>Cuenta general autenticada.</summary>
    User = 0,
    /// <summary>Administrador de la plataforma (backoffice).</summary>
    Admin = 1
}
