using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Fonts;

internal static class FontPaintCapture
{
    internal static FontPaint Capture(Font font)
        => new(
            font.EffectiveFamily,
            font.EffectiveSize.Point,
            font.EffectiveBold,
            font.EffectiveItalic,
            font.EffectiveUnderline,
            font.EffectiveStrikethrough,
            font.EffectiveColor);
}
