using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Services;

/// <summary>
/// Movimientos del saldo de puntos.
/// </summary>
/// <remarks>
/// Existe para que nadie escriba <c>user.LoyaltyPoints += 100</c> suelto por ahí: el
/// saldo y su fila en el libro se escriben siempre juntos. Los métodos <b>no</b> llaman
/// a <c>SaveChanges</c>: se apuntan a la transacción de negocio que los invoca, para que
/// una venta verificada y sus puntos entren o no entren a la vez.
/// </remarks>
public static class LoyaltyPointsService
{
    /// <summary>Puntos que genera una venta verificada, según la configuración.</summary>
    public static async Task<int> PointsPerSaleAsync(IApplicationDbContext db, CancellationToken ct)
    {
        var settings = await db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return settings?.PointsPerVerifiedSale ?? 100;
    }

    /// <summary>
    /// Añade un movimiento y actualiza el saldo del usuario.
    /// </summary>
    /// <returns>
    /// El movimiento, o <c>null</c> si eran cero puntos: un movimiento de cero no dice
    /// nada y solo ensucia el libro.
    /// </returns>
    public static LoyaltyPointEntry? Add(
        IApplicationDbContext db,
        UserProfile user,
        int points,
        LoyaltyPointOrigin origin,
        Guid? contractId = null,
        string? contractReference = null,
        Guid? adminId = null,
        string? note = null)
    {
        if (points == 0) return null;

        var entry = new LoyaltyPointEntry
        {
            UserId            = user.Id,
            Points            = points,
            Origin            = origin,
            ContractId        = contractId,
            ContractReference = contractReference,
            AdminId           = adminId,
            Note              = note
        };

        db.LoyaltyPointEntries.Add(entry);

        // El saldo puede quedar en negativo tras un ajuste a la baja: es el reflejo
        // fiel del libro, y falsearlo a cero desharía la cuenta.
        user.LoyaltyPoints += points;

        return entry;
    }
}
