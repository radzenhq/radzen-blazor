using System;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class QrEncoderTests
    {
        [Fact]
        public void EncodeUtf8_AB_Quartile_ProducesVersion1Matrix()
        {
            var modules = QrEncoder.EncodeUtf8("AB", QrErrorCorrection.Quartile);

            Assert.Equal(21, modules.GetLength(0));
            Assert.Equal(21, modules.GetLength(1));
        }

        [Fact]
        public void EncodeUtf8_ProducesFinderPatterns()
        {
            var modules = QrEncoder.EncodeUtf8("AB", QrErrorCorrection.Quartile);
            int n = modules.GetLength(0);

            // Finder pattern corners are dark, separator next to them is light
            Assert.True(modules[0, 0]);
            Assert.True(modules[3, 3]);
            Assert.True(modules[0, n - 1]);
            Assert.True(modules[n - 1, 0]);
            Assert.False(modules[0, 7]);
            Assert.False(modules[7, 0]);
        }

        [Fact]
        public void EncodeUtf8_DifferentValues_ProduceDifferentMatrices()
        {
            var hello = QrEncoder.EncodeUtf8("Hello", QrErrorCorrection.Medium);
            var world = QrEncoder.EncodeUtf8("World", QrErrorCorrection.Medium);

            Assert.False(MatricesEqual(hello, world));
        }

        [Fact]
        public void EncodeUtf8_HigherEcc_ProducesLargerMatrix()
        {
            var low = QrEncoder.EncodeUtf8("ABCDEFGHIJKLMNOP", QrErrorCorrection.Low);
            var high = QrEncoder.EncodeUtf8("ABCDEFGHIJKLMNOP", QrErrorCorrection.High);

            Assert.True(high.GetLength(0) > low.GetLength(0));
        }

        [Fact]
        public void EncodeBytes_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => QrEncoder.EncodeBytes(null!));
        }

        [Fact]
        public void EncodeBytes_InvalidVersionRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => QrEncoder.EncodeBytes(new byte[] { 1 }, QrErrorCorrection.Medium, 5, 3));
        }

        [Fact]
        public void ToSvg_AB_RendersViewBoxWithQuietZone()
        {
            var modules = QrEncoder.EncodeUtf8("AB", QrErrorCorrection.Quartile);
            var svg = QrEncoder.ToSvg(modules);

            // 21 modules + 2*4 quiet zone = 29
            Assert.Contains(@"viewBox=""0 0 29 29""", svg);
            Assert.Contains(@"<rect x=""4"" y=""4"" width=""7"" height=""7""", svg);
            Assert.Contains(@"<rect x=""18"" y=""4"" width=""7"" height=""7""", svg);
            Assert.Contains(@"<rect x=""4"" y=""18"" width=""7"" height=""7""", svg);
        }

        [Fact]
        public void ToSvg_CircleModuleShape_RendersCircles()
        {
            var modules = QrEncoder.EncodeUtf8("AB", QrErrorCorrection.Quartile);
            var svg = QrEncoder.ToSvg(modules, moduleShape: QrModuleShape.Circle);

            Assert.Contains("<circle", svg);
        }

        [Theory]
        [InlineData(RadzenQREcc.Low)]
        [InlineData(RadzenQREcc.Medium)]
        [InlineData(RadzenQREcc.Quartile)]
        [InlineData(RadzenQREcc.High)]
        public void LegacyEncoder_MatchesNeutralEncoder(RadzenQREcc ecc)
        {
            var legacy = RadzenQREncoder.EncodeUtf8("Hello", ecc);
            var neutral = QrEncoder.EncodeUtf8("Hello", (QrErrorCorrection)(int)ecc);

            Assert.True(MatricesEqual(legacy, neutral));
        }

        static bool MatricesEqual(bool[,] a, bool[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
            {
                return false;
            }

            for (int r = 0; r < a.GetLength(0); r++)
            {
                for (int c = 0; c < a.GetLength(1); c++)
                {
                    if (a[r, c] != b[r, c])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
