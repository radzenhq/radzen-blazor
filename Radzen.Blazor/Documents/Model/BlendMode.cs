namespace Radzen.Documents;


/// <summary>
/// A color blend mode: how the color being painted is combined with the backdrop already
/// on the page.
/// </summary>
public enum BlendMode
{
    /// <summary>Paints the source over the backdrop (the default).</summary>
    Normal,

    /// <summary>Multiplies the backdrop and source colors.</summary>
    Multiply,

    /// <summary>Multiplies the complements, then complements the result.</summary>
    Screen,

    /// <summary>Multiplies or screens depending on the backdrop.</summary>
    Overlay,

    /// <summary>Selects the darker of backdrop and source.</summary>
    Darken,

    /// <summary>Selects the lighter of backdrop and source.</summary>
    Lighten,

    /// <summary>Brightens the backdrop to reflect the source.</summary>
    ColorDodge,

    /// <summary>Darkens the backdrop to reflect the source.</summary>
    ColorBurn,

    /// <summary>Multiplies or screens depending on the source.</summary>
    HardLight,

    /// <summary>Darkens or lightens depending on the source.</summary>
    SoftLight,

    /// <summary>Subtracts the darker of the two colors from the lighter.</summary>
    Difference,

    /// <summary>Like <see cref="Difference"/> but lower in contrast.</summary>
    Exclusion,

    /// <summary>Uses the source hue with the backdrop saturation and luminosity.</summary>
    Hue,

    /// <summary>Uses the source saturation with the backdrop hue and luminosity.</summary>
    Saturation,

    /// <summary>Uses the source hue and saturation with the backdrop luminosity.</summary>
    Color,

    /// <summary>Uses the source luminosity with the backdrop hue and saturation.</summary>
    Luminosity,
}
