#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Signing;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentSignTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class ConstantSigner(byte[] blob) : ISigner
    {
        public byte[] Sign(SignedContent content) => blob;
    }

    private static Document BuildDocument()
    {
        var document = new Radzen.Documents.Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Signed document body", BuildTestSupport.Latin);
        return new DocumentRenderer().Render(document);
    }

    private static SignatureOptions Options() => new()
    {
        Reason = "Approval",
        Location = "Sofia",
        SignerName = "Radzen Test Signer",
        SigningTime = FixedTime,
    };

    [Fact]
    public void Sign_MatchesPdfSignerOverToArrayBytes()
    {
        var blob = new byte[128];
        for (var i = 0; i < blob.Length; i++)
        {
            blob[i] = (byte)(i * 7);
        }

        var signer = new ConstantSigner(blob);
        var document = BuildDocument();
        var expected = PdfSigner.Sign(document.ToArray(), Options(), signer);

        var actual = document.Sign(Options(), signer);

        Assert.Equal(expected, actual);
    }
}
