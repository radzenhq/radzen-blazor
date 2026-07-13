#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Document.Sign is a discoverability wrapper: it must produce the exact bytes of
// PdfSigner.Sign over the document's own serialized output for the same options
// and signer.
public class DocumentSignTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class ConstantSigner(byte[] blob) : ISigner
    {
        public byte[] Sign(ReadOnlySpan<byte> content) => blob;
    }

    private static Document BuildDocument()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Signed document body", BuildTestSupport.Latin);
        return builder.Build();
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
