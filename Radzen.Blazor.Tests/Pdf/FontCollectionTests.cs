#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class FontCollectionTests
{
    private static double SfntWidth(byte[] bytes, string text, double size)
    {
        var font = SfntFont.Parse(bytes);
        return SfntWidth(font, text, size);
    }

    private static double SfntWidth(SfntFont font, string text, double size)
    {
        double units = 0;
        foreach (var c in text)
        {
            units += font.GetAdvanceWidth(font.GetGlyphId(c));
        }

        return units * size / font.UnitsPerEm;
    }

    [Fact]
    public void Base14_ResolvesWithoutRegistration()
    {
        var fonts = new FontCollection();
        var font = new Font { Family = "Helvetica", Size = 12 };

        var expected = BuiltInFontMetrics.Resolve(font)!.MeasureString("Hello", 12);
        Assert.Equal(expected, fonts.MeasureText("Hello", font), 10);
    }

    [Fact]
    public void Base14_DefaultFontNameIsMeasurable()
    {
        var fonts = new FontCollection();
        var font = new Font { Size = 12 };

        Assert.True(fonts.MeasureText("Radzen", font) > 0);
    }

    [Fact]
    public void Base14_TimesBoldSelectsBoldVariant()
    {
        var fonts = new FontCollection();
        var plain = new Font { Family = "Times", Size = 12 };
        var bold = new Font { Family = "Times", Size = 12, Bold = true };

        var expectedPlain = BuiltInFontMetrics.Resolve(plain)!.MeasureString("Hello", 12);
        var expectedBold = BuiltInFontMetrics.Resolve(bold)!.MeasureString("Hello", 12);

        Assert.Equal(expectedPlain, fonts.MeasureText("Hello", plain), 10);
        Assert.Equal(expectedBold, fonts.MeasureText("Hello", bold), 10);
        Assert.NotEqual(fonts.MeasureText("Hello", plain), fonts.MeasureText("Hello", bold));
    }

    [Fact]
    public void UnknownFamily_MeasureThrowsWithNameInMessage()
    {
        var fonts = new FontCollection();
        var font = new Font { Family = "Nonexistent Font", Size = 12 };

        var ex = Assert.Throws<InvalidOperationException>(() => fonts.MeasureText("Hello", font));
        Assert.Contains("Nonexistent Font", ex.Message);
    }

    [Fact]
    public void RegisteredTtf_MeasureMatchesSfntAdvances()
    {
        var bytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var fonts = new FontCollection();
        using (var stream = new MemoryStream(bytes))
        {
            fonts.Register("Liberation Sans", stream);
        }

        var font = new Font { Family = "Liberation Sans", Size = 12 };
        Assert.Equal(SfntWidth(bytes, "Hello", 12), fonts.MeasureText("Hello", font), 10);
    }

    [Fact]
    public void Register_BuffersStream_ClosedStreamStillMeasurable()
    {
        var bytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var fonts = new FontCollection();
        var stream = new MemoryStream(bytes);
        fonts.Register("Liberation Sans", stream);
        stream.Dispose();

        var font = new Font { Family = "Liberation Sans", Size = 12 };
        Assert.Equal(SfntWidth(bytes, "Hello", 12), fonts.MeasureText("Hello", font), 10);
    }

    [Fact]
    public void RegisteredBoldFace_UsedByItsOwnName()
    {
        var regular = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var boldBytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf");
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(regular));
        fonts.Register("Liberation Sans Bold", new MemoryStream(boldBytes));

        var boldFont = new Font { Family = "Liberation Sans Bold", Size = 12 };
        Assert.Equal(SfntWidth(boldBytes, "Hello", 12), fonts.MeasureText("Hello", boldFont), 10);

        var regularBold = new Font { Family = "Liberation Sans", Size = 12, Bold = true };
        Assert.Equal(SfntWidth(regular, "Hello", 12), fonts.MeasureText("Hello", regularBold), 10);
        Assert.NotEqual(fonts.MeasureText("Hello", boldFont), fonts.MeasureText("Hello", regularBold));
    }

    [Fact]
    public void RegisterTwice_LastWins()
    {
        var sans = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var serif = PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");
        var fonts = new FontCollection();
        fonts.Register("Custom", new MemoryStream(sans));
        fonts.Register("Custom", new MemoryStream(serif));

        var font = new Font { Family = "Custom", Size = 12 };
        Assert.Equal(SfntWidth(serif, "Hello", 12), fonts.MeasureText("Hello", font), 10);
    }

    [Fact]
    public void RegisterTtc_SelectsFaceMatchingFamily()
    {
        var ttc = PdfTestResources.ReadAllBytes("Fonts/Liberation-Subset.ttc");
        var fonts = new FontCollection();
        fonts.Register("Liberation Serif", new MemoryStream(ttc));

        var serifFace = SfntFont.Parse(ttc, "Liberation Serif");
        var font = new Font { Family = "Liberation Serif", Size = 12 };
        Assert.Equal(SfntWidth(serifFace, "Hello", 12), fonts.MeasureText("Hello", font), 10);

        var sans = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        Assert.NotEqual(SfntWidth(sans, "Hello", 12), fonts.MeasureText("Hello", font));
    }

    [Fact]
    public void RegisterTtc_NoMatchingFaceThrows()
    {
        var ttc = PdfTestResources.ReadAllBytes("Fonts/Liberation-Subset.ttc");
        var fonts = new FontCollection();

        var ex = Assert.ThrowsAny<Exception>(() => fonts.Register("Comic Sans", new MemoryStream(ttc)));
        Assert.Contains("Comic Sans", ex.Message);
    }

    [Fact]
    public void Register_GarbageStream_Throws()
    {
        var fonts = new FontCollection();
        Assert.ThrowsAny<Exception>(() => fonts.Register("Junk", new MemoryStream(new byte[64])));
    }
}
