#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class TabStopWrapTests
{
    private const double Tol = 0.5;

    private const string Sentence =
        "The quick brown fox jumps over the lazy dog and then some more words here today";

    private static double ContentWidth
        => PageSizes.A4.Width.Point - (2 * 36.0);

    [Fact]
    public void ExplicitTabStop_WrapsPostStopSegment_WithinContentWidth()
    {
        var fonts = LineLayoutSupport.Fonts();
        var max = ContentWidth;
        var paragraph = LineLayoutSupport.SingleRun(
            "Label:\tInternational Business Machines Corporation Global Services Division");
        paragraph.TabStops.Add(Unit.FromPoint(455), TabAlignment.Left);

        var lines = IsolatedLineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1, "post-stop segment must wrap onto more than one line");
        foreach (var fragment in lines.SelectMany(l => l.Fragments))
        {
            Assert.True(fragment.XOffset + fragment.Advance <= max + Tol,
                $"fragment '{fragment.Text}' at {fragment.XOffset} overruns the {max}pt measure");
        }

        Assert.Equal(
            new[]
            {
                new[] { "Label:", "International" },
                new[] { "Business", "Machines", "Corporation", "Global", "Services", "Division" },
            },
            LineLayoutSupport.Grouping(lines));

        Assert.Equal(0.0, lines[0].Fragments[0].XOffset, Tol);
        Assert.Equal(455.0, lines[0].Fragments[1].XOffset, Tol);
        Assert.Equal(0.0, lines[1].Fragments[0].XOffset, Tol);
    }

    [Fact]
    public void TabbedRun_ResumesAtLeftMarginOnTheContinuationLine()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Name:\tAtanas Korchev of Radzen Ltd Bulgaria");
        paragraph.TabStops.Add(Unit.FromPoint(80), TabAlignment.Left);

        var lines = IsolatedLineBreaker.Break(paragraph, 220.0, fonts);

        Assert.Equal(
            new[]
            {
                new[] { "Name:", "Atanas", "Korchev", "of" },
                new[] { "Radzen", "Ltd", "Bulgaria" },
            },
            LineLayoutSupport.Grouping(lines));

        Assert.Equal(80.0, lines[0].Fragments[1].XOffset, Tol);
        Assert.Equal(0.0, lines[1].Fragments[0].XOffset, Tol);
        foreach (var fragment in lines.SelectMany(l => l.Fragments))
        {
            Assert.True(fragment.XOffset + fragment.Advance <= 220.0 + Tol,
                $"fragment '{fragment.Text}' at {fragment.XOffset} overruns the 220pt measure");
        }
    }

    [Fact]
    public void NoExplicitTabStops_WrapsGreedilyWithoutSplittingWords()
    {
        var fonts = LineLayoutSupport.Fonts();
        var max = 200.0;
        var paragraph = LineLayoutSupport.SingleRun(Sentence);

        var lines = IsolatedLineBreaker.Break(paragraph, max, fonts);

        LineLayoutSupport.AssertFitsAndPreservesWords(fonts, lines, Sentence.Split(' '), max);
    }

    [Fact]
    public void NoExplicitTabStops_GroupsWordsIntoPinnedLines()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Sentence);

        var lines = IsolatedLineBreaker.Break(paragraph, 200.0, fonts);

        Assert.Equal(
            new[]
            {
                new[] { "The", "quick", "brown", "fox", "jumps", "over", "the" },
                new[] { "lazy", "dog", "and", "then", "some", "more", "words" },
                new[] { "here", "today" },
            },
            LineLayoutSupport.Grouping(lines));
    }
}
