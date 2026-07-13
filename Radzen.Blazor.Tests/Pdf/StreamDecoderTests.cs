#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The stream-decode pipeline (extracted into StreamDecoder) exercised in isolation:
// filter selection, chain-length limit, and the FlateDecode round trip.
public class StreamDecoderTests
{
    private static StreamDecoder NewDecoder()
        => new(ReaderLimits.Default, x => x);

    private static byte[] Deflate(byte[] plain)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(plain, 0, plain.Length);
        }

        return output.ToArray();
    }

    [Fact]
    public void NoFilter_ReturnsInputUnchanged()
    {
        var payload = Encoding.ASCII.GetBytes("no filter here");
        var result = NewDecoder().Decode(new DictionaryObject(), payload);

        Assert.Same(payload, result);
    }

    [Fact]
    public void NumericFilter_Throws()
    {
        var dictionary = new DictionaryObject { ["Filter"] = new NumberObject(7) };

        Assert.Throws<DocumentParseException>(() => NewDecoder().Decode(dictionary, [1, 2, 3]));
    }

    [Fact]
    public void FilterArrayWithNumericMember_Throws()
    {
        var dictionary = new DictionaryObject
        {
            ["Filter"] = new ArrayObject { new NameObject("FlateDecode"), new NumberObject(7) },
        };

        Assert.Throws<DocumentParseException>(() => NewDecoder().Decode(dictionary, [1, 2, 3]));
    }

    [Fact]
    public void FlateDecode_RoundTrips()
    {
        var plain = Encoding.ASCII.GetBytes("BT /F1 12 Tf (hello) Tj ET");
        var dictionary = new DictionaryObject { ["Filter"] = new NameObject("FlateDecode") };

        var result = NewDecoder().Decode(dictionary, Deflate(plain));

        Assert.Equal(plain, result);
    }

    [Fact]
    public void UnsupportedFilter_Throws()
    {
        var dictionary = new DictionaryObject { ["Filter"] = new NameObject("Bogus") };

        Assert.Throws<DocumentParseException>(() => NewDecoder().Decode(dictionary, [1, 2, 3]));
    }

    [Fact]
    public void FilterChainExceedsLimit_Throws()
    {
        var chain = new ArrayObject();
        for (var i = 0; i <= ReaderLimits.Default.MaxFilterChainLength; i++)
        {
            chain.Add(new NameObject("FlateDecode"));
        }

        var dictionary = new DictionaryObject { ["Filter"] = chain };

        Assert.Throws<DocumentParseException>(() => NewDecoder().Decode(dictionary, [0]));
    }
}
