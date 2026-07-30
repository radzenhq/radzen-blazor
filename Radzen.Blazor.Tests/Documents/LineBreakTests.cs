#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class LineBreakTests
{
    private const string Sentence =
        "The quick brown fox jumps over the lazy dog and then some more words here";

    private const double MaxWidth = 160.0;

    private static string[] Words => Sentence.Split(' ');

    [Fact]
    public void EntireSentence_FitsOnOneLine_WhenWidthIsFullWidth()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var full = fonts.MeasureText(Sentence, LineLayoutSupport.FontAt(12));

        var lines = IsolatedLineBreaker.Break(paragraph, full + 1.0, fonts);

        Assert.Single(lines);
        Assert.Equal(Words.Length, lines[0].Fragments.Length);
        Assert.Equal(full, lines[0].Width, 6);
    }

    [Fact]
    public void ExactFitWidth_KeepsTheSentenceOnOneLine()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var full = fonts.MeasureText(Sentence, LineLayoutSupport.FontAt(12));

        var lines = IsolatedLineBreaker.Break(paragraph, full, fonts);

        Assert.Equal(new[] { Words }, LineLayoutSupport.Grouping(lines));
    }

    [Fact]
    public void ShortText_StaysOnOneLine()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Hello wrapped world");

        var lines = IsolatedLineBreaker.Break(paragraph, 200.0, fonts);

        Assert.Equal(
            new[] { new[] { "Hello", "wrapped", "world" } },
            LineLayoutSupport.Grouping(lines));
    }

    [Fact]
    public void EachWordOnItsOwnLine_WhenWidthHoldsTheWidestWord()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var widest = Words.Max(w => LineLayoutSupport.WordWidth(fonts, w, 12));

        var lines = IsolatedLineBreaker.Break(paragraph, widest + 1.0, fonts);

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

        var lines = IsolatedLineBreaker.Break(paragraph, 1.0, fonts);

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

        var lines = IsolatedLineBreaker.Break(paragraph, maxWidth, fonts);

        Assert.Equal(2, lines[0].Fragments.Length);
        Assert.Equal("The", lines[0].Fragments[0].Text);
        Assert.Equal("quick", lines[0].Fragments[1].Text);
        Assert.Equal(twoWords, lines[0].Width, 6);

        Assert.Equal("brown", lines[1].Fragments[0].Text);
    }

    [Fact]
    public void WrappedLines_FitTheMeasureAndPreserveEveryWord()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);

        var lines = IsolatedLineBreaker.Break(paragraph, MaxWidth, fonts);

        Assert.True(lines.Count >= 3);
        LineLayoutSupport.AssertFitsAndPreservesWords(fonts, lines, Words, MaxWidth);
    }

    [Fact]
    public void WrappedLines_GroupWordsIntoPinnedLines()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);

        var lines = IsolatedLineBreaker.Break(paragraph, MaxWidth, fonts);

        Assert.Equal(
            new[]
            {
                new[] { "The", "quick", "brown", "fox", "jumps" },
                new[] { "over", "the", "lazy", "dog", "and", "then" },
                new[] { "some", "more", "words", "here" },
            },
            LineLayoutSupport.Grouping(lines));
    }

    [Fact]
    public void NarrowMeasure_GroupsWordsIntoPinnedLines()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);

        var lines = IsolatedLineBreaker.Break(paragraph, 120.0, fonts);

        Assert.Equal(
            new[]
            {
                new[] { "The", "quick", "brown", "fox" },
                new[] { "jumps", "over", "the", "lazy" },
                new[] { "dog", "and", "then", "some" },
                new[] { "more", "words", "here" },
            },
            LineLayoutSupport.Grouping(lines));
    }

    [Fact]
    public void FragmentStartAndLength_IndexIntoRunText()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var run = (Run)paragraph.Inlines[0];

        var lines = IsolatedLineBreaker.Break(paragraph, MaxWidth, fonts);
        var fragments = lines.SelectMany(l => l.Fragments).ToArray();
        var source = fragments[0].Source;

        Assert.Equal(Words.Length, fragments.Length);

        var previousEnd = -1;
        for (var k = 0; k < fragments.Length; k++)
        {
            var fragment = fragments[k];
            Assert.Equal(source, fragment.Source);
            Assert.Equal(Words[k], fragment.Text);
            Assert.Equal(Words[k].Length, fragment.Length);
            Assert.True(fragment.Start > previousEnd);
            Assert.Equal(run.Text.Substring(fragment.Start, fragment.Length), fragment.Text);
            Assert.Equal(LineLayoutSupport.WordWidth(fonts, Words[k], 12), fragment.Advance, 6);
            previousEnd = fragment.Start + fragment.Length;
        }
    }

    [Fact]
    public void LineWidth_EqualsSumOfAdvancesPlusInteriorSpaces()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);

        var lines = IsolatedLineBreaker.Break(paragraph, MaxWidth, fonts);

        foreach (var line in lines)
        {
            var advances = line.Fragments.Sum(f => f.Advance);
            var gaps = (line.Fragments.Length - 1) * space;
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

        var lines = IsolatedLineBreaker.Break(paragraph, max, fonts);

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

        var lines = IsolatedLineBreaker.Break(paragraph, MaxWidth, fonts);

        foreach (var line in lines)
        {
            Assert.Equal(0.0, line.Fragments[0].XOffset, 6);
        }
    }
}
