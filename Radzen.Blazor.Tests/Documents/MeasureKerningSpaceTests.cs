#nullable enable
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Documents.Tests;

public class MeasureKerningSpaceTests
{
    private static FontCollection Kerned() => new() { EnableKerning = true };

    private static Font Helvetica => new() { Family = "Helvetica", Size = 12 };

    [Fact]
    public void MeasureText_DoesNotKernAcrossSpace()
    {
        var fonts = Kerned();
        var whole = fonts.MeasureText("A V", Helvetica);
        var parts = fonts.MeasureText("A", Helvetica)
            + fonts.MeasureText(" ", Helvetica)
            + fonts.MeasureText("V", Helvetica);

        Assert.Equal(parts, whole, 6);
    }

    [Fact]
    public void MeasureText_StillKernsWithinAWord()
    {
        var fonts = Kerned();
        var kerned = fonts.MeasureText("AV", Helvetica);
        var unkerned = fonts.MeasureText("A", Helvetica) + fonts.MeasureText("V", Helvetica);

        Assert.True(kerned < unkerned, "an unspaced AV pair is kerned tighter than its parts");
    }
}
