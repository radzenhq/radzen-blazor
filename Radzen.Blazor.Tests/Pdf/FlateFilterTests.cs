#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FlateFilterTests
{
    [Fact]
    public void Decode_KnownZlibVector_ReturnsData()
    {
        // Verified: python3 -c "import zlib;print(zlib.compress(b'data').hex())" -> 789c4b492c4904000400019b
        var input = new byte[] { 0x78, 0x9C, 0x4B, 0x49, 0x2C, 0x49, 0x04, 0x00, 0x04, 0x00, 0x01, 0x9B };
        var result = FlateFilter.Decode(input);
        Assert.Equal("data", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void Encode_OutputStartsWithZlibCmf()
    {
        var result = FlateFilter.Encode(Encoding.ASCII.GetBytes("data"));
        Assert.Equal(0x78, result[0]);
    }

    [Fact]
    public void RoundTrip_Empty()
    {
        var data = Array.Empty<byte>();
        Assert.Equal(data, FlateFilter.Decode(FlateFilter.Encode(data)));
    }

    [Fact]
    public void RoundTrip_SingleByte()
    {
        var data = new byte[] { 0x42 };
        Assert.Equal(data, FlateFilter.Decode(FlateFilter.Encode(data)));
    }

    [Fact]
    public void RoundTrip_Text()
    {
        var data = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. 0123456789");
        Assert.Equal(data, FlateFilter.Decode(FlateFilter.Encode(data)));
    }

    [Fact]
    public void RoundTrip_LargeDeterministic()
    {
        var data = new byte[100 * 1024];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)((i * 31 + (i >> 3) * 7 + 13) & 0xFF);
        }
        Assert.Equal(data, FlateFilter.Decode(FlateFilter.Encode(data)));
    }

    [Fact]
    public void Decode_Garbage_Throws()
    {
        var garbage = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
        Assert.Throws<InvalidDataException>(() => FlateFilter.Decode(garbage));
    }

    [Fact]
    public void Decode_Empty_Throws()
    {
        Assert.Throws<InvalidDataException>(() => FlateFilter.Decode(Array.Empty<byte>()));
    }
}
