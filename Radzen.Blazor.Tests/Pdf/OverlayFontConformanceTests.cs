#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A user can stamp overlay content onto a built page via Page.Content. Overlay
// TextContent always emits a non-embedded base-14 Type1 font, which PDF/A forbids.
// SaveToStream must reject such an overlay on a conforming document with the same
// embeddable-font error the generator raises, while leaving image-only overlays and
// non-conforming documents untouched.
public class OverlayFontConformanceTests
{
    private static byte[] Png() => PdfTestResources.ReadAllBytes("Images/rgb.png");

    private static DocumentBuilder Author(PdfAConformance conformance)
    {
        var builder = new DocumentBuilder { Conformance = conformance };
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);
        return builder;
    }

    [Fact]
    public void PdfA3B_OverlayBase14Text_ThrowsEmbeddableFontError()
    {
        var document = Author(PdfAConformance.PdfA3B).Build();
        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        var exception = Record.Exception(() => document.ToArray());

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PDF/A", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", exception.Message, StringComparison.Ordinal);
        Assert.Contains("embeddable font file", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA3B_OverlayImageOnly_SavesWithoutError()
    {
        var document = Author(PdfAConformance.PdfA3B).Build();
        document.Pages[0].Content.Add(new ImageContent(Png()) { Bounds = PdfRect.FromSize(72, 72, 96, 48) });

        Assert.NotEmpty(document.ToArray());
    }

    [Fact]
    public void None_OverlayBase14Text_SavesWithoutError()
    {
        var document = Author(PdfAConformance.None).Build();
        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        Assert.NotEmpty(document.ToArray());
    }
}
