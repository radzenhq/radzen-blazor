#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Codes;

namespace Radzen.Blazor.Documents.Tests;

public class BarcodeEncoderTests
{
    [Fact]
    public void EncodeCode128B_ABC_ProducesExpectedChecksumAndWidths()
    {
        var widths = BarcodeEncoder.EncodeCode128B("ABC", out var checksum);

        Assert.Equal(1, checksum);
        // start + A + B + C + checksum (6 modules each) + stop (7 modules)
        Assert.Equal(37, widths.Count);
        // total width 70 modules (including 2-module termination bar)
        Assert.Equal(70, widths.Sum());
    }

    [Fact]
    public void EncodeCode39_AB_ProducesExpectedTotalWidth()
    {
        var widths = BarcodeEncoder.EncodeCode39("AB");

        // *AB* = 4 chars x 12 modules + 3 inter-character gaps
        Assert.Equal(51, widths.Sum());
    }

    [Fact]
    public void EncodeEan13_ComputesCheckDigit()
    {
        var bits = BarcodeEncoder.EncodeEan13("590123412345", out var checksum);

        Assert.Equal("7", checksum);
        Assert.Equal(95, bits.Length);
        Assert.StartsWith("101", bits, StringComparison.Ordinal);
        Assert.EndsWith("101", bits, StringComparison.Ordinal);
    }

    [Fact]
    public void EncodeEan13_InvalidCheckDigit_Throws()
    {
        Assert.Throws<ArgumentException>(() => BarcodeEncoder.EncodeEan13("5901234123450", out _));
    }

    [Fact]
    public void EncodeEan8_ProducesExpectedPattern()
    {
        var bits = BarcodeEncoder.EncodeEan8("9638507", out var checksum);

        Assert.Equal("4", checksum);
        Assert.Equal(67, bits.Length);
    }

    [Fact]
    public void EncodeUpcA_ProducesExpectedPattern()
    {
        var bits = BarcodeEncoder.EncodeUpcA("03600029145", out var checksum);

        Assert.Equal("2", checksum);
        Assert.Equal(95, bits.Length);
    }

    [Fact]
    public void EncodeMsiPlessey_AppendsLuhnCheckDigit()
    {
        var bits = BarcodeEncoder.EncodeMsiPlessey("1234", out var checksum);

        Assert.Equal("4", checksum);
        Assert.StartsWith("110", bits, StringComparison.Ordinal);
        Assert.EndsWith("1001", bits, StringComparison.Ordinal);
    }

    [Fact]
    public void EncodePharmacode_ProducesNarrowAndWideBars()
    {
        var (bars, vbWidth) = BarcodeEncoder.EncodePharmacode("12345", 50, 10);

        Assert.NotEmpty(bars);
        Assert.True(vbWidth > 0);
        Assert.All(bars, bar => Assert.True(bar.Width == 1 || bar.Width == 2));
    }

    [Fact]
    public void ToSvg_Code128_ABC_RendersExpectedBars()
    {
        var svg = BarcodeEncoder.ToSvg(BarcodeType.Code128, "ABC");

        Assert.Contains(@"viewBox=""0 0 90 50""", svg);
        // 1 background rect + 19 bar rects
        var rectCount = System.Text.RegularExpressions.Regex.Matches(svg, "<rect").Count;
        Assert.Equal(20, rectCount);
    }

    private static readonly Dictionary<BarcodeType, string> SampleByType = new()
    {
        [BarcodeType.Code128] = "ABC",
        [BarcodeType.Code39] = "AB",
        [BarcodeType.Ean13] = "5901234123457",
        [BarcodeType.Ean8] = "96385074",
        [BarcodeType.UpcA] = "036000291452",
        [BarcodeType.Itf] = "1234",
        [BarcodeType.Postnet] = "55555",
        [BarcodeType.Rm4scc] = "AB12",
        [BarcodeType.Codabar] = "A40156B",
        [BarcodeType.Pharmacode] = "12345",
        [BarcodeType.Isbn] = "9783161484100",
        [BarcodeType.Issn] = "20493630",
        [BarcodeType.Msi] = "1234",
        [BarcodeType.Telepen] = "ABC",
    };

