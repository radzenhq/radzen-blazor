#nullable enable
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// Dot-leader tab stops: a tab stop may carry a Leader char that fills the tab gap with
// repeated leader glyphs (e.g. the dots in "Chapter 1 ...... 5"). A stop with no leader
// (the default) produces no extra fragment, so existing tab layouts are unchanged.
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
        paragraph.TabStops.AddTabStop(Unit.FromPoint(100), TabAlignment.Left, leader: '.');

        var lines = LineBreaker.Break(paragraph, 400, fonts);
        var fragments = lines.SelectMany(l => l.Fragments).ToList();

        var leader = fragments.Single(f => f.Text.Length > 0 && f.Text.All(c => c == '.'));
        var dotWidth = Width(fonts, ".");

        // The leader lives strictly inside the gap between "Left" and the stop at 100.
        Assert.True(leader.XOffset >= Width(fonts, "Left") - Tol, "leader starts after the label");
        Assert.True(leader.XOffset + leader.Advance <= 100 + Tol, "leader ends at or before the stop");
        // It is right-aligned to the stop: the last dot sits within one dot of the stop.
        Assert.True(100 - (leader.XOffset + leader.Advance) < dotWidth + Tol, "leader reaches the stop");
        // The tabbed text still lands on the stop.
        Assert.Equal(100.0, Fragment(lines, "Right").XOffset, Tol);
    }

    [Fact]
    public void NoLeader_ProducesNoDotFragment()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Left\tRight");
        paragraph.TabStops.AddTabStop(Unit.FromPoint(100), TabAlignment.Left);

        var lines = LineBreaker.Break(paragraph, 400, fonts);
        var fragments = lines.SelectMany(l => l.Fragments).ToList();

        Assert.DoesNotContain(fragments, f => f.Text.Length > 0 && f.Text.All(c => c == '.'));
        Assert.Equal(2, fragments.Count);
    }

    [Fact]
    public void RightAlignedLeaderStop_FillsUpToTheNumber()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Chapter\t5");
        paragraph.TabStops.AddTabStop(Unit.FromPoint(200), TabAlignment.Right, leader: '.');

        var lines = LineBreaker.Break(paragraph, 400, fonts);
        var fragments = lines.SelectMany(l => l.Fragments).ToList();

        var number = fragments.Single(f => f.Text == "5");
        var leader = fragments.Single(f => f.Text.Length > 0 && f.Text.All(c => c == '.'));

        Assert.Equal(200.0, number.XOffset + number.Advance, Tol); // right tab
        Assert.True(leader.XOffset + leader.Advance <= number.XOffset + Tol, "dots stop before the number");
    }
}
