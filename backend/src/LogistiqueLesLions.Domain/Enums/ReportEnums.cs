namespace LogistiqueLesLions.Domain.Enums;

/// <summary>Motivos por los que un usuario puede reportar algo.</summary>
public enum ReportReason
{
    /// <summary>Annonce suspecte.</summary>
    AnnonceSuspecte = 1,
    /// <summary>Information fausse.</summary>
    InformationFausse = 2,
    /// <summary>Prix trompeur.</summary>
    PrixTrompeur = 3,
    /// <summary>Photographies incorrectes.</summary>
    PhotosIncorrectes = 4,
    /// <summary>Véhicule inexistant.</summary>
    VehiculeInexistant = 5,
    /// <summary>Tentative de fraude.</summary>
    TentativeDeFraude = 6,
    /// <summary>Comportement inapproprié.</summary>
    ComportementInapproprie = 7,
    /// <summary>Spam.</summary>
    Spam = 8,
    /// <summary>Autre motif.</summary>
    Autre = 99
}

/// <summary>Qué se está reportando.</summary>
public enum ReportTargetType
{
    Listing = 1,
    User = 2,
    Negotiation = 3
}

/// <summary>Estado del reporte.</summary>
public enum ReportStatus
{
    /// <summary>Nouveau — nadie lo ha mirado todavía.</summary>
    Nouveau = 1,
    /// <summary>En examen.</summary>
    EnExamen = 2,
    /// <summary>Résolu.</summary>
    Resolu = 3,
    /// <summary>Rejeté — no procedía.</summary>
    Rejete = 4
}
