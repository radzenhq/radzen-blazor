#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Write;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class SaveTimeConformanceTests
{
    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance conformance)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Save time conformance";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello conformance", BuildTestSupport.Latin);

        return (document, new DocumentRenderer { Conformance = conformance });
    }

    private static void Attach(PortableDocument document)
        => document.Attachments.Add(
            "data.xml", Encoding.UTF8.GetBytes("<data/>"), AttachmentRelationship.Data, "text/xml");

    private static string MetadataPacket(string emission)
    {
        var reference = Shaped("catalog", @"/Metadata (\d+) 0 R", Line(emission, "/Type /Catalog"));
        return IndirectObject(emission, reference.Groups[1].Value);
    }

    private static PortableDocument LoadedDocument()
    {
        var document = new PortableDocument();
        document.Info.Title = "Original title";
        document.Pages.Add(PageSizes.A4);

        using var stream = new MemoryStream(document.ToArray());
        return PortableDocument.LoadFromStream(stream);
    }

    [Fact]
    public void PdfA4F_OnTheRenderer_RendersAndSavesWithAnAttachmentAddedAfterRender()
    {
        var (document, renderer) = Author(PdfAConformance.PdfA4F);

        var rendered = renderer.Render(document);
        Attach(rendered);

        var packet = MetadataPacket(Emit(rendered));
        Carries("metadata packet", "<pdfaid:part>4</pdfaid:part>", packet);
        Carries("metadata packet", "<pdfaid:conformance>F</pdfaid:conformance>", packet);
    }

    [Fact]
    public void PdfA4F_WithoutAnAttachment_RendersAndFailsAtSave()
    {
        var (document, renderer) = Author(PdfAConformance.PdfA4F);

        var rendered = renderer.Render(document);

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);
        Assert.Contains("PDF/A-4F requires at least one embedded file", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA4F_WithACachedGraphThatPredatesTheViolation_StillFailsAtSave()
    {
        var (document, renderer) = Author(PdfAConformance.PdfA4);
        var rendered = renderer.Render(document);
        rendered.Conformance = PdfAConformance.PdfA4F;
        rendered.MaterializedGraph = new DocumentGraphBuilder(rendered, renderTime: true).Build();

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);
        Assert.Contains("PDF/A-4F requires at least one embedded file", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B)]
    [InlineData(PdfAConformance.PdfA2A)]
    [InlineData(PdfAConformance.PdfA4)]
    [InlineData(PdfAConformance.PdfA4E)]
    public void AttachmentAddedAfterRender_FailsAtSaveForTheProfilesThatForbidIt(PdfAConformance level)
    {
        var (document, renderer) = Author(level);
        var rendered = renderer.Render(document);

        Attach(rendered);

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);
        Assert.Contains(level.ToString(), error.Message, StringComparison.Ordinal);
        Assert.Contains("only permits embedded files", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EncryptionSetAfterRender_FailsAtSaveNamingThePortableDocumentProperty()
    {
        var (document, renderer) = Author(PdfAConformance.PdfA3B);
        var rendered = renderer.Render(document);
        rendered.Encryption = new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = EncryptionAlgorithm.Aes128,
            UserPassword = "s3cret",
        };

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);
        Assert.Contains("clear PortableDocument.Encryption", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("DocumentRenderer.Encryption", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IncrementalSave_ViolatingASaveTimeRule_Fails()
    {
        var loaded = LoadedDocument();
        loaded.Conformance = PdfAConformance.PdfA4F;
        loaded.Info.Title = "Edited title";

        using var stream = new MemoryStream();
        var error = Assert.Throws<InvalidOperationException>(() => loaded.SaveIncremental(stream));

        Assert.Contains("PDF/A-4F requires at least one embedded file", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void IncrementalSave_WithoutAConformanceClaim_StillSavesTheSameEdit()
    {
        var loaded = LoadedDocument();
        loaded.Info.Title = "Edited title";

        using var stream = new MemoryStream();
        loaded.SaveIncremental(stream);

        Assert.NotEqual(0, stream.Length);
    }

    [Fact]
    public void FacturXProfile_CompletedAfterRender_IsCheckedAtSave()
    {
        var (document, renderer) = Author(PdfAConformance.PdfA3B);
        var rendered = renderer.Render(document);
        var attachment = rendered.Attachments.Add(
            "factur-x.xml",
            Encoding.UTF8.GetBytes("<invoice/>"),
            AttachmentRelationship.Data,
            "text/xml");
        attachment.FacturX = new FacturXProfile { Version = "" };

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);
        Assert.Contains("Attachment.FacturX requires DocumentType, Version and ConformanceLevel", error.Message, StringComparison.Ordinal);
    }
}
