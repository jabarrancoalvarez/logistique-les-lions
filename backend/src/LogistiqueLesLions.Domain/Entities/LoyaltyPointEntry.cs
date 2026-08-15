using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Un movimiento del saldo de puntos de un usuario.
/// </summary>
/// <remarks>
/// Es un libro de movimientos, no un contador: append-only, como
/// <see cref="AdminAction"/>. La especificación pide poder consultar «saldo, origen,
/// fecha y movimiento», y eso solo se responde si cada suma y cada resta dejaron su
/// fila. ❌ Nunca se modifica ni se borra un movimiento: se compensa con otro.
///
/// El saldo vive además en <see cref="UserProfile.LoyaltyPoints"/> para no sumar el
/// libro entero cada vez que se pinta un listado; ambos se escriben en la misma
/// transacción.
/// </remarks>
public class LoyaltyPointEntry : AuditableEntity
{
    public Guid UserId { get; set; }

    /// <summary>Con signo: positivo suma, negativo resta.</summary>
    public int Points { get; set; }

    public LoyaltyPointOrigin Origin { get; set; }

    /// <summary>
    /// Qué lo originó, cuando hay algo concreto detrás: el contrato de la venta
    /// verificada. <c>null</c> en un ajuste manual.
    /// </summary>
    public Guid? ContractId { get; set; }

    /// <summary>
    /// Referencia pública de lo que lo originó (<c>YC00125</c>), copiada aquí para que
    /// el movimiento siga siendo legible aunque el contrato cambie de estado.
    /// </summary>
    public string? ContractReference { get; set; }

    /// <summary>Administrador que lo registró, en los ajustes manuales.</summary>
    public Guid? AdminId { get; set; }

    /// <summary>Motivo escrito. Obligatorio en los ajustes manuales.</summary>
    public string? Note { get; set; }

    public UserProfile? User { get; set; }
    public UserProfile? Admin { get; set; }
}
