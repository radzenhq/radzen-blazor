#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Fonts.Sfnt;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class FontCollectionFallbackOrderTests
{
    private static double Width(byte[] bytes, string text, double size)
    {
        var font = SfntFont.Parse(bytes);
        double units = 0;
        foreach (var c in text)
        {
            units += font.GetAdvanceWidth(font.GetGlyphId(c));
        }

        return units * size / font.UnitsPerEm;
    }

    private static FontCollection Collection(params (bool Bold, bool Italic, byte[] Bytes)[] faces)
    {
        var fonts = new FontCollection();
        foreach (var (bold, italic, bytes) in faces)
        {
            fonts.Register("Fallback Family", new MemoryStream(bytes), bold, italic);
        }

        return fonts;
    }

    [Fact]
    public void StyleFallbackPicksTheFirstRegisteredFaceOfTheFamily()
    {
        var bold = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

        var fonts = Collection((true, false, bold), (false, true, serif));
        var plain = new Font { Family = "Fallback Family", Size = 12 };

        Assert.Equal(Width(bold, "Hello", 12), fonts.MeasureText("Hello", plain), 10);
    }

    [Fact]
    public void StyleFallbackFollowsRegistrationOrderNotStyle()
    {
        var bold = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

        var fonts = Collection((false, true, serif), (true, false, bold));
        var plain = new Font { Family = "Fallback Family", Size = 12 };

        Assert.Equal(Width(serif, "Hello", 12), fonts.MeasureText("Hello", plain), 10);
    }

    [Fact]
    public void ReRegisteringAFaceKeepsItsOriginalFallbackPosition()
    {
        var bold = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

        var fonts = Collection((true, false, bold), (false, true, serif), (true, false, bold));
        var plain = new Font { Family = "Fallback Family", Size = 12 };

        Assert.Equal(Width(bold, "Hello", 12), fonts.MeasureText("Hello", plain), 10);
    }

    [Fact]
    public void ExactMatchIsUnaffectedByRegistrationOrder()
    {
        var bold = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

        var boldFirst = Collection((true, false, bold), (false, true, serif));
        var italicFirst = Collection((false, true, serif), (true, false, bold));
        var request = new Font { Family = "Fallback Family", Size = 12, Italic = true };

        Assert.Equal(Width(serif, "Hello", 12), boldFirst.MeasureText("Hello", request), 10);
        Assert.Equal(Width(serif, "Hello", 12), italicFirst.MeasureText("Hello", request), 10);
    }
}
