#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Signing;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfSignerAllocationTests
{
    private sealed class FixedSigner : ISigner
    {
        public int ContentLength { get; private set; }

        public byte[] Sign(SignedContent content)
        {
            ContentLength = content.Length;
            return new byte[100];
        }
    }

    private static byte[] BuildLargeDocument(int padding)
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (page zero) Tj ET"));

        var payload = new byte[padding];
        new Random(7).NextBytes(payload);
        document.Pages.Add(PageSizes.A4).SetContent(payload);

        return document.ToArray();
    }

    [Fact]
    public void Sign_HandsTheSignerBothByteRangeSegments()
    {
        var original = BuildLargeDocument(64 * 1024);
        var options = new SignatureOptions { SignerName = "Signer", SignatureMaxSizeBytes = 4096 };
        var signer = new FixedSigner();

        var signed = PdfSigner.Sign(original, options, signer);

        var contents = (options.SignatureMaxSizeBytes * 2) + 2;
        Assert.Equal(signed.Length - contents, signer.ContentLength);
    }
}
