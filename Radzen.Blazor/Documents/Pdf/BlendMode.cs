namespace Radzen.Documents.Pdf;


/// <summary>
/// A colour blend mode selected through a graphics-state parameter dictionary's
/// <c>/BM</c> entry (ISO 32000-1 11.3.5). The first twelve are separable blend modes;
/// the last four are non-separable.
/// </summary>
public enum BlendMode
{
    /// <summary>Paints the source over the backdrop (the default).</summary>
    Normal,

    /// <summary>Multiplies the backdrop and source colours.</summary>
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

    /// <summary>Subtracts the darker of the two colours from the lighter.</summary>
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

internal static class BlendModes
{
    public static string PdfName(this BlendMode mode) => mode switch
    {
        BlendMode.Multiply => "Multiply",
        BlendMode.Screen => "Screen",
        BlendMode.Overlay => "Overlay",
        BlendMode.Darken => "Darken",
        BlendMode.Lighten => "Lighten",
        BlendMode.ColorDodge => "ColorDodge",
        BlendMode.ColorBurn => "ColorBurn",
        BlendMode.HardLight => "HardLight",
        BlendMode.SoftLight => "SoftLight",
        BlendMode.Difference => "Difference",
        BlendMode.Exclusion => "Exclusion",
        BlendMode.Hue => "Hue",
        BlendMode.Saturation => "Saturation",
        BlendMode.Color => "Color",
        BlendMode.Luminosity => "Luminosity",
        _ => "Normal",
    };
}
