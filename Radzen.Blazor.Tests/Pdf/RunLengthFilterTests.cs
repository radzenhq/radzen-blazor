#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class RunLengthFilterTests
{
    const long MaxOutput = 1 << 20;

    [Fact]
    public void Decode_LiteralRun()
    {
        var input = new byte[] { 2, 0x41, 0x42, 0x43, 128 };
        Assert.Equal("ABC", Encoding.ASCII.GetString(RunLengthFilter.Decode(input, MaxOutput)));
    }

    [Fact]
    public void Decode_RepeatRun()
    {
        var input = new byte[] { 253, 0x41, 128 };
        Assert.Equal("AAAA", Encoding.ASCII.GetString(RunLengthFilter.Decode(input, MaxOutput)));
    }

    [Fact]
    public void Decode_MixedRuns()
    {
        var input = new byte[] { 1, 0x41, 0x42, 254, 0x43, 0, 0x44, 128 };
        Assert.Equal("ABCCCD", Encoding.ASCII.GetString(RunLengthFilter.Decode(input, MaxOutput)));
    }

    [Fact]
    public void Decode_StopsAtEod_IgnoresTrailing()
    {
        var input = new byte[] { 2, 0x41, 0x42, 0x43, 128, 2, 0x58, 0x59, 0x5A };
        Assert.Equal("ABC", Encoding.ASCII.GetString(RunLengthFilter.Decode(input, MaxOutput)));
    }

    [Fact]
    public void Decode_MissingEod_Tolerated()
    {
        var input = new byte[] { 2, 0x41, 0x42, 0x43 };
        Assert.Equal("ABC", Encoding.ASCII.GetString(RunLengthFilter.Decode(input, MaxOutput)));
    }

    [Fact]
    public void Decode_ImmediateEod_ReturnsEmpty()
    {
        Assert.Empty(RunLengthFilter.Decode(new byte[] { 128 }, MaxOutput));
    }

    [Fact]
    public void Decode_Empty_ReturnsEmpty()
    {
        Assert.Empty(RunLengthFilter.Decode(Array.Empty<byte>(), MaxOutput));
    }

    [Fact]
    public void Decode_LongRunsAndLiterals()
    {
        var input = new byte[]
        {
            0xF9, 0x41, 0x03, 0x42, 0x43, 0x44, 0x45, 0xFB, 0x46, 0xF7, 0x47, 0x02, 0x48, 0x49, 0x4A, 0x80,
        };

        Assert.Equal("AAAAAAAABCDEFFFFFFGGGGGGGGGGHIJ", Encoding.ASCII.GetString(RunLengthFilter.Decode(input, MaxOutput)));
    }
}
