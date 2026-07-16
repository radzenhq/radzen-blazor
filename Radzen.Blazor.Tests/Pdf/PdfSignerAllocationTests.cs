#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Signing appends a few KB to a document that may be tens of MB. The signed bytes are the
// two /ByteRange segments of the document already in hand, so handing them to an ISigner
// must not materialize a second copy of the whole file: for a large scanned PDF that copy
// alone is an LOH allocation the size of the document.
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

        // Incompressible bytes give the original a predictable size.
        var payload = new byte[padding];
        new Random(7).NextBytes(payload);
        document.Pages.Add(PageSizes.A4).SetContent(payload);

        return document.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] Sign(byte[] original)
        => PdfSigner.Sign(original, new SignatureOptions { SignerName = "Signer" }, new FixedSigner());

    [Fact]
    public void Sign_DoesNotCopyTheDocumentForTheSigner()
    {
        var original = BuildLargeDocument(4 * 1024 * 1024);

        Sign(original);

        var bytes = Measure(() => Sign(original));

        // The returned signed document is unavoidable; a second full-size array is not.
        var budget = original.Length * 2L;
        Assert.True(
            bytes < budget,
            $"Signing a {original.Length} byte document allocated {bytes} bytes (budget {budget}).");
    }

    [Fact]
    public void Sign_OverheadDoesNotScaleWithDocumentSize()
    {
        var small = BuildLargeDocument(1 * 1024 * 1024);
        var large = BuildLargeDocument(8 * 1024 * 1024);

        Sign(small);
        Sign(large);

        var smallBytes = Measure(() => Sign(small)) - small.Length;
        var largeBytes = Measure(() => Sign(large)) - large.Length;

        Assert.True(
            largeBytes < smallBytes + (large.Length - small.Length),
            $"Excess grew from {smallBytes} to {largeBytes} between a {small.Length} and {large.Length} byte document.");
    }

    [Fact]
    public void Sign_HandsTheSignerBothByteRangeSegments()
    {
        var original = BuildLargeDocument(64 * 1024);
        var options = new SignatureOptions { SignerName = "Signer", SignatureMaxSizeBytes = 4096 };
        var signer = new FixedSigner();

        var signed = PdfSigner.Sign(original, options, signer);

        // Everything except the <...> /Contents token, angle brackets included.
        var contents = (options.SignatureMaxSizeBytes * 2) + 2;
        Assert.Equal(signed.Length - contents, signer.ContentLength);
    }
}
