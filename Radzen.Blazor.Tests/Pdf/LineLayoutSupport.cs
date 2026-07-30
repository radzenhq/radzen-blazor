#nullable enable
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

internal static class LineLayoutSupport
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

    public static Paragraph SingleRun(
        string text,
        double size = 12,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        double lineSpacing = 1.0)
    {
        var paragraph = new Paragraph { Alignment = alignment, LineSpacing = lineSpacing };
        var run = paragraph.Inlines.Add(text);
        run.Font.Family = Family;
        run.Font.Size = size;
        return paragraph;
    }

    public static double WordWidth(FontCollection fonts, string word, double size)
        => fonts.MeasureText(word, FontAt(size));

    public static double SpaceWidth(FontCollection fonts, double size)
        => fonts.MeasureText(" ", FontAt(size));

    public static List<(int First, int Last)> Wrap(double[] widths, double space, double max)
    {
        var lines = new List<(int, int)>();
        var i = 0;
        while (i < widths.Length)
        {
            var j = i;
            var width = widths[i];
            while (j + 1 < widths.Length && width + space + widths[j + 1] <= max)
            {
                j++;
                width += space + widths[j];
            }

            lines.Add((i, j));
            i = j + 1;
        }

        return lines;
    }

    public static int WordStart(string[] words, int k)
    {
        var start = 0;
        for (var m = 0; m < k; m++)
        {
            start += words[m].Length + 1;
        }

        return start;
    }
}
