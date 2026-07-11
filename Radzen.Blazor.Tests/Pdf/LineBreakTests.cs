#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// Contract pinned for the L2 line breaker (INTERNAL, namespace Radzen.Documents.Pdf,
// reachable via InternalsVisibleTo). No PDF bytes are produced; assertions are purely
// numeric on the laid-out model using deterministic Liberation Sans metrics.
//
// Pinned shapes:
//  - static IReadOnlyList<LineBox> LineBreaker.Break(Paragraph paragraph,
//        double maxWidthPoints, FontCollection fonts)
//  - LineBox { IReadOnlyList<LineFragment> Fragments; double Width; double Height;
//        double Baseline; }
//  - LineFragment { Run Run; string Text; int Start; int Length;
//        double XOffset; double Advance; }
//
// Pinned semantics:
//  - Greedy word wrap at single ASCII spaces. A word joins the current line iff
//    currentWidth + spaceWidth + wordWidth <= maxWidth (no leading space for the first
//    word). The space that triggers a break is consumed (not a fragment, not counted
//    in the line width).
//  - One LineFragment per maximal non-space run of chars within a single Run. Text is
//    the word with NO surrounding spaces; Start/Length index into that Run's Text.
//    Fragment.Advance == FontCollection.MeasureText(Text, run.Font).
//  - Words that fit are never split mid-word (no hyphenation). A single token wider
//    than maxWidth is broken at code-point granularity so no line exceeds the measure;
//    a lone glyph wider than maxWidth occupies its own line as a last resort.
//  - LineBox.Width is the natural visible width of the line (sum of fragment advances
//    plus interior single-space gaps); trailing spaces are excluded.
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

        // Wide enough for any single word, too narrow for the narrowest word pair: word-level wrap only.
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

        // The third word starts the second line.
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
        var words = Words;
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
