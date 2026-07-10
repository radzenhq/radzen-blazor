#nullable enable
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// Alignment contract for the L2 line breaker. v1 supports LTR only, so Start resolves
// to Left and End resolves to Right. Left/Start put the first fragment at x=0; Right/End
// shift the line by (maxWidth - lineWidth); Center shifts by half that. Word gaps stay a
// single natural space in all non-Justify modes (Justify is covered separately).
public class AlignmentTests
{
    private const string Phrase = "The quick brown";

    private static (LineBox Line, double MaxWidth, double LineWidth) LayoutSingleLine(
        HorizontalAlignment alignment)
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Phrase, alignment: alignment);
        var lineWidth = fonts.MeasureText(Phrase, LineLayoutSupport.FontAt(12));
        var maxWidth = lineWidth + 120.0;

        var lines = LineBreaker.Break(paragraph, maxWidth, fonts);
        Assert.Single(lines);
        return (lines[0], maxWidth, lineWidth);
    }

    private static double RightEdge(LineBox line)
    {
        var last = line.Fragments[^1];
        return last.XOffset + last.Advance;
    }

    [Fact]
    public void Left_FirstFragmentAtZero()
    {
        var (line, _, lineWidth) = LayoutSingleLine(HorizontalAlignment.Left);

        Assert.Equal(0.0, line.Fragments[0].XOffset, 6);
        Assert.Equal(lineWidth, RightEdge(line), 6);
    }

    [Fact]
    public void Center_FirstFragmentShiftedByHalfSlack()
    {
        var (line, maxWidth, lineWidth) = LayoutSingleLine(HorizontalAlignment.Center);

        var expected = (maxWidth - lineWidth) / 2.0;
        Assert.Equal(expected, line.Fragments[0].XOffset, 6);
        Assert.Equal(expected + lineWidth, RightEdge(line), 6);
    }

    [Fact]
    public void Right_LineRightEdgeMeetsMaxWidth()
    {
        var (line, maxWidth, lineWidth) = LayoutSingleLine(HorizontalAlignment.Right);

        Assert.Equal(maxWidth - lineWidth, line.Fragments[0].XOffset, 6);
        Assert.Equal(maxWidth, RightEdge(line), 6);
    }

    [Fact]
    public void Start_ResolvesToLeft_ForLtr()
    {
        var (start, _, _) = LayoutSingleLine(HorizontalAlignment.Start);
        var (left, _, _) = LayoutSingleLine(HorizontalAlignment.Left);

        Assert.Equal(left.Fragments[0].XOffset, start.Fragments[0].XOffset, 6);
        Assert.Equal(RightEdge(left), RightEdge(start), 6);
    }

    [Fact]
    public void End_ResolvesToRight_ForLtr()
    {
        var (end, _, _) = LayoutSingleLine(HorizontalAlignment.End);
        var (right, maxWidth, _) = LayoutSingleLine(HorizontalAlignment.Right);

        Assert.Equal(right.Fragments[0].XOffset, end.Fragments[0].XOffset, 6);
        Assert.Equal(maxWidth, RightEdge(end), 6);
    }

    [Fact]
    public void InteriorGaps_AreNaturalSpace_WhenNotJustified()
    {
        var fonts = LineLayoutSupport.Fonts();
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var (line, _, _) = LayoutSingleLine(HorizontalAlignment.Center);

        for (var k = 1; k < line.Fragments.Count; k++)
        {
            var gap = line.Fragments[k].XOffset
                - (line.Fragments[k - 1].XOffset + line.Fragments[k - 1].Advance);
            Assert.Equal(space, gap, 6);
        }
    }
}
