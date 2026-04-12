namespace LogistiqueLesLions.Domain.Enums;

public enum ProcessType
{
    /// <summary>Importación entre países de la UE (sin aduana pero con matriculación)</summary>
    IntraEu = 1,
    /// <summary>Importación desde país extra-UE hacia país UE</summary>
    ImportExtraEu = 2,
    /// <summary>Exportación desde país UE hacia país extra-UE</summary>
    ExportExtraEu = 3,
    /// <summary>Importación desde país extra-UE hacia otro país extra-UE</summary>
    ExtraEuToExtraEu = 4
}
