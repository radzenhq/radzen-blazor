#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

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

    public static Paragraph Repeated(string word, int count, double size = 12)
    {
        var joined = string.Join(" ", Enumerable.Repeat(word, count));
        return Text(joined, size);
    }

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

    public static double HeightForLines(double lineHeight, int lines)
        => (lines + 0.4) * lineHeight;

    public static IReadOnlyList<string> BodyTexts(PaginatedPage page)
        => [.. page.Lines.Select(l => string.Concat(l.Line.Fragments.Select(f => f.Text)))];
}
