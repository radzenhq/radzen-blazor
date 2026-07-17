#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Pins the constraint that keeps TextLineEmitter.EmitBase14Fragment out of SfntRunBuilder:
// SfntRunBuilder maps text through FontCollection.Shaper(), and the shaper resolves a PRIMARY
// sfnt face up front. A base-14 family has no primary face, so shaping one throws - the base-14
// path is not the sfnt path with a different argument. If this test ever stops throwing, the
// two draw sites are mergeable and should be merged.
public class SfntRunBuilderBase14BoundaryTests
{
    [Fact]
    public void Shaper_Base14Family_Throws()
    {
        var fonts = new FontCollection();
        var helvetica = new Font { Name = "Helvetica", Size = 12 };

        // Base-14 measures fine: the base-14 metrics path serves it.
        Assert.True(fonts.MeasureText("Hi", helvetica) > 0);

        // The sfnt shaping seam SfntRunBuilder is built on does not.
        var ex = Assert.Throws<InvalidOperationException>(() => fonts.Shaper().Shape("Hi", helvetica, out _));
        Assert.Contains("Helvetica", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shaper_RegisteredFamily_Shapes()
    {
        var fonts = LineLayoutSupport.Fonts();

        var glyphs = fonts.Shaper().Shape("Hi", LineLayoutSupport.FontAt(12), out _);

        Assert.Equal(2, glyphs.Count);
    }
}
