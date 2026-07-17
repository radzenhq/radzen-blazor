#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The style fallback (no exact (family, bold, italic) face, no regular face) picks a face by
// walking the registrations. Which face it lands on decides which bytes get embedded, so the
// walk order is part of the contract rather than an artifact of the backing dictionary.
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

        // Neither an exact (regular, non-italic) face nor a regular face is registered, so a
        // plain request falls back. Bold was registered first, so bold is the answer.
        var fonts = Collection((true, false, bold), (false, true, serif));
        var plain = new Font { Name = "Fallback Family", Size = 12 };

        Assert.Equal(Width(bold, "Hello", 12), fonts.MeasureText("Hello", plain), 10);
    }

    [Fact]
    public void StyleFallbackFollowsRegistrationOrderNotStyle()
    {
        var bold = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

        // The same two faces registered the other way round: the answer tracks registration
        // order, so it is the caller's choice rather than the dictionary's enumeration.
        var fonts = Collection((false, true, serif), (true, false, bold));
        var plain = new Font { Name = "Fallback Family", Size = 12 };

        Assert.Equal(Width(serif, "Hello", 12), fonts.MeasureText("Hello", plain), 10);
    }

    [Fact]
    public void ReRegisteringAFaceKeepsItsOriginalFallbackPosition()
    {
        var bold = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

        var fonts = Collection((true, false, bold), (false, true, serif), (true, false, bold));
        var plain = new Font { Name = "Fallback Family", Size = 12 };

        // Overwriting the bold face does not move it behind the serif face: last registration wins on
        // value, first registration wins on order.
        Assert.Equal(Width(bold, "Hello", 12), fonts.MeasureText("Hello", plain), 10);
    }

    [Fact]
    public void ExactMatchIsUnaffectedByRegistrationOrder()
    {
        var bold = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

        // An exact (family, bold, italic) hit never consults the fallback walk, so it answers
        // the same face whichever order the two were registered in.
        var boldFirst = Collection((true, false, bold), (false, true, serif));
        var italicFirst = Collection((false, true, serif), (true, false, bold));
        var request = new Font { Name = "Fallback Family", Size = 12, Italic = true };

        Assert.Equal(Width(serif, "Hello", 12), boldFirst.MeasureText("Hello", request), 10);
        Assert.Equal(Width(serif, "Hello", 12), italicFirst.MeasureText("Hello", request), 10);
    }
}
