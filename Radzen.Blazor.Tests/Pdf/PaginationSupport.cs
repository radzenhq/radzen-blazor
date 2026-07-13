#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Shared fixture for the L3 flow-pagination contract tests. Everything numeric is
// derived from the already-merged FontCollection + LineBreaker using deterministic
// Liberation Sans metrics (unitsPerEm 2048, hhea ascent 1854 / descent -434 /
// lineGap 67); nothing is hardcoded from memory.
//
// Pinned INTERNAL paginator (namespace Radzen.Documents.Pdf, reachable via
// InternalsVisibleTo). No PDF bytes are produced; asserts are purely numeric on the
// laid-out model.
//
//   static IReadOnlyList<PaginatedPage> Paginator.Paginate(Section, FontCollection)
//   static IReadOnlyList<PaginatedPage> Paginator.Paginate(DocumentBuilder, FontCollection)
//
//   PaginatedPage { PageSize Size; Rect ContentBox; int Number;      // 1-based
//       IReadOnlyList<PositionedLine> Lines;                          // body
//       IReadOnlyList<PositionedLine> Header;                         // top-margin band
//       IReadOnlyList<PositionedLine> Footer; }                       // bottom-margin band
//
//   PositionedLine { LineBox Line; Block Source; double Y; }
//
// Coordinate spaces (all increase downward):
//   * ContentBox = Rect(marginLeft, marginTop, pageWidth - left - right,
//                       pageHeight - top - bottom). Effective page dims honor
//                       Section.Orientation (portrait unless swapped).
//   * Body PositionedLine.Y is measured from the TOP of ContentBox (0..Height).
//   * Header PositionedLine.Y is measured from the page top edge (band-local origin 0).
//   * Footer PositionedLine.Y is measured from the top of the bottom-margin band
//     (i.e. page bottom minus marginBottom), band-local origin 0.
//
// Body flow semantics (pinned):
//   * Blocks flow in order. A block's top = cursor + SpacingBefore (SpacingBefore is
//     ALWAYS applied, even at the top of a page). After a block, cursor = blockTop +
//     sum(lineHeights) + SpacingAfter.
//   * Each body line's Y = its cumulative top offset from ContentBox top. A line "fits"
//     iff lineTop + lineHeight <= ContentBox.Height.
//   * PageBreak flushes the current page and resets cursor to 0 on a fresh page.
//   * Widows/Orphans (defaults 2): when a paragraph splits across a page boundary at
//     least Orphans lines must remain at the bottom and at least Widows lines must
//     carry to the top. If only k lines fit and k < Orphans the whole paragraph moves
//     to the next page; if the remainder r = N-k satisfies r < Widows, k is reduced to
//     N-Widows (still requiring k >= Orphans, otherwise the whole paragraph moves).
//   * KeepTogether: a paragraph that does not fit whole in the remaining space moves
//     WHOLE to the next page.
//   * KeepWithNext: keeps a paragraph on the same page as the first line of the
//     following block.
//   * Each Section starts on a fresh page.
internal static class PaginationSupport
{
    public const string Family = "Liberation Sans";

    public static FontCollection Fonts()
    {
        var fonts = new FontCollection();
        fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return fonts;
    }

    public static Font FontAt(double size) => new() { Name = Family, Size = size };

    public static double Measure(FontCollection fonts, string text, double size)
        => fonts.MeasureText(text, FontAt(size));

    // The pinned single-line height for a Liberation paragraph at the given size /
    // spacing, taken straight from the L2 LineBreaker so product and test share metrics.
    public static double LineHeight(FontCollection fonts, double size = 12, double spacing = 1.0)
    {
        var p = Text("Xg", size);
        p.LineSpacing = spacing;
        return LineBreaker.Break(p, 100000, fonts)[0].Height;
    }

    public static Paragraph Text(string text, double size = 12)
    {
        var p = new Paragraph();
        var run = p.Inlines.Add(text);
        run.Font.Name = Family;
        run.Font.Size = size;
        return p;
    }

    // A paragraph of `count` identical words; combined with WidthForWordsPerLine this
    // yields an exact, deterministic line count regardless of the proportional metrics.
    public static Paragraph Repeated(string word, int count, double size = 12)
    {
        var joined = string.Join(" ", Enumerable.Repeat(word, count));
        return Text(joined, size);
    }

    // Content width that fits exactly `n` copies of `word` per greedy-wrapped line.
    public static double WidthForWordsPerLine(FontCollection fonts, string word, int n, double size)
    {
        var w = Measure(fonts, word, size);
        var s = Measure(fonts, " ", size);
        return (n * w) + ((n - 1) * s) + (0.5 * s);
    }

    public static Section Section(double widthPt, double heightPt, double marginPt = 0)
    {
        var section = new Section
        {
            PageSize = new PageSize(Unit.FromPoint(widthPt), Unit.FromPoint(heightPt)),
        };
        section.Margin = Unit.FromPoint(marginPt);
        return section;
    }

    // Page height (margins 0) whose content rectangle fits exactly `lines` Liberation
    // lines with slack, so the fit boundary is float-robust.
    public static double HeightForLines(double lineHeight, int lines)
        => (lines + 0.4) * lineHeight;

    public static IReadOnlyList<string> BodyTexts(PaginatedPage page)
        => [.. page.Lines.Select(l => string.Concat(l.Line.Fragments.Select(f => f.Text)))];
}
