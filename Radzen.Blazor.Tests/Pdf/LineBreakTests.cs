#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class LineBreakTests
{
    private const string Sentence =
        "The quick brown fox jumps over the lazy dog and then some more words here";

    private static string[] Words => Sentence.Split(' ');

    [Fact]
    public void EntireSentence_FitsOnOneLine_WhenWidthIsFullWidth()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var full = fonts.MeasureText(Sentence, LineLayoutSupport.FontAt(12));

        var lines = LineBreaker.Break(paragraph, full + 1.0, fonts);

        Assert.Single(lines);
        Assert.Equal(Words.Length, lines[0].Fragments.Count);
        Assert.Equal(full, lines[0].Width, 6);
    }

    [Fact]
    public void EachWordOnItsOwnLine_WhenWidthHoldsTheWidestWord()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var widest = Words.Max(w => LineLayoutSupport.WordWidth(fonts, w, 12));

        var lines = LineBreaker.Break(paragraph, widest + 1.0, fonts);

        Assert.Equal(Words.Length, lines.Count);
        foreach (var line in lines)
        {
            Assert.Single(line.Fragments);
        }

        Assert.Equal("The", lines[0].Fragments[0].Text);
        Assert.Equal("here", lines[^1].Fragments[0].Text);
    }

    [Fact]
    public void SingleCharacterPerLine_WhenWidthIsSmallerThanEveryGlyph()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);

        var lines = LineBreaker.Break(paragraph, 1.0, fonts);

        var nonSpace = Sentence.Count(c => c != ' ');
        Assert.Equal(nonSpace, lines.Count);
        foreach (var line in lines)
        {
            Assert.Single(line.Fragments);
            Assert.Equal(1, line.Fragments[0].Text.Length);
        }
    }

    [Fact]
    public void WidthBetweenTwoAndThreeWords_FirstLineHoldsExactlyTwoWords()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);

        var w0 = LineLayoutSupport.WordWidth(fonts, "The", 12);
        var w1 = LineLayoutSupport.WordWidth(fonts, "quick", 12);
        var w2 = LineLayoutSupport.WordWidth(fonts, "brown", 12);

        var twoWords = w0 + space + w1;
        var threeWords = twoWords + space + w2;
        var maxWidth = (twoWords + threeWords) / 2.0;

        var lines = LineBreaker.Break(paragraph, maxWidth, fonts);

        Assert.Equal(2, lines[0].Fragments.Count);
        Assert.Equal("The", lines[0].Fragments[0].Text);
        Assert.Equal("quick", lines[0].Fragments[1].Text);
        Assert.Equal(twoWords, lines[0].Width, 6);

        Assert.Equal("brown", lines[1].Fragments[0].Text);
    }

    [Fact]
    public void FragmentStartAndLength_IndexIntoRunText()
    {
        var fonts = LineLayoutSupport.Fonts();
        var words = Words;
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var run = paragraph.Inlines[0];

        var widths = words.Select(w => LineLayoutSupport.WordWidth(fonts, w, 12)).ToArray();
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        const double MaxWidth = 160.0;

        var expected = LineLayoutSupport.Wrap(widths, space, MaxWidth);
        var lines = LineBreaker.Break(paragraph, MaxWidth, fonts);

        Assert.Equal(expected.Count, lines.Count);

        for (var li = 0; li < expected.Count; li++)
        {
            var (first, last) = expected[li];
            var line = lines[li];
            Assert.Equal(last - first + 1, line.Fragments.Count);

            for (var k = first; k <= last; k++)
            {
                var frag = line.Fragments[k - first];
                Assert.Same(run, frag.Run);
                Assert.Equal(words[k], frag.Text);
                Assert.Equal(LineLayoutSupport.WordStart(words, k), frag.Start);
                Assert.Equal(words[k].Length, frag.Length);
                Assert.Equal(run.Text.Substring(frag.Start, frag.Length), frag.Text);
                Assert.Equal(widths[k], frag.Advance, 6);
            }
        }
    }

    [Fact]
    public void LineWidth_EqualsSumOfAdvancesPlusInteriorSpaces()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        const double MaxWidth = 160.0;

        var lines = LineBreaker.Break(paragraph, MaxWidth, fonts);

        foreach (var line in lines)
        {
            var advances = line.Fragments.Sum(f => f.Advance);
            var gaps = (line.Fragments.Count - 1) * space;
            Assert.Equal(advances + gaps, line.Width, 6);
        }
    }

    [Fact]
    public void OverlongSingleWord_SplitsAcrossLines_WithinTheMeasure()
    {
        const string Token = "Supercalifragilistic";
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Token);
        var width = LineLayoutSupport.WordWidth(fonts, Token, 12);
        var max = width / 3.0;

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1);
        Assert.All(lines, line => Assert.True(line.Width <= max + 1e-6));
        var joined = string.Concat(lines.Select(l => l.Fragments[0].Text));
        Assert.Equal(Token, joined);
        Assert.All(lines, line => Assert.True(line.Fragments[0].XOffset >= 0));
    }

    [Fact]
    public void FirstFragment_LeftAligned_StartsAtZero()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);

        var lines = LineBreaker.Break(paragraph, 160.0, fonts);

        foreach (var line in lines)
        {
            Assert.Equal(0.0, line.Fragments[0].XOffset, 6);
        }
    }
}
