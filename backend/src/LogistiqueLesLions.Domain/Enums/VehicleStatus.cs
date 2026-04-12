namespace LogistiqueLesLions.Domain.Enums;

public enum VehicleStatus
{
    /// <summary>Pendiente de revisión por moderador</summary>
    Reviewing = 0,
    /// <summary>Activo y visible en búsquedas</summary>
    Active = 1,
    /// <summary>Vendido, no aparece en búsquedas</summary>
    Sold = 2,
    /// <summary>Pausado temporalmente por el vendedor</summary>
    Paused = 3,
    /// <summary>Rechazado por moderación</summary>
    Rejected = 4,
    /// <summary>Expirado (superó días máximos sin renovar)</summary>
    Expired = 5
}
