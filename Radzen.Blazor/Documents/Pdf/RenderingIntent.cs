namespace Radzen.Documents.Pdf;


/// <summary>
/// The colour rendering intent used when converting colours to the output device's
/// gamut (ISO 32000-1 8.6.5.8, the <c>ri</c> operator and the <c>/Intent</c> entry).
/// </summary>
public enum RenderingIntent
{
    /// <summary>Preserve appearance relative to the true white of the output medium.</summary>
    AbsoluteColorimetric,

    /// <summary>Preserve appearance relative to the output medium's white point.</summary>
    RelativeColorimetric,

    /// <summary>Preserve saturation, favouring vivid colours over exact hue.</summary>
    Saturation,

    /// <summary>Preserve overall appearance, compressing the gamut smoothly.</summary>
    Perceptual,
}
