#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents;

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

    public static Font FontAt(double size) => new() { Family = Family, Size = size };

    public static double Measure(FontCollection fonts, string text, double size)
        => fonts.MeasureText(text, FontAt(size));

    public const double LiberationSansLineHeightPerEm = (1854 + 434 + 67) / 2048.0;

    public const double BuiltInLineHeightPerEm = 1.2;

    public static double LineHeight(double size = 12, double spacing = 1.0)
        => size * LiberationSansLineHeightPerEm * spacing;

    public static double BuiltInLineHeight(double size = 12) => size * BuiltInLineHeightPerEm;

    public static Paragraph Text(string text, double size = 12)
    {
        var p = new Paragraph();
        var run = p.Inlines.Add(text);
        run.Font.Family = Family;
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
            HeaderDistance = Unit.FromPoint(0),
            FooterDistance = Unit.FromPoint(0),
        };
        section.Margins.SetAll(Unit.FromPoint(marginPt));
        return section;
    }

    public static double HeightForLines(double lineHeight, int lines)
        => (lines + 0.4) * lineHeight;
}
