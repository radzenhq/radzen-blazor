#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ColorTests
{
    [Fact]
    public void FromRgb_SetsChannelsAndOpaqueAlpha()
    {
        var c = Color.FromRgb(10, 20, 30);
        Assert.Equal(10, c.R);
        Assert.Equal(20, c.G);
        Assert.Equal(30, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void FromArgb_SetsAllChannels()
    {
        var c = Color.FromArgb(64, 10, 20, 30);
        Assert.Equal(64, c.A);
        Assert.Equal(10, c.R);
        Assert.Equal(20, c.G);
        Assert.Equal(30, c.B);
    }

    [Fact]
    public void FromHex_RRGGBB_WithHash()
    {
        var c = Color.FromHex("#12ABef");
        Assert.Equal(0x12, c.R);
        Assert.Equal(0xAB, c.G);
        Assert.Equal(0xEF, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void FromHex_RRGGBB_WithoutHash()
    {
        var c = Color.FromHex("112233");
        Assert.Equal(0x11, c.R);
        Assert.Equal(0x22, c.G);
        Assert.Equal(0x33, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void FromHex_AARRGGBB()
    {
        var c = Color.FromHex("#80112233");
        Assert.Equal(0x80, c.A);
        Assert.Equal(0x11, c.R);
        Assert.Equal(0x22, c.G);
        Assert.Equal(0x33, c.B);
    }

    [Fact]
    public void FromHex_RgbShorthand()
    {
        var c = Color.FromHex("#F0A");
        Assert.Equal(0xFF, c.R);
        Assert.Equal(0x00, c.G);
        Assert.Equal(0xAA, c.B);
        Assert.Equal(255, c.A);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#12")]
    [InlineData("#GGGGGG")]
    [InlineData("12345")]
    public void FromHex_Invalid_Throws(string value)
    {
        Assert.Throws<FormatException>(() => Color.FromHex(value));
    }

    [Fact]
    public void Equality_Operators()
    {
        var a = Color.FromRgb(1, 2, 3);
        var b = Color.FromArgb(255, 1, 2, 3);
        var c = Color.FromRgb(1, 2, 4);

        Assert.True(a == b);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
    }
}
