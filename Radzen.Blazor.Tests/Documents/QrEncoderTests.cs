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
        public void EncodeBytes_SingleMasked_RoundTripsToOriginalPayload()
        {
            // A double-applied data mask XOR-cancels while the format bits still claim a mask,
            // so the matrix no longer decodes. Decoding must recover the exact input bytes.
            var input = System.Text.Encoding.UTF8.GetBytes("AB");
            var matrix = QrEncoder.EncodeBytes(input, QrErrorCorrection.Medium);

            Assert.Equal(input, DecodeVersion1(matrix));
        }

        [Fact]
        public void EncodeBytes_MatchesEncodeUtf8_ForSameContent()
        {
            var viaString = QrEncoder.EncodeUtf8("Hello", QrErrorCorrection.Medium);
            var viaBytes = QrEncoder.EncodeBytes(System.Text.Encoding.UTF8.GetBytes("Hello"), QrErrorCorrection.Medium);

            Assert.True(MatricesEqual(viaString, viaBytes));
        }

        // Minimal decoder for a version-1 byte-mode symbol (no Reed-Solomon correction assumed).
        static byte[] DecodeVersion1(bool[,] m)
        {
            const int n = 21;
            var reserved = Version1Reserved();

            int fmt = 0;
            void Rd(int i, bool cell) { if (cell) { fmt |= 1 << (14 - i); } }
            for (int i = 0; i <= 5; i++) { Rd(i, m[8, i]); }
            Rd(6, m[8, 7]);
            Rd(7, m[8, 8]);
            Rd(8, m[7, 8]);
            for (int i = 9; i <= 14; i++) { Rd(i, m[14 - i, 8]); }
            int mask = ((fmt ^ 0x5412) >> 10) & 7;

            var bits = new System.Collections.Generic.List<int>();
            int dir = -1;
            for (int col = n - 1; col > 0; col -= 2)
            {
                if (col == 6) { col--; }
                int rowStart = dir < 0 ? n - 1 : 0;
                for (int i = 0; i < n; i++)
                {
                    int r = rowStart + i * dir;
                    for (int c = 0; c < 2; c++)
                    {
                        int cc = col - c;
                        if (reserved[r, cc]) { continue; }
                        bool cell = m[r, cc];
                        if (MaskBit(mask, r, cc)) { cell = !cell; }
                        bits.Add(cell ? 1 : 0);
                    }
                }
                dir *= -1;
            }

            int pos = 0;
            int Read(int k) { int v = 0; for (; k > 0; k--) { v = (v << 1) | bits[pos++]; } return v; }
            int mode = Read(4);
            Assert.Equal(0b0100, mode);
            int len = Read(8);
            var payload = new byte[len];
            for (int i = 0; i < len; i++) { payload[i] = (byte)Read(8); }
            return payload;
        }

        static bool[,] Version1Reserved()
        {
            const int n = 21;
            var res = new bool[n, n];
            void Block(int r0, int r1, int c0, int c1)
            {
                for (int r = r0; r <= r1; r++)
                {
                    for (int c = c0; c <= c1; c++) { res[r, c] = true; }
                }
            }
            Block(0, 7, 0, 7);   // top-left finder + separator
            Block(0, 7, 13, 20); // top-right finder + separator
            Block(13, 20, 0, 7); // bottom-left finder + separator
            for (int i = 8; i <= 12; i++) { res[6, i] = true; res[i, 6] = true; } // timing
            res[13, 8] = true;   // dark module
            Block(8, 8, 0, 5);   // format info copies
            res[8, 7] = true; res[8, 8] = true; res[7, 8] = true;
            Block(0, 5, 8, 8);
            for (int i = 0; i < 7; i++) { res[n - 1 - i, 8] = true; }
            for (int i = 0; i < 8; i++) { res[8, n - 8 + i] = true; }
            return res;
        }

        static bool MaskBit(int mask, int r, int c) => mask switch
        {
            0 => ((r + c) & 1) == 0,
            1 => (r & 1) == 0,
            2 => (c % 3) == 0,
            3 => ((r + c) % 3) == 0,
            4 => (((r / 2) + (c / 3)) & 1) == 0,
            5 => ((r * c) % 2 + (r * c) % 3) == 0,
            6 => ((((r * c) % 2) + ((r * c) % 3)) & 1) == 0,
            7 => ((((r + c) % 2) + ((r * c) % 3)) & 1) == 0,
            _ => false
        };

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
