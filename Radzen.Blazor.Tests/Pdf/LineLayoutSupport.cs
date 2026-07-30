#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;

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

    public static void AssertFitsAndPreservesWords(
        FontCollection fonts,
        IReadOnlyList<LineBox> lines,
        string[] words,
        double max,
        double size = 12,
        double tolerance = 1e-6)
    {
        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.NotEmpty(line.Fragments));
        Assert.All(lines, line => Assert.True(line.Width <= max + tolerance,
            $"line '{string.Join(" ", line.Fragments.Select(f => f.Text))}' is {line.Width}pt wide, measure is {max}pt"));

        Assert.Equal(words, lines.SelectMany(l => l.Fragments).Select(f => f.Text).ToArray());

        var space = SpaceWidth(fonts, size);
        for (var i = 0; i + 1 < lines.Count; i++)
        {
            var next = lines[i + 1].Fragments[0];
            Assert.True(lines[i].Width + space + next.Advance > max - tolerance,
                $"line {i} could still take '{next.Text}' within {max}pt");
        }
    }

    public static string[][] Grouping(IReadOnlyList<LineBox> lines)
        => lines.Select(l => l.Fragments.Select(f => f.Text).ToArray()).ToArray();
}
