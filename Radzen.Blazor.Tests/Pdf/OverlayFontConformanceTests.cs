#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class OverlayFontConformanceTests
{
    private static byte[] Png() => PdfTestResources.ReadAllBytes("Images/rgb.png");

    private static Radzen.Documents.Pdf.Document RenderAuthored((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.Render(authored.Document);

    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance conformance)
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer { Conformance = conformance };
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);
        return (document, builderRenderer);
    }

    [Fact]
    public void PdfA3B_OverlayBase14Text_ThrowsEmbeddableFontError()
    {
        var document = RenderAuthored(Author(PdfAConformance.PdfA3B));
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
        var document = RenderAuthored(Author(PdfAConformance.PdfA3B));
        document.Pages[0].Content.Add(new ImageContent(Png()) { Bounds = PdfRect.FromSize(72, 72, 96, 48) });

        Assert.NotEmpty(document.ToArray());
    }

    [Fact]
    public void None_OverlayBase14Text_SavesWithoutError()
    {
        var document = RenderAuthored(Author(PdfAConformance.None));
        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        Assert.NotEmpty(document.ToArray());
    }
}