    public static TheoryData<BarcodeType> EveryBarcodeType()
    {
        var data = new TheoryData<BarcodeType>();

        foreach (var type in Enum.GetValues<BarcodeType>())
        {
            data.Add(type);
        }

        return data;
    }

    [Fact]
    public void SampleByType_CoversEveryBarcodeType()
    {
        Assert.Equal(Enum.GetValues<BarcodeType>(), SampleByType.Keys.OrderBy(type => (int)type));
    }

    [Fact]
    public void RadzenBarcodeType_MirrorsBarcodeTypeByNameAndOrdinal()
    {
        Assert.Equal(
            Enum.GetValues<BarcodeType>().Select(type => (type.ToString(), (int)type)),
            Enum.GetValues<RadzenBarcodeType>().Select(type => (type.ToString(), (int)type)));
    }

    [Theory]
    [MemberData(nameof(EveryBarcodeType))]
    public void LegacyToSvg_MatchesNeutralToSvg(BarcodeType type)
    {
        var value = SampleByType[type];

        var legacy = RadzenBarcodeEncoder.ToSvg((RadzenBarcodeType)(int)type, value);
        var neutral = BarcodeEncoder.ToSvg(type, value);

        Assert.Equal(neutral, legacy);
    }

    [Fact]
    public void LegacyPostnet_MatchesNeutralGeometry()
    {
        var (legacyBars, legacyWidth) = RadzenBarcodeEncoder.EncodePostnet("55555", 50, 10, out var legacyChecksum);
        var (neutralBars, neutralWidth) = BarcodeEncoder.EncodePostnet("55555", 50, 10, out var neutralChecksum);

        Assert.Equal(neutralChecksum, legacyChecksum);
        Assert.Equal(neutralWidth, legacyWidth);
        Assert.Equal(neutralBars.Count, legacyBars.Count);

        for (int i = 0; i < neutralBars.Count; i++)
        {
            Radzen.Documents.Rect converted = legacyBars[i];
            Assert.Equal(neutralBars[i].X, converted.X);
            Assert.Equal(neutralBars[i].Y, converted.Y);
            Assert.Equal(neutralBars[i].Width, converted.Width);
            Assert.Equal(neutralBars[i].Height, converted.Height);
        }
    }

    [Theory]
    [InlineData("#000\" onload=\"alert(1)")]
    [InlineData("red\"><script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public void ToSvg_RejectsOrNeutralizesHostileColors(string color)
    {
        var thrown = Record.Exception(() => BarcodeEncoder.ToSvg(BarcodeType.Code128, "ABC", foreground: color));

        if (thrown is null)
        {
            var svg = BarcodeEncoder.ToSvg(BarcodeType.Code128, "ABC", foreground: color);

            Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onload=", svg, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.IsType<ArgumentException>(thrown);
        }
    }

    [Fact]
    public void ToSvg_RejectsAnUnparseableBackgroundColor()
        => Assert.Throws<ArgumentException>(
            () => BarcodeEncoder.ToSvg(BarcodeType.Code128, "ABC", background: "\"><rect fill=\"red\"/>"));

    [Fact]
    public void ToSvg_AcceptsTheDocumentedColorForms()
    {
        var svg = BarcodeEncoder.ToSvg(BarcodeType.Code128, "ABC", foreground: "rgb(1, 2, 3)", background: "papayawhip");

        Assert.Contains("fill=\"rgb(1, 2, 3)\"", svg, StringComparison.Ordinal);
        Assert.Contains("fill=\"papayawhip\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("fill-opacity", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void ToSvg_HonoursTheAlphaChannelOfAnRgbaColor()
    {
        var svg = BarcodeEncoder.ToSvg(BarcodeType.Code128, "ABC", foreground: "rgba(1, 2, 3, 0.25)");

        Assert.Contains("fill=\"rgb(1, 2, 3)\" fill-opacity=\"0.25\"", svg, StringComparison.Ordinal);
    }
}
