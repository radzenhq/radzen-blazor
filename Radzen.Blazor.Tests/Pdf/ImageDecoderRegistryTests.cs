#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
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
}
