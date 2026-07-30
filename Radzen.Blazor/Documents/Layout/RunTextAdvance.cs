using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class RunTextAdvance
{
    internal static double Measure(
        FontCollection fonts,
        Run run,
        Font font,
        string text,
        bool leadingCharacterSpacing = false,
        bool trailingCharacterSpacing = false)
        => MeasureCore(
            fonts,
            font,
            text,
            run.ScriptScale,
            run.LetterSpacing.Point,
            run.WordSpacing.Point,
            run.HorizontalScale,
            leadingCharacterSpacing, trailingCharacterSpacing);

    internal static double Measure(
        FontCollection fonts,
        in FragmentPaint paint,
        Font font,
        string text,
        bool leadingCharacterSpacing = false,
        bool trailingCharacterSpacing = false)
        => MeasureCore(
            fonts,
            font,
            text,
            paint.ScriptScale,
            paint.LetterSpacing,
            paint.WordSpacing,
            paint.HorizontalScale,
            leadingCharacterSpacing, trailingCharacterSpacing);

    private static double MeasureCore(
        FontCollection fonts,
        Font font,
        string text,
        double scriptScale,
        double letterSpacing,
        double wordSpacing,
        double horizontalScale,
        bool leadingCharacterSpacing,
        bool trailingCharacterSpacing)
    {
        var glyphCount = 0;
        var wordSpaceCount = 0;
        for (var i = 0; i < text.Length; glyphCount++)
        {
            if (FontCollection.CodePointAt(text, i, out var codePointLength) == ' ')
            {
                wordSpaceCount++;
            }

            i += codePointLength;
        }

        return CalculateCore(
            fonts.MeasureText(text, font),
            glyphCount,
            wordSpaceCount,
            scriptScale,
            letterSpacing,
            wordSpacing,
            horizontalScale,
            leadingCharacterSpacing, trailingCharacterSpacing);
    }

    internal static double Calculate(
        double glyphAdvance,
        int glyphCount,
        int wordSpaceCount,
        in FragmentPaint paint,
        bool leadingCharacterSpacing = false,
        bool trailingCharacterSpacing = false)
        => CalculateCore(
            glyphAdvance,
            glyphCount,
            wordSpaceCount,
            paint.ScriptScale,
            paint.LetterSpacing,
            paint.WordSpacing,
            paint.HorizontalScale,
            leadingCharacterSpacing,
            trailingCharacterSpacing);

    internal static double Calculate(
        double glyphAdvance,
        int glyphCount,
        int wordSpaceCount,
        Run run,
        bool leadingCharacterSpacing = false,
        bool trailingCharacterSpacing = false)
        => CalculateCore(
            glyphAdvance,
            glyphCount,
            wordSpaceCount,
            run.ScriptScale,
            run.LetterSpacing.Point,
            run.WordSpacing.Point,
            run.HorizontalScale,
            leadingCharacterSpacing,
            trailingCharacterSpacing);

    private static double CalculateCore(
        double glyphAdvance,
        int glyphCount,
        int wordSpaceCount,
        double scriptScale,
        double letterSpacing,
        double wordSpacing,
        double horizontalScale,
        bool leadingCharacterSpacing,
        bool trailingCharacterSpacing)
        => (glyphAdvance * scriptScale
            + (System.Math.Max(0, glyphCount - 1)
                + (leadingCharacterSpacing ? 1 : 0)
                + (trailingCharacterSpacing ? 1 : 0)) * letterSpacing
            + wordSpaceCount * wordSpacing)
            * horizontalScale;
}
