using System;

namespace Radzen.Documents.Pdf.Content;

// The reading-order rule, applied to the runs TextSearch parses. ExtractText and FindText
// must keep reaching it through that one Parse/Sort/Compose path: a second walk measuring
// its own advances is how the two silently disagreed about separators before.
internal static class TextComposition
{
    public const double LineTolerance = 0.5;

    // TJ adjustments are in thousandths of an em; a leftward move (negative) beyond
    // ~0.2 em is an inter-word gap, smaller values are kerning.
    public const double TjSpaceThreshold = 200.0;

    // Stands in for a width the font does not provide, keeping a partial-width font searchable.
    // Reading may estimate and says so via GeometryEstimated; editing (TextReplacer, Redactor)
    // must refuse an estimate rather than consume one.
    public const double AverageGlyphEm = 0.5;

    // Sits below a space (~0.25 em) yet clears kerning and abutting fragments.
    private const double SpaceGapEm = 0.2;

    public readonly record struct Placement(double Y, double X, double Advance, double Em);

    public static Placement Place(Matrix matrix, double advance, double fontSize)
    {
        var origin = matrix.Transform(0, 0);
        var pen = matrix.Transform(advance, 0);
        var emPoint = matrix.Transform(fontSize, 0);
        return new Placement(origin.Y, origin.X, pen.X - origin.X, emPoint.X - origin.X);
    }

    public static int Compare(Placement a, Placement b)
        => Math.Abs(a.Y - b.Y) > LineTolerance ? b.Y.CompareTo(a.Y) : a.X.CompareTo(b.X);

    public static char? Separator(Placement previous, string previousText, Placement current, string currentText)
    {
        if (Math.Abs(current.Y - previous.Y) > LineTolerance)
        {
            return '\n';
        }

        return NeedsSpace(previous, previousText, current, currentText) ? ' ' : null;
    }

    private static bool NeedsSpace(Placement previous, string previousText, Placement current, string currentText)
    {
        if (previousText.Length == 0 || currentText.Length == 0
            || char.IsWhiteSpace(previousText[^1]) || char.IsWhiteSpace(currentText[0]))
        {
            return false;
        }

        var gap = current.X - (previous.X + previous.Advance);

        // A zero em means a zero font size, which would make every positive gap a word break.
        var em = Math.Abs(current.Em != 0 ? current.Em : previous.Em);
        return gap > SpaceGapEm * em;
    }
}
