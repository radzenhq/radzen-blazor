namespace Radzen.Documents.Pdf.Emit;

internal static class RunTextAdvance
{
    internal static double Measure(
        FontCollection fonts,
        Run run,
        Font font,
        string text,
        bool leadingCharacterSpacing = false,
        bool trailingCharacterSpacing = false)
    {
        var glyphCount = 0;
        var wordSpaceCount = 0;
        for (var i = 0; i < text.Length; glyphCount++)
        {
            var codePointLength = char.IsHighSurrogate(text[i])
                && i + 1 < text.Length
                && char.IsLowSurrogate(text[i + 1])
                ? 2
                : 1;
            if (codePointLength == 1 && text[i] == ' ')
            {
                wordSpaceCount++;
            }

            i += codePointLength;
        }

        return Calculate(
            fonts.MeasureText(text, font), glyphCount, wordSpaceCount, run,
            leadingCharacterSpacing, trailingCharacterSpacing);
    }

    internal static double Calculate(
        double glyphAdvance,
        int glyphCount,
        int wordSpaceCount,
        Run run,
        bool leadingCharacterSpacing = false,
        bool trailingCharacterSpacing = false)
        => (glyphAdvance * run.ScriptScale
            + (System.Math.Max(0, glyphCount - 1)
                + (leadingCharacterSpacing ? 1 : 0)
                + (trailingCharacterSpacing ? 1 : 0)) * run.LetterSpacing.Point
            + wordSpaceCount * run.WordSpacing.Point)
            * (run.HorizontalScale / 100.0);
}
