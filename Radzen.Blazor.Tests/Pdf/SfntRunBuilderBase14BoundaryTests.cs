#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class SfntRunBuilderBase14BoundaryTests
{
    [Fact]
    public void Shaper_Base14Family_Throws()
    {
        var fonts = new FontCollection();
        var helvetica = new Font { Name = "Helvetica", Size = 12 };

        Assert.True(fonts.MeasureText("Hi", helvetica) > 0);

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
