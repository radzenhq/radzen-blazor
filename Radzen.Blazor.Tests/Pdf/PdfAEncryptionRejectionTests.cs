#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfAEncryptionRejectionTests
{
    private static DocumentBuilder Author(PdfAConformance conformance)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        builder.Info.Title = "PDF/A Invoice";

        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello PDF/A", BuildTestSupport.Latin);

        builder.Conformance = conformance;
        return builder;
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B)]
    [InlineData(PdfAConformance.PdfA3B)]
    [InlineData(PdfAConformance.PdfA4)]
    public void PdfA_WithEncryption_ThrowsActionable(PdfAConformance conformance)
    {
        var builder = Author(conformance);
        builder.Encryption = new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = EncryptionAlgorithm.Aes128,
            UserPassword = "s3cret",
        };

        var exception = Record.Exception(() => builder.ToArray());

        Assert.True(exception is not null, "PDF/A output must not be encrypted");
        Assert.Contains("PDF/A forbids encryption", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Encryption", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA_WithoutEncryption_Succeeds()
    {
        Assert.NotEmpty(Author(PdfAConformance.PdfA3B).ToArray());
    }
}
