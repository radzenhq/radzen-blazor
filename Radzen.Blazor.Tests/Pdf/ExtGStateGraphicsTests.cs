#nullable enable

using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.4.5, 11.3.7: /BM, /OP /op /OPM and /RI entries in an ExtGState parameter dictionary.
public class ExtGStateGraphicsTests
{
    private static string Name(DocumentObject o) => Assert.IsType<NameObject>(o).Value;

    [Fact]
    public void AlphaOnly_ProducesTypeCaCaInOrder()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(0.5, 0.5);

        Assert.Equal(new[] { "Type", "ca", "CA" }, dict.Keys);
        Assert.Equal("ExtGState", Name(dict["Type"]!));
    }

    [Fact]
    public void BlendMode_EmitsBmName()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(1, 1, blend: BlendMode.Multiply);

        Assert.Equal("Multiply", Name(dict["BM"]!));
    }

    [Fact]
    public void NonSeparableBlendMode_EmitsSpecName()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(1, 1, blend: BlendMode.Luminosity);

        Assert.Equal("Luminosity", Name(dict["BM"]!));
    }

    [Fact]
    public void Overprint_EmitsOpOpAndOpm()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(
            1, 1, overprintStroke: true, overprintFill: false, overprintMode: 1);

        Assert.True(Assert.IsType<BooleanObject>(dict["OP"]!).Value);
        Assert.False(Assert.IsType<BooleanObject>(dict["op"]!).Value);
        Assert.Equal(1, Assert.IsType<NumberObject>(dict["OPM"]!).IntValue);
    }

    [Fact]
    public void RenderingIntent_EmitsRiName()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(1, 1, intent: RenderingIntent.Perceptual);

        Assert.Equal("Perceptual", Name(dict["RI"]!));
    }

    [Fact]
    public void AlphaOnly_OmitsAllOptionalKeys()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(0.25, 0.75);

        Assert.False(dict.ContainsKey("BM"));
        Assert.False(dict.ContainsKey("OP"));
        Assert.False(dict.ContainsKey("op"));
        Assert.False(dict.ContainsKey("OPM"));
        Assert.False(dict.ContainsKey("RI"));
    }
}
