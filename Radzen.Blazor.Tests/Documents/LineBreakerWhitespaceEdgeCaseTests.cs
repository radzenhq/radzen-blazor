#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class LineBreakerWhitespaceEdgeCaseTests
{
    [Fact]
    public void OverWideCenteredWord_DoesNotShiftLeftOfMargin()
    {
        var fonts = LineLayoutSupport.Fonts();
        var word = "Supercalifragilisticexpialidocious" + "\u00A0" + "Supercalifragilisticexpialidocious";
        var paragraph = LineLayoutSupport.SingleRun(word, alignment: HorizontalAlignment.Center);

        var lines = IsolatedLineBreaker.Break(paragraph, 50.0, fonts);

        foreach (var frag in lines.SelectMany(l => l.Fragments))
        {
            Assert.True(frag.XOffset >= -1e-6, $"fragment at {frag.XOffset} shifted left of the margin");
        }
    }

    [Fact]
    public void LeadingSpaces_AdvanceFollowingWord()
    {
        var fonts = LineLayoutSupport.Fonts();
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var paragraph = LineLayoutSupport.SingleRun("  word");

        var line = Assert.Single(IsolatedLineBreaker.Break(paragraph, 1000.0, fonts));
        var frag = Assert.Single(line.Fragments);

        Assert.Equal("word", frag.Text);
        Assert.Equal(2 * space, frag.XOffset, 6);
    }
}
