namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Nivel de destacado («mise en avant») de un anuncio. Jerárquico: cada nivel incluye
/// lo del anterior. Gratuito durante la fase de prueba; de pago más adelante.
/// </summary>
public enum FeaturedTier
{
    /// <summary>Anuncio normal, sin destacar.</summary>
    Aucune = 0,

    /// <summary>«En vedette»: encabeza la página de búsqueda, por encima del resto.</summary>
    EnVedette = 1,

    /// <summary>«À la une»: además de encabezar la búsqueda, aparece en la portada.</summary>
    ALaUne = 2
}
