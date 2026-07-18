using Radzen.Documents.Pdf.Content;

namespace Radzen.Documents.Pdf.Fonts;

internal enum MissingWidthPolicy
{
    Estimate,
    Throw,
}

internal static class LoadedGlyphAdvance
{
    internal static double Calculate(
        ReverseFont font,
        int code,
        bool isWordSpace,
        double fontSize,
        double horizontalScale,
        double charSpacing,
        double wordSpacing,
        MissingWidthPolicy missingWidthPolicy,
        out bool estimated)
    {
        var known = font.TryGetWidth(code, out var width);
        if (!known && missingWidthPolicy == MissingWidthPolicy.Throw)
        {
            throw new System.NotSupportedException(
                $"The source font does not provide a usable width for character code {code}.");
        }

        estimated = !known;
        var widthEm = known ? width / 1000.0 : TextComposition.AverageGlyphEm;
        return GlyphMetrics.Advance(widthEm, fontSize, charSpacing, wordSpacing, isWordSpace)
            * horizontalScale;
    }
}
