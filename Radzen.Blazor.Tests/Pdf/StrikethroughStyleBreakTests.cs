#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class StrikethroughStyleBreakTests
{
    private const double Size = 12;

    private readonly record struct Stroke(double R, double G, double B, double X1, double X2);

    private static List<Stroke> Strokes(string content)
    {
        var list = new List<Stroke>();
        var pattern = new Regex(
            @"(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) RG\s+[-\d.]+ w\s+(-?[\d.]+) (-?[\d.]+) m\s+(-?[\d.]+) (-?[\d.]+) l");
        foreach (Match m in pattern.Matches(content))
        {
            double N(int g) => double.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);
            if (N(5) == N(7))
            {
                list.Add(new Stroke(N(1), N(2), N(3), N(4), N(6)));
            }
        }

        return list;
    }

    [Fact]
    public void AdjacentStruckRunsOfDifferentColor_EachGetOwnColoredLine()
    {
        var document = new Document();
        var paragraph = document.Sections.Add().Blocks.AddParagraph();

        var red = paragraph.Inlines.Add("Red ");
        red.Font.Size = Size;
        red.Font.Strikethrough = true;
        red.Font.Color = Color.Red;

        var blue = paragraph.Inlines.Add("Blue");
        blue.Font.Size = Size;
        blue.Font.Strikethrough = true;
        blue.Font.Color = Color.Blue;

        var strokes = Strokes(CascadeTestSupport.FirstPageContent(document));

        Assert.Equal(2, strokes.Count);
        Assert.Contains(strokes, s => s is { R: 1, G: 0, B: 0 });
        Assert.Contains(strokes, s => s is { R: 0, G: 0, B: 1 });

        var redLine = Assert.Single(strokes, s => s.R == 1);
        var blueLine = Assert.Single(strokes, s => s.B == 1);
        Assert.True(redLine.X2 <= blueLine.X1 + 0.5, "each run keeps its own segment");
    }

    [Fact]
    public void AdjacentStruckRunsOfSameStyle_MergeIntoOneLine()
    {
        var document = new Document();
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        foreach (var word in new[] { "One ", "Two" })
        {
            var run = paragraph.Inlines.Add(word);
            run.Font.Size = Size;
            run.Font.Strikethrough = true;
        }

        Assert.Single(Strokes(CascadeTestSupport.FirstPageContent(document)));
    }
}
