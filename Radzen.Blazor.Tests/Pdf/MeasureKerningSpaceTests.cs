#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The public MeasureText must not kern a pair straddling a space: the draw side never does
// (see KerningSpaceBoundaryTests), so a kerned measure would report a width narrower than
// anything that can be drawn.
public class MeasureKerningSpaceTests
{
    private static FontCollection Kerned() => new() { EnableKerning = true };

    private static Font Helvetica => new() { Name = "Helvetica", Size = 12 };

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
