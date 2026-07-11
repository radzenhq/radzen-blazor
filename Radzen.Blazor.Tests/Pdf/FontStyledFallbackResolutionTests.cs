#nullable enable
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// When a family is registered with only a styled face (e.g. bold), regular-weight text
// in that family must resolve to the available face instead of throwing. An exact-style
// face, when present, still wins.
public class FontStyledFallbackResolutionTests
{
    private static double SfntWidth(byte[] bytes, string text, double size)
    {
        var font = SfntFont.Parse(bytes);
        double units = 0;
        foreach (var c in text)
        {
            units += font.GetAdvanceWidth(font.GetGlyphId(c));
        }

        return units * size / font.UnitsPerEm;
    }

    [Fact]
    public void OnlyBoldRegistered_RegularTextResolvesToBoldFace()
    {
        var boldBytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(boldBytes), bold: true, italic: false);

        var regular = new Font { Name = "Liberation Sans", Size = 12 };
        Assert.Equal(SfntWidth(boldBytes, "Hello", 12), fonts.MeasureText("Hello", regular), 10);
    }

    [Fact]
    public void OnlyBoldItalicRegistered_RegularTextResolvesToStyledFace()
    {
        var styledBytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-BoldItalic.ttf");
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(styledBytes), bold: true, italic: true);

        var regular = new Font { Name = "Liberation Sans", Size = 12 };
        Assert.Equal(SfntWidth(styledBytes, "Hello", 12), fonts.MeasureText("Hello", regular), 10);
    }

    [Fact]
    public void ExactRegularFace_PreferredOverStyledFace()
    {
        var regularBytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var boldBytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(regularBytes), bold: false, italic: false);
        fonts.Register("Liberation Sans", new MemoryStream(boldBytes), bold: true, italic: false);

        var regular = new Font { Name = "Liberation Sans", Size = 12 };
        Assert.Equal(SfntWidth(regularBytes, "Hello", 12), fonts.MeasureText("Hello", regular), 10);

        var bold = new Font { Name = "Liberation Sans", Size = 12, Bold = true };
        Assert.Equal(SfntWidth(boldBytes, "Hello", 12), fonts.MeasureText("Hello", bold), 10);
    }
}
