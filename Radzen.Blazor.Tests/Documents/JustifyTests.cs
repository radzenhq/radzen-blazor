#nullable enable
using System.Linq;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class JustifyTests
{
    private const string Sentence =
        "The quick brown fox jumps over the lazy dog and then some more words here";
    private const double MaxWidth = 170.0;

    private static (LineBox[] Lines, double Space) Layout()
    {
        var fonts = LineLayoutSupport.Fonts();
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var paragraph = LineLayoutSupport.SingleRun(Sentence, alignment: HorizontalAlignment.Justify);
        var lines = IsolatedLineBreaker.Break(paragraph, MaxWidth, fonts).ToArray();
        Assert.True(lines.Length >= 3);
        return (lines, space);
    }

    private static double RightEdge(LineBox line)
    {
        var last = line.Fragments[^1];
        return last.XOffset + last.Advance;
    }

    [Fact]
    public void NonLastLines_RightEdgeEqualsMaxWidth_Exactly()
    {
        var (lines, _) = Layout();

        for (var i = 0; i < lines.Length - 1; i++)
        {
            if (lines[i].Fragments.Length < 2)
            {
                continue;
            }

            Assert.Equal(MaxWidth, RightEdge(lines[i]), 6);
            Assert.Equal(0.0, lines[i].Fragments[0].XOffset, 6);
        }
    }

    [Fact]
    public void NonLastLines_SummedAdvancesPlusGaps_FillMaxWidth()
    {
        var (lines, _) = Layout();

        for (var i = 0; i < lines.Length - 1; i++)
        {
            var line = lines[i];
            if (line.Fragments.Length < 2)
            {
                continue;
            }

            double sum = 0;
            for (var k = 0; k < line.Fragments.Length; k++)
            {
                sum += line.Fragments[k].Advance;
                if (k > 0)
                {
                    sum += line.Fragments[k].XOffset
                        - (line.Fragments[k - 1].XOffset + line.Fragments[k - 1].Advance);
                }
            }

            Assert.Equal(MaxWidth, sum, 6);
        }
    }

    [Fact]
    public void NonLastLines_GapsAreEqual_AndAtLeastNaturalSpace()
    {
        var (lines, space) = Layout();

        for (var i = 0; i < lines.Length - 1; i++)
        {
            var line = lines[i];
            if (line.Fragments.Length < 2)
            {
                continue;
            }

            var advances = line.Fragments.Sum(f => f.Advance);
            var gapCount = line.Fragments.Length - 1;
            var expectedGap = (MaxWidth - advances) / gapCount;

            for (var k = 1; k < line.Fragments.Length; k++)
            {
                var gap = line.Fragments[k].XOffset
                    - (line.Fragments[k - 1].XOffset + line.Fragments[k - 1].Advance);
                Assert.Equal(expectedGap, gap, 6);
            }

            Assert.True(expectedGap >= space - 1e-6);
        }
    }

    [Fact]
    public void WordAdvances_Unchanged_ByJustification()
    {
        var fonts = LineLayoutSupport.Fonts();
        var (lines, _) = Layout();

        foreach (var line in lines)
        {
            foreach (var frag in line.Fragments)
            {
                var natural = fonts.MeasureText(frag.Text, LineLayoutSupport.FontAt(12));
                Assert.Equal(natural, frag.Advance, 6);
            }
        }
    }

    [Fact]
    public void LastLine_NotJustified_LeftAligned()
    {
        var (lines, _) = Layout();
        var last = lines[^1];

        Assert.Equal(0.0, last.Fragments[0].XOffset, 6);
        Assert.True(RightEdge(last) < MaxWidth - 1e-6);
    }
}
