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

public class TabStopTests
{
    private const double Tol = 0.5;

    private static Paragraph TwoRuns(string text)
        => LineLayoutSupport.SingleRun(text);

    private static double Width(FontCollection fonts, string text)
        => fonts.MeasureText(text, LineLayoutSupport.FontAt(12));

    private static LineFragment Fragment(IReadOnlyList<LineBox> lines, string text)
        => lines.SelectMany(l => l.Fragments).Single(f => f.Text == text);

    [Fact]
    public void LeftTabStop_PlacesFollowingRunAtStop()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = TwoRuns("Left\tRight");
        paragraph.TabStops.Add(Unit.FromPoint(100), TabAlignment.Left);

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);

        Assert.Equal(0.0, Fragment(lines, "Left").XOffset, Tol);
        Assert.Equal(100.0, Fragment(lines, "Right").XOffset, Tol);
    }

    [Fact]
    public void RightTabStop_RightAlignsRunEndingAtStop()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = TwoRuns("Left\tRight");
        paragraph.TabStops.Add(Unit.FromPoint(200), TabAlignment.Right);

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);
        var right = Fragment(lines, "Right");

        Assert.Equal(200.0, right.XOffset + right.Advance, Tol);
        Assert.Equal(200.0 - Width(fonts, "Right"), right.XOffset, Tol);
    }

    [Fact]
    public void CenterTabStop_CentersRunOnStop()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = TwoRuns("Left\tRight");
        paragraph.TabStops.Add(Unit.FromPoint(150), TabAlignment.Center);

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);
        var right = Fragment(lines, "Right");

        Assert.Equal(150.0 - (Width(fonts, "Right") / 2.0), right.XOffset, Tol);
    }

    [Fact]
    public void DecimalTabStop_AlignsDecimalSeparatorAtStop()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = TwoRuns("Item\t12.34");
        paragraph.TabStops.Add(Unit.FromPoint(120), TabAlignment.Decimal);

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);
        var value = Fragment(lines, "12.34");

        Assert.Equal(120.0, value.XOffset + Width(fonts, "12"), Tol);
    }

    [Fact]
    public void TabStop_AdvancesToNextStopBeyondCursor()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = TwoRuns("LongishLabel\tX");
        paragraph.TabStops.Add(Unit.FromPoint(10), TabAlignment.Left);
        paragraph.TabStops.Add(Unit.FromPoint(140), TabAlignment.Left);

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);

        Assert.True(Width(fonts, "LongishLabel") > 10,
            "test relies on the label overrunning the first stop");
        Assert.Equal(140.0, Fragment(lines, "X").XOffset, Tol);
    }

    [Fact]
    public void NoTabStops_UsesDefault36ptIncrements()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = TwoRuns("Left\tRight");

        var lines = IsolatedLineBreaker.Break(paragraph, 400, fonts);

        Assert.True(Width(fonts, "Left") < 36, "test relies on the label fitting the first 36pt cell");
        Assert.Equal(36.0, Fragment(lines, "Right").XOffset, Tol);
    }
}
