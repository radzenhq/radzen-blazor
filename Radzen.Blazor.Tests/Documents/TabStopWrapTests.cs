#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class TabStopWrapTests
{
    private const double Tol = 0.5;

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

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1, "post-stop segment must wrap onto more than one line");
        foreach (var fragment in lines.SelectMany(l => l.Fragments))
        {
            Assert.True(fragment.XOffset + fragment.Advance <= max + Tol,
                $"fragment '{fragment.Text}' at {fragment.XOffset} overruns the {max}pt measure");
        }
    }

    [Fact]
    public void NoExplicitTabStops_WrapsOnDefaultGrid_Unchanged()
    {
        var fonts = LineLayoutSupport.Fonts();
        const string sentence =
            "The quick brown fox jumps over the lazy dog and then some more words here today";
        var words = sentence.Split(' ');
        var max = 200.0;
        var paragraph = LineLayoutSupport.SingleRun(sentence);

        var lines = LineBreaker.Break(paragraph, max, fonts);

        var widths = words.Select(w => LineLayoutSupport.WordWidth(fonts, w, 12)).ToArray();
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var expected = LineLayoutSupport.Wrap(widths, space, max);

        Assert.Equal(expected.Count, lines.Count);
        var w = 0;
        for (var li = 0; li < expected.Count; li++)
        {
            var (first, last) = expected[li];
            var expectedWords = last - first + 1;
            Assert.Equal(expectedWords, lines[li].Fragments.Length);
            for (var f = 0; f < expectedWords; f++)
            {
                Assert.Equal(words[w + f], lines[li].Fragments[f].Text);
            }

            w += expectedWords;
        }
    }
}
