using System;

namespace Radzen.Documents.Pdf.Content;

// The reading-order rule shared by extraction and search: runs sort by descending Y then
// ascending X, a Y step beyond LineTolerance breaks a line, and a same-line gap clearing
// SpaceGapEm reads as a word break. The advance behind a Placement is the caller's model -
// search measures real glyph widths, extraction estimates - but the rule applied to it is
// one job, so ExtractText and FindText cannot disagree about where a separator belongs.
internal static class TextComposition
{
    public const double LineTolerance = 0.5;

    // TJ adjustments are in thousandths of an em; a leftward move (negative) beyond
    // ~0.2 em is an inter-word gap, smaller values are kerning. Third-party streams
    // rely on this for word breaks; the authoring path never emits TJ arrays.
    public const double TjSpaceThreshold = 200.0;

    // Stands in for a width the font does not provide, and is the whole advance model of
    // estimated extraction; it only ever feeds the gap test, never the emitted text.
    public const double AverageGlyphEm = 0.5;

    // Two same-line fragments read as separate words only when the X gap between the
    // pen left by one and the origin of the next exceeds this fraction of an em, which
    // sits below a space (~0.25 em) yet clears kerning and abutting fragments.
    private const double SpaceGapEm = 0.2;

    // A run reduced to the device-space quantities the rule needs: its origin, the pen
    // delta its own advance model produced, and the device width of one em.
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

    // The separator belonging between two adjacent runs, or null when they abut.
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
