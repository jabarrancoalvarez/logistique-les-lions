namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Tipo de cuenta declarado por el usuario durante el registro.
/// </summary>
/// <remarks>
/// Regla de negocio de la especificación funcional (V1): es <b>únicamente un campo
/// informativo del perfil</b>. No genera interfaces, permisos ni funcionalidades
/// distintas. No debe usarse nunca para autorizar ni para limitar el número de anuncios.
/// </remarks>
public enum AccountType
{
    Particulier = 1,
    Professionnel = 2
}
