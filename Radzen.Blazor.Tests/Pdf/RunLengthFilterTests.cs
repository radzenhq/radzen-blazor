#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class RunLengthFilterTests
{
    [Fact]
    public void Decode_LiteralRun()
    {
        var input = new byte[] { 2, 0x41, 0x42, 0x43, 128 };
        Assert.Equal("ABC", Encoding.ASCII.GetString(RunLengthFilter.Decode(input)));
    }

    [Fact]
    public void Decode_RepeatRun()
    {
        var input = new byte[] { 253, 0x41, 128 };
        Assert.Equal("AAAA", Encoding.ASCII.GetString(RunLengthFilter.Decode(input)));
    }

    [Fact]
    public void Decode_MixedRuns()
    {
        var input = new byte[] { 1, 0x41, 0x42, 254, 0x43, 0, 0x44, 128 };
        Assert.Equal("ABCCCD", Encoding.ASCII.GetString(RunLengthFilter.Decode(input)));
    }

    [Fact]
    public void Decode_StopsAtEod_IgnoresTrailing()
    {
        var input = new byte[] { 2, 0x41, 0x42, 0x43, 128, 2, 0x58, 0x59, 0x5A };
        Assert.Equal("ABC", Encoding.ASCII.GetString(RunLengthFilter.Decode(input)));
    }

    [Fact]
    public void Decode_MissingEod_Tolerated()
    {
        var input = new byte[] { 2, 0x41, 0x42, 0x43 };
        Assert.Equal("ABC", Encoding.ASCII.GetString(RunLengthFilter.Decode(input)));
    }

    [Fact]
    public void Decode_ImmediateEod_ReturnsEmpty()
    {
        Assert.Empty(RunLengthFilter.Decode(new byte[] { 128 }));
    }

    [Fact]
    public void Decode_Empty_ReturnsEmpty()
    {
        Assert.Empty(RunLengthFilter.Decode(Array.Empty<byte>()));
    }

    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        var data = Encoding.ASCII.GetBytes("AAAAAAAABCDEFFFFFFGGGGGGGGGGHIJ");
        var decoded = RunLengthFilter.Decode(RunLengthFilter.Encode(data));
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void Encode_EndsWithEod()
    {
        var encoded = RunLengthFilter.Encode(Encoding.ASCII.GetBytes("hello"));
        Assert.Equal(128, encoded[encoded.Length - 1]);
    }
}
