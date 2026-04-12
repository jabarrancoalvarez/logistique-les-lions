namespace LogistiqueLesLions.Domain.Enums;

/// <summary>Tipos de servicios partner del marketplace.</summary>
public enum PartnerType
{
    Gestor      = 0, // Gestoría administrativa / aduanas
    Transport   = 1, // Transporte transfronterizo
    Inspector   = 2, // Inspección técnica pre-compra
    Homologator = 3, // Homologación / ITV
    Insurance   = 4, // Seguros internacionales
    Financing   = 5  // Financiación
}
