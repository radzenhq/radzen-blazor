#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class TabLeaderTests
{
    private const double Tol = 0.5;

    private static double Width(FontCollection fonts, string text)
        => fonts.MeasureText(text, LineLayoutSupport.FontAt(12));

    private static LineFragment Fragment(IReadOnlyList<LineBox> lines, string text)
        => lines.SelectMany(l => l.Fragments).Single(f => f.Text == text);

    [Fact]
    public void TabStop_DefaultHasNoLeader()
    {
        var stop = new TabStop(Unit.FromPoint(100));
        Assert.Equal('\0', stop.Leader);
    }

    [Fact]
    public void LeaderTabStop_FillsGapWithLeaderChar()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Left\tRight");
        paragraph.TabStops.Add(Unit.FromPoint(100), TabAlignment.Left, leader: '.');

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);
        var fragments = lines.SelectMany(l => l.Fragments).ToList();

        var leader = fragments.Single(f => f.Text.Length > 0 && f.Text.All(c => c == '.'));
        var dotWidth = Width(fonts, ".");

        Assert.True(leader.XOffset >= Width(fonts, "Left") - Tol, "leader starts after the label");
        Assert.True(leader.XOffset + leader.Advance <= 100 + Tol, "leader ends at or before the stop");
        Assert.True(100 - (leader.XOffset + leader.Advance) < dotWidth + Tol, "leader reaches the stop");
        Assert.Equal(100.0, Fragment(lines, "Right").XOffset, Tol);
    }

    [Fact]
    public void NoLeader_ProducesNoDotFragment()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Left\tRight");
        paragraph.TabStops.Add(Unit.FromPoint(100), TabAlignment.Left);

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);
        var fragments = lines.SelectMany(l => l.Fragments).ToList();

        Assert.DoesNotContain(fragments, f => f.Text.Length > 0 && f.Text.All(c => c == '.'));
        Assert.Equal(2, fragments.Count);
    }

    [Fact]
    public void RightAlignedLeaderStop_FillsUpToTheNumber()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Chapter\t5");
        paragraph.TabStops.Add(Unit.FromPoint(200), TabAlignment.Right, leader: '.');

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);
        var fragments = lines.SelectMany(l => l.Fragments).ToList();

        var number = fragments.Single(f => f.Text == "5");
        var leader = fragments.Single(f => f.Text.Length > 0 && f.Text.All(c => c == '.'));

        Assert.Equal(200.0, number.XOffset + number.Advance, Tol);
        Assert.True(leader.XOffset + leader.Advance <= number.XOffset + Tol, "dots stop before the number");
    }
}
