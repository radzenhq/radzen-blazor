using Radzen.Documents.Core;

namespace Radzen.Documents.Fonts;

internal readonly record struct FontPaint(
    string Family,
    double Size,
    bool Bold,
    bool Italic,
    bool Underline,
    bool Strikethrough,
    Color Color);

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
