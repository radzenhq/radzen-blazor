#nullable enable
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class MixedRunTests
{
    private static Paragraph MixedParagraph(out Run first, out Run second)
    {
        var paragraph = new Paragraph();
        first = paragraph.Inlines.Add("Hello ");
        first.Font.Name = LineLayoutSupport.Family;
        first.Font.Size = 12;

        second = paragraph.Inlines.Add("World");
        second.Font.Name = LineLayoutSupport.Family;
        second.Font.Size = 18;
        return paragraph;
    }

    [Fact]
    public void RunBoundary_ProducesTwoFragments_WithSourceRuns()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = MixedParagraph(out var r1, out var r2);

        var lines = LineBreaker.Break(paragraph, 500.0, fonts);

        Assert.Single(lines);
        Assert.Equal(2, lines[0].Fragments.Count);
        Assert.Same(r1, lines[0].Fragments[0].Run);
        Assert.Same(r2, lines[0].Fragments[1].Run);
    }

    [Fact]
    public void EachFragment_HasItsRunsText_AndIndices()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = MixedParagraph(out _, out _);

        var frags = LineBreaker.Break(paragraph, 500.0, fonts)[0].Fragments;

        Assert.Equal("Hello", frags[0].Text);
        Assert.Equal(0, frags[0].Start);
        Assert.Equal(5, frags[0].Length);

        Assert.Equal("World", frags[1].Text);
        Assert.Equal(0, frags[1].Start);
        Assert.Equal(5, frags[1].Length);
    }

    [Fact]
    public void PerRunAdvances_UseEachRunsFontSize()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = MixedParagraph(out _, out _);

        var frags = LineBreaker.Break(paragraph, 500.0, fonts)[0].Fragments;

        Assert.Equal(fonts.MeasureText("Hello", LineLayoutSupport.FontAt(12)), frags[0].Advance, 6);
        Assert.Equal(fonts.MeasureText("World", LineLayoutSupport.FontAt(18)), frags[1].Advance, 6);
    }

    [Fact]
    public void SecondRun_XOffset_AccountsForSeparatingSpaceInFirstRunsFont()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = MixedParagraph(out _, out _);

        var line = LineBreaker.Break(paragraph, 500.0, fonts)[0];

        var expected = fonts.MeasureText("Hello", LineLayoutSupport.FontAt(12))
            + fonts.MeasureText(" ", LineLayoutSupport.FontAt(12));

        Assert.Equal(0.0, line.Fragments[0].XOffset, 6);
        Assert.Equal(expected, line.Fragments[1].XOffset, 6);
        Assert.Equal(expected + fonts.MeasureText("World", LineLayoutSupport.FontAt(18)), line.Width, 6);
    }

    [Fact]
    public void MixedLineHeight_ExceedsAllSmallFontLine()
    {
        var fonts = LineLayoutSupport.Fonts();
        var mixed = MixedParagraph(out _, out _);
        var small = LineLayoutSupport.SingleRun("Hello World", size: 12);

        var mixedLine = LineBreaker.Break(mixed, 500.0, fonts)[0];
        var smallLine = LineBreaker.Break(small, 500.0, fonts)[0];

        Assert.True(mixedLine.Height > smallLine.Height);
        Assert.True(mixedLine.Height > 0);
        Assert.True(mixedLine.Baseline > 0);
        Assert.True(mixedLine.Baseline <= mixedLine.Height + 1e-6);
    }

    [Fact]
    public void LineSpacing_ScalesLinePitchLinearly()
    {
        var fonts = LineLayoutSupport.Fonts();
        var sentence = "The quick brown fox jumps over the lazy dog and then some more";
        var single = LineLayoutSupport.SingleRun(sentence, size: 12, lineSpacing: 1.0);
        var oneAndHalf = LineLayoutSupport.SingleRun(sentence, size: 12, lineSpacing: 1.5);
        const double MaxWidth = 150.0;

        var linesSingle = LineBreaker.Break(single, MaxWidth, fonts);
        var linesWide = LineBreaker.Break(oneAndHalf, MaxWidth, fonts);

        Assert.Equal(linesSingle.Count, linesWide.Count);
        Assert.True(linesSingle.Count >= 2);

        for (var i = 0; i < linesSingle.Count; i++)
        {
            Assert.Equal(1.5 * linesSingle[i].Height, linesWide[i].Height, 6);
        }
    }

    [Fact]
    public void LineSpacing_DoesNotAffectHorizontalLayout()
    {
        var fonts = LineLayoutSupport.Fonts();
        var sentence = "The quick brown fox jumps over the lazy dog";
        var single = LineLayoutSupport.SingleRun(sentence, size: 12, lineSpacing: 1.0);
        var oneAndHalf = LineLayoutSupport.SingleRun(sentence, size: 12, lineSpacing: 1.5);
        const double MaxWidth = 150.0;

        var a = LineBreaker.Break(single, MaxWidth, fonts);
        var b = LineBreaker.Break(oneAndHalf, MaxWidth, fonts);

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Width, b[i].Width, 6);
            Assert.Equal(a[i].Fragments.Count, b[i].Fragments.Count);
        }
    }
}
