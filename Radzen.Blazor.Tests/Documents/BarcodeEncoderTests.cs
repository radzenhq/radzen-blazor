using System;
using System.Linq;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Tests
{
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

        [Theory]
        [InlineData(RadzenBarcodeType.Code128, "ABC")]
        [InlineData(RadzenBarcodeType.Code39, "AB")]
        [InlineData(RadzenBarcodeType.Ean13, "5901234123457")]
        [InlineData(RadzenBarcodeType.Ean8, "96385074")]
        [InlineData(RadzenBarcodeType.UpcA, "036000291452")]
        [InlineData(RadzenBarcodeType.Itf, "1234")]
        [InlineData(RadzenBarcodeType.Postnet, "55555")]
        [InlineData(RadzenBarcodeType.Rm4scc, "AB12")]
        [InlineData(RadzenBarcodeType.Codabar, "A40156B")]
        [InlineData(RadzenBarcodeType.Pharmacode, "12345")]
        [InlineData(RadzenBarcodeType.Isbn, "9783161484100")]
        [InlineData(RadzenBarcodeType.Issn, "20493630")]
        [InlineData(RadzenBarcodeType.Msi, "1234")]
        [InlineData(RadzenBarcodeType.Telepen, "ABC")]
        public void LegacyToSvg_MatchesNeutralToSvg(RadzenBarcodeType type, string value)
        {
            var legacy = RadzenBarcodeEncoder.ToSvg(type, value);
            var neutral = BarcodeEncoder.ToSvg((BarcodeType)(int)type, value);

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
                Radzen.Documents.BarcodeRect converted = legacyBars[i];
                Assert.Equal(neutralBars[i].X, converted.X);
                Assert.Equal(neutralBars[i].Y, converted.Y);
                Assert.Equal(neutralBars[i].Width, converted.Width);
                Assert.Equal(neutralBars[i].Height, converted.Height);
            }
        }
    }
}
