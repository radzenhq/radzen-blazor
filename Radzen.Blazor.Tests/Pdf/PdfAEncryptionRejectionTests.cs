#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;
using Radzen.Documents;

using Radzen.Documents.Pdf.Objects;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfAEncryptionRejectionTests
{
    private static DocumentReader ReadAuthored((Document Document, DocumentRenderer Renderer) authored)
        => BuildTestSupport.Read(authored.Document, authored.Renderer);

    private static byte[] RenderAuthored((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance conformance)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        document.Info.Title = "PDF/A Invoice";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello PDF/A", BuildTestSupport.Latin);

        var builderRenderer = new DocumentRenderer();
        builderRenderer.Conformance = conformance;
        return (document, builderRenderer);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B)]
    [InlineData(PdfAConformance.PdfA3B)]
    [InlineData(PdfAConformance.PdfA4)]
    public void PdfA_WithEncryption_ThrowsActionable(PdfAConformance conformance)
    {
        var (document, builderRenderer) = Author(conformance);
        builderRenderer.Encryption = new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = EncryptionAlgorithm.Aes128,
            UserPassword = "s3cret",
        };

        var exception = Record.Exception(() => builderRenderer.ToArray(document));

        Assert.True(exception is not null, "PDF/A output must not be encrypted");
        Assert.Contains("PDF/A forbids encryption", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Encryption", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA_WithoutEncryption_Succeeds()
    {
        Assert.NotEmpty(RenderAuthored(Author(PdfAConformance.PdfA3B)));
    }
}
