#nullable enable
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class WhitespaceMeasurementTests
{
    [Fact]
    public void MultiSpaceGap_MeasuresAsCountTimesSingleSpaceWidth()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("a        b");
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var a = LineLayoutSupport.WordWidth(fonts, "a", 12);
        var b = LineLayoutSupport.WordWidth(fonts, "b", 12);

        var lines = IsolatedLineBreaker.Break(paragraph, 1000.0, fonts);

        var line = Assert.Single(lines);
        Assert.Equal(a + (8 * space) + b, line.Width, 6);
    }

    [Fact]
    public void GapWidth_ScalesLinearlyWithSpaceCount()
    {
        var fonts = LineLayoutSupport.Fonts();
        var one = IsolatedLineBreaker.Break(LineLayoutSupport.SingleRun("a b"), 1000.0, fonts);
        var five = IsolatedLineBreaker.Break(LineLayoutSupport.SingleRun("a     b"), 1000.0, fonts);
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);

        Assert.Equal(one[0].Width + (4 * space), five[0].Width, 6);
    }
}
