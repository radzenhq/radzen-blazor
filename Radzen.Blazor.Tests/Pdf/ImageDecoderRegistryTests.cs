#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The image decoders are a registry of magic-byte-sniffing IImageDecoder implementations
// rather than a central if-chain: each sniffs only its own signature (returning false and
// yielding to the next for foreign bytes), and only decodes when its magic matches.
public class ImageDecoderRegistryTests
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] Jpeg2000CodestreamMagic = [0xFF, 0x4F, 0xFF, 0x51];

    [Fact]
    public void PngDecoder_YieldsOnForeignMagic()
    {
        Assert.False(new PngImageDecoder().TryDecode(JpegMagic, ReaderLimits.Default, out var png));
        Assert.Null(png);
        Assert.False(new PngImageDecoder().TryDecode(Jpeg2000CodestreamMagic, ReaderLimits.Default, out _));
    }

    [Fact]
    public void JpegDecoder_YieldsOnForeignMagic()
    {
        Assert.False(new JpegImageDecoder().TryDecode(PngMagic, ReaderLimits.Default, out var jpeg));
        Assert.Null(jpeg);
    }

    [Fact]
    public void Jpeg2000Decoder_YieldsOnForeignMagic()
    {
        Assert.False(new Jpeg2000ImageDecoder().TryDecode(PngMagic, ReaderLimits.Default, out var jp2));
        Assert.Null(jp2);
    }

    // A matching signature routes into the format's own decode: a malformed body of the right
    // magic reaches that decoder's validation (rather than silently yielding to another arm).
    [Fact]
    public void PngDecoder_ClaimsThenDecodesOwnMagic()
        => Assert.Throws<InvalidDataException>(
            () => new PngImageDecoder().TryDecode(PngMagic, ReaderLimits.Default, out _));

    [Fact]
    public void Jpeg2000Decoder_ClaimsThenDecodesOwnMagic()
        => Assert.Throws<InvalidDataException>(
            () => new Jpeg2000ImageDecoder().TryDecode(Jpeg2000CodestreamMagic, ReaderLimits.Default, out _));

    [Fact]
    public void Decode_UnrecognizedFormat_Throws()
        => Assert.Throws<NotSupportedException>(() => ImageDecoder.Decode([0x00, 0x01, 0x02, 0x03]));

    // Register is a public plugin seam over process-wide static state, and its read-copy-swap
    // must be atomic: concurrent registrations that each copy the same snapshot lose all but the
    // last, and the loss only surfaces later as an "unrecognized format" for a decoder the caller
    // did register. Every decoder registered here sniffs a magic no other test uses.
    [Fact]
    public void ConcurrentRegistrationsAreAllRetained()
    {
        const int Threads = 8;
        using var start = new Barrier(Threads);
        var magics = new byte[Threads][];

        var tasks = new Task[Threads];
        for (var i = 0; i < Threads; i++)
        {
            var magic = new byte[] { 0x5A, 0x5A, (byte)i };
            magics[i] = magic;
            tasks[i] = Task.Run(() =>
            {
                start.SignalAndWait();
                ImageDecoder.Register(new StubDecoder(magic));
            });
        }

        Task.WaitAll(tasks);

        foreach (var magic in magics)
        {
            // Throws NotSupportedException if this registration was lost.
            Assert.NotNull(ImageDecoder.Decode(magic));
        }
    }

    // Claims exactly one magic prefix and yields on everything else, so registering it
    // cannot shadow a built-in format for any other test in the process.
    private sealed class StubDecoder(byte[] magic) : IImageDecoder
    {
        public bool TryDecode(byte[] data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
        {
            if (!data.AsSpan().StartsWith(magic))
            {
                xobject = null;
                return false;
            }

            var stream = new StreamObject([]);
            stream.Dictionary["Width"] = new NumberObject(1);
            stream.Dictionary["Height"] = new NumberObject(1);
            xobject = new ImageXObject(stream, null);
            return true;
        }
    }
}
