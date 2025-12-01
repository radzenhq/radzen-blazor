using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Blazor;

/// <summary>
/// Error correction.
/// </summary>
public enum RadzenQREcc 
{
    /// <summary>
    /// 7% recovery.
    /// </summary>
    Low,
    /// <summary>
    /// 15% recovery.
    /// </summary>
    Medium,
    /// <summary>
    /// 25% recovery.
    /// </summary>
    Quartile,
    /// <summary>
    /// 30% recovery.
    /// </summary>
    High
}

internal static class RadzenQREncoder
{
    /// <summary>Encode a UTF-8 string into a QR module matrix.</summary>
    public static bool[,] EncodeUtf8(string value, RadzenQREcc ecc, int minVersion = 1, int maxVersion = 40)
    {
        var data = Encoding.UTF8.GetBytes(value ?? string.Empty);

        for (int ver = minVersion; ver <= maxVersion; ver++)
        {
            // --- Compute capacity from EC params
            var (dataCwCount, ecPerBlock, g1, g1dcw, g2, g2dcw) = EcParams(ver, ecc);
            int capacityBits = dataCwCount * 8;

            // --- Build header (BYTE mode) exactly as per spec
            int charCountBits = ver <= 9 ? 8 : 16;
            var bb = new BitBuffer();
            bb.AppendBits(0b0100, 4);                     // mode: BYTE
            bb.AppendBits(data.Length, charCountBits);    // char count
            foreach (var b in data) bb.AppendBits(b, 8);  // payload

            // --- Fit math (this is the authoritative acceptance test)
            int baseBits = bb.Length;                     // header+data bits
            if (baseBits > capacityBits)                  // early reject
                continue;

            int needed = baseBits + Math.Min(4, capacityBits - baseBits); // add up to 4-bit terminator
            needed += (8 - (needed % 8)) % 8;            // pad to byte boundary

            if (needed > capacityBits)
                continue; // try next version

            // --- Build data codewords (pads 0xEC/0x11 as needed)
            var dataCwBytes = BuildDataCodewords(bb, dataCwCount);

            // --- Split into blocks, make EC, interleave
            var blocks = BuildBlocks(dataCwBytes, ecPerBlock, g1, g1dcw, g2, g2dcw);
            var final = Interleave(blocks);

            // --- Base matrix + place + mask + format (+version if v>=7)
            var (m, reserved) = BuildBaseMatrix(ver);
            PlaceData(m, reserved, final);

            int bestMask = ChooseBestMask(m, reserved);  // or force 0 while testing
            ApplyMask(m, reserved, bestMask);            // mask only NON-reserved
            WriteFormatInfo(m, reserved, ecc, bestMask); // write both copies
            if (ver >= 7) WriteVersionInfo(m, reserved, ver);

            //var sb = new StringBuilder();
            //sb.AppendLine("DATA: " + BitConverter.ToString(dataCwBytes));
            //sb.AppendLine("EC  : " + BitConverter.ToString(blocks[0].Ec));
            //int ecl = 0; // M=00
            //int fmt = BchEncode((ecl << 3) | 0 /*mask 0*/, 0x537, 15, 5) ^ 0x5412;
            //sb.AppendLine("FMT : " + Convert.ToString(fmt, 2).PadLeft(15, '0'));

            return m;
        }

        // If we get here, nothing fit. Throw with diagnostics so you see why.
        throw new ArgumentException($"Data too long for versions {minVersion}..{maxVersion} at ECC={ecc}.");
    }

    /// <summary>Encode raw bytes into a QR module matrix.</summary>
    public static bool[,] EncodeBytes(byte[] data, RadzenQREcc ecc = RadzenQREcc.Medium, int minVersion = 1, int maxVersion = 40)
    {
        if (minVersion < 1 || maxVersion > 40 || minVersion > maxVersion)
            throw new ArgumentOutOfRangeException(nameof(minVersion), "Version range must be within 1..40");

        // Try versions until payload (with headers) fits into available data bits
        for (int ver = minVersion; ver <= maxVersion; ver++)
        {
            int charCountBits = (ver <= 9) ? 8 : 16; // byte mode: v1-9:8, v10-40:16
            var bb = new BitBuffer();
            // Mode = Byte (0100)
            bb.AppendBits(0b0100, 4);
            bb.AppendBits(data.Length, charCountBits);
            foreach (byte b in data) bb.AppendBits(b, 8);

            var (dataCw, ecPerBlock, grp1Blocks, grp1DataCw, grp2Blocks, grp2DataCw) = EcParams(ver, ecc);
            int capacityBits = dataCw * 8;

            // Terminator up to 4 bits
            int needed = bb.Length + Math.Min(4, Math.Max(0, capacityBits - bb.Length));
            // Pad to byte
            needed += (8 - (needed % 8)) % 8;

            if (needed <= capacityBits)
            {
                // Build final data codewords (with pad bytes 0xEC, 0x11)
                var dataCwBytes = BuildDataCodewords(bb, dataCw);

                // Split into blocks and generate EC codewords
                var blocks = BuildBlocks(dataCwBytes, ecPerBlock, grp1Blocks, grp1DataCw, grp2Blocks, grp2DataCw);

                // Interleave data & EC codewords
                var final = Interleave(blocks);

                // Build base matrix with patterns
                var (m, reserved) = BuildBaseMatrix(ver);

                // Place data bits (zig-zag)
                PlaceData(m, reserved, final);

                // Choose best mask and apply
                int bestMask = ChooseBestMask(m, reserved);
                ApplyMask(m, reserved, bestMask);
                WriteFormatInfo(m, reserved, ecc, bestMask);

                ApplyMask(m, reserved, bestMask);

                // Write format info (depends on ECC + mask)
                WriteFormatInfo(m, reserved, ecc, bestMask);

                // Write version info for v7+
                if (ver >= 7) WriteVersionInfo(m, reserved, ver);

                return m;
            }
        }

        throw new ArgumentException("Data too long for the given version range and ECC.");
    }

    /// <summary>Render a module matrix into an SVG string with a 4-module quiet zone.</summary>
    public static string ToSvg(bool[,] modules, int moduleSize = 8, string foreground = "#000000", string background = "#FFFFFF")
    {
        int n = modules.GetLength(0);
        int vb = n + 8;  // 4 modules of quiet zone on each side
        int px = vb * moduleSize;

        var sb = new StringBuilder(n * n + 1024);
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{px}\" height=\"{px}\" viewBox=\"0 0 {vb} {vb}\" shape-rendering=\"crispEdges\">");
        sb.Append($"<rect x=\"0\" y=\"0\" width=\"{vb}\" height=\"{vb}\" fill=\"{background}\"/>");

        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                if (modules[r, c])
                    sb.Append($"<rect x=\"{c + 4}\" y=\"{r + 4}\" width=\"1\" height=\"1\" fill=\"{foreground}\"/>");
            }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    // ---------- Core building ----------

    private static (bool[,] m, bool[,] res) BuildBaseMatrix(int ver)
    {
        int n = 21 + 4 * (ver - 1);
        var m = new bool[n, n];
        var reserved = new bool[n, n];

        // Finder patterns + separators + timing
        DrawFinder(m, reserved, 0, 0);
        DrawFinder(m, reserved, n - 7, 0);
        DrawFinder(m, reserved, 0, n - 7);

        // Timing
        for (int i = 8; i < n - 8; i++)
        {
            m[6, i] = (i % 2) == 0; reserved[6, i] = true;
            m[i, 6] = (i % 2) == 0; reserved[i, 6] = true;
        }

        // Alignment patterns
        var ap = AlignmentPatternPositions(ver);
        foreach (int y in ap)
            foreach (int x in ap)
            {
                // Skip the corners with finders
                bool corner = (x < 9 && y < 9) || (x > n - 9 && y < 9) || (x < 9 && y > n - 9);
                if (!corner) DrawAlignment(m, reserved, x, y);
            }

        // Dark module (always)
        m[4 * ver + 9, 8] = true; reserved[4 * ver + 9, 8] = true;

        // Reserve format info areas
        ReserveFormat(reserved);

        // Reserve version info (v7+)
        if (ver >= 7) ReserveVersion(reserved);

        return (m, reserved);
    }

    // Finder with 1-module white separator around (as reserved)
    private static void DrawFinder(bool[,] m, bool[,] res, int x, int y)
    {
        for (int r = -1; r <= 7; r++)
            for (int c = -1; c <= 7; c++)
            {
                int rr = y + r, cc = x + c;
                if (rr < 0 || cc < 0 || rr >= m.GetLength(0) || cc >= m.GetLength(1)) continue;
                bool in7 = r >= 0 && r < 7 && c >= 0 && c < 7;
                if (in7)
                {
                    bool on = r == 0 || r == 6 || c == 0 || c == 6 || (r >= 2 && r <= 4 && c >= 2 && c <= 4);
                    m[rr, cc] = on;
                    res[rr, cc] = true;
                }
                else
                {
                    // separator (light), just reserve
                    res[rr, cc] = true;
                }
            }
    }

    private static void DrawAlignment(bool[,] m, bool[,] res, int cx, int cy)
    {
        for (int r = -2; r <= 2; r++)
            for (int c = -2; c <= 2; c++)
            {
                int rr = cy + r, cc = cx + c;
                m[rr, cc] = Math.Max(Math.Abs(r), Math.Abs(c)) != 1; // dark outer, white ring, dark center
                res[rr, cc] = true;
            }
    }

    private static void ReserveFormat(bool[,] res)
    {
        int n = res.GetLength(0);

        // Row 8, left of the timing cross (cols 0..5)
        for (int c = 0; c <= 5; c++) res[8, c] = true;

        // Row 8, skip col 6 (timing), then 7 and 8 are format
        res[8, 7] = true;
        res[8, 8] = true;

        // Column 8, above the timing cross (rows 0..5)
        for (int r = 0; r <= 5; r++) res[r, 8] = true;

        // Column 8, row 7 is format (row 6 is timing, skip it)
        res[7, 8] = true;

        // Second copy:
        // Column 8, bottom 7 cells (rows n-1 down to n-7)  ← exactly 7 cells
        for (int i = 0; i < 7; i++) res[n - 1 - i, 8] = true;

        // Row 8, right side 8 cells (cols n-8 .. n-1)      ← exactly 8 cells
        for (int i = 0; i < 8; i++) res[8, n - 8 + i] = true;
    }

    private static void ReserveVersion(bool[,] res)
    {
        int n = res.GetLength(0);

        // Bottom-left version info block: 3 rows x 6 columns
        // Rows: n-11, n-10, n-9
        // Cols: 0..5
        for (int r = n - 11; r <= n - 9; r++)
            for (int c = 0; c <= 5; c++)
                res[r, c] = true;

        // Top-right version info block: 6 rows x 3 columns
        // Rows: 0..5
        // Cols: n-11, n-10, n-9
        for (int r = 0; r <= 5; r++)
            for (int c = n - 11; c <= n - 9; c++)
                res[r, c] = true;
    }

    private static void WriteVersionInfo(bool[,] m, bool[,] res, int ver)
    {
        // BCH(18,6) with generator 0x1F25
        int bits = BchEncode(ver, 0x1F25, 18, 6);
        int n = m.GetLength(0);

        // ---- Bottom-left block (3 tall x 6 wide) ----
        // Thonky table:
        //   00 03 06 09 12 15
        //   01 04 07 10 13 16
        //   02 05 08 11 14 17
        //
        // Our bit index i = 0..17 is LSB-first, matching "0 is least significant bit".
        // Map: bitIndex = row + col*3
        for (int col = 0; col < 6; col++)
        {
            for (int row = 0; row < 3; row++)
            {
                int bitIndex = row + col * 3; // 0..17
                bool bit = ((bits >> bitIndex) & 1) != 0;

                int r = n - 11 + row; // rows n-11..n-9
                int c = col;          // cols 0..5
                Set(m, res, r, c, bit);
            }
        }

        // ---- Top-right block (6 tall x 3 wide) ----
        // Thonky table:
        //   00 01 02
        //   03 04 05
        //   06 07 08
        //   09 10 11
        //   12 13 14
        //   15 16 17
        //
        // Map: bitIndex = row*3 + col
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int bitIndex = row * 3 + col; // 0..17
                bool bit = ((bits >> bitIndex) & 1) != 0;

                int r = row;               // rows 0..5
                int c = n - 11 + col;      // cols n-11..n-9
                Set(m, res, r, c, bit);
            }
        }
    }

    // ---------- Data & EC ----------

    private static byte[] BuildDataCodewords(BitBuffer bb, int dataCw)
    {
        int totalBits = dataCw * 8;
        // Terminator
        int remaining = totalBits - bb.Length;
        bb.AppendBits(0, Math.Min(4, remaining));
        // Pad to byte
        while (bb.Length % 8 != 0) bb.AppendBits(0, 1);
        // Build bytes
        var bytes = new List<byte>(dataCw);
        for (int i = 0; i < bb.Length; i += 8)
        {
            if (bytes.Count == dataCw) break;
            int b = 0;
            for (int j = 0; j < 8; j++) b = (b << 1) | bb[i + j];
            bytes.Add((byte)b);
        }
        // Pad bytes
        byte[] pads = { 0xEC, 0x11 }; int p = 0;
        while (bytes.Count < dataCw) bytes.Add(pads[(p++) & 1]);
        return bytes.ToArray();
    }

    private sealed class Block
    {
        public byte[] Data;
        public byte[] Ec;
        public Block(byte[] d, byte[] e) { Data = d; Ec = e; }
    }

    private static List<Block> BuildBlocks(byte[] dataCwBytes, int ecPerBlock,
        int grp1Blocks, int grp1DataCw, int grp2Blocks, int grp2DataCw)
    {
        var blocks = new List<Block>(grp1Blocks + grp2Blocks);
        int idx = 0;
        for (int i = 0; i < grp1Blocks; i++)
        {
            var data = new byte[grp1DataCw];
            Array.Copy(dataCwBytes, idx, data, 0, grp1DataCw);
            idx += grp1DataCw;
            var ec = ReedSolomon(data, ecPerBlock);
            blocks.Add(new Block(data, ec));
        }
        for (int i = 0; i < grp2Blocks; i++)
        {
            var data = new byte[grp2DataCw];
            Array.Copy(dataCwBytes, idx, data, 0, grp2DataCw);
            idx += grp2DataCw;
            var ec = ReedSolomon(data, ecPerBlock);
            blocks.Add(new Block(data, ec));
        }
        return blocks;
    }

    private static byte[] Interleave(List<Block> blocks)
    {
        // Interleave data bytes then EC bytes
        int totalLen = 0;
        int maxData = 0, maxEc = 0;
        foreach (var b in blocks)
        {
            totalLen += b.Data.Length + b.Ec.Length;
            if (b.Data.Length > maxData) maxData = b.Data.Length;
            if (b.Ec.Length > maxEc) maxEc = b.Ec.Length;
        }
        var r = new byte[totalLen];
        int k = 0;
        for (int i = 0; i < maxData; i++)
            foreach (var b in blocks)
                if (i < b.Data.Length) r[k++] = b.Data[i];
        for (int i = 0; i < maxEc; i++)
            foreach (var b in blocks)
                if (i < b.Ec.Length) r[k++] = b.Ec[i];
        return r;
    }

    private static byte[] RsGenerator(int ecCount)
    {
        var gen = new List<byte> { 1 };
        for (int i = 0; i < ecCount; i++)
            gen = PolyMul(gen, new List<byte> { 1, GfPow(2, i) }); // (x - α^i), α=2
        return gen.ToArray(); // length = ecCount + 1 (leading 1 included)
    }

    private static byte[] ReedSolomon(byte[] data, int ecCount)
    {
        var gen = RsGenerator(ecCount);
        var msg = new byte[data.Length + ecCount];
        Array.Copy(data, msg, data.Length);

        for (int i = 0; i < data.Length; i++)
        {
            int factor = msg[i];
            if (factor == 0) continue;
            for (int j = 0; j < gen.Length; j++)           // use full generator
                msg[i + j] ^= GfMul((byte)factor, gen[j]); // start at i, not i+1
        }

        var ec = new byte[ecCount];
        Array.Copy(msg, data.Length, ec, 0, ecCount);
        return ec;
    }

    private static List<byte> PolyMul(List<byte> a, List<byte> b)
    {
        var r = new byte[a.Count + b.Count - 1];
        for (int i = 0; i < a.Count; i++)
            for (int j = 0; j < b.Count; j++)
                r[i + j] ^= GfMul(a[i], b[j]);
        return new List<byte>(r);
    }

    const int GF_POLY = 0x11D;
    const int GF_SIZE = 256;
    const int GF_GEN = 2;

    static byte GfMul(byte x, byte y)
    {
        int r = 0;
        while (y > 0)
        {
            if ((y & 1) != 0)
                r ^= x;
            y >>= 1;
            x = (byte)((x << 1) ^ ((x & 0x80) != 0 ? GF_POLY : 0));
        }
        return (byte)(r & 0xFF);
    }

    private static byte GfPow(byte a, int e)
    {
        byte r = 1;
        for (int i = 0; i < e; i++) r = GfMul(r, a);
        return r;
    }

    // ---------- Placement, masking, penalties ----------

    private static void PlaceData(bool[,] m, bool[,] res, byte[] codewords)
    {
        int n = m.GetLength(0);
        int totalBits = codewords.Length * 8;
        int bitIndex = 0;
        int dir = -1; // up first

        for (int col = n - 1; col > 0; col -= 2)
        {
            if (col == 6) col--; // skip timing column
            int rowStart = (dir < 0) ? n - 1 : 0;
            for (int i = 0; i < n; i++)
            {
                int r = rowStart + i * dir;
                for (int c = 0; c < 2; c++)
                {
                    int cc = col - c;
                    if (res[r, cc]) continue;
                    if (bitIndex < totalBits)
                    {
                        m[r, cc] = ((codewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                        bitIndex++;
                    }
                }
            }
            dir *= -1;
        }
    }

    private static int ChooseBestMask(bool[,] m, bool[,] reserved)
    {
        int bestMask = 0, bestPenalty = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            var masked = CloneAndMask(m, reserved, mask);
            int pen = Penalty(masked);
            if (pen < bestPenalty) { bestPenalty = pen; bestMask = mask; }
        }
        return bestMask;
    }

    private static bool[,] CloneAndMask(bool[,] src, bool[,] res, int mask)
    {
        int n = src.GetLength(0);
        var dst = new bool[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                bool v = src[r, c];
                if (!res[r, c] && Mask(mask, r, c)) v = !v;
                dst[r, c] = v;
            }
        return dst;
    }

    private static void ApplyMask(bool[,] m, bool[,] res, int mask)
    {
        int n = m.GetLength(0);
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                if (!res[r, c] && Mask(mask, r, c)) m[r, c] = !m[r, c];
    }

    private static bool Mask(int mask, int r, int c) => mask switch
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

    private static int Penalty(bool[,] m)
    {
        int n = m.GetLength(0);
        int total = 0;

        // N1: Runs
        for (int r = 0; r < n; r++)
        {
            int run = 1;
            for (int c = 1; c < n; c++)
            {
                if (m[r, c] == m[r, c - 1]) run++;
                else { if (run >= 5) total += 3 + (run - 5); run = 1; }
            }
            if (run >= 5) total += 3 + (run - 5);
        }
        for (int c = 0; c < n; c++)
        {
            int run = 1;
            for (int r = 1; r < n; r++)
            {
                if (m[r, c] == m[r - 1, c]) run++;
                else { if (run >= 5) total += 3 + (run - 5); run = 1; }
            }
            if (run >= 5) total += 3 + (run - 5);
        }

        // N2: 2x2 blocks
        for (int r = 0; r < n - 1; r++)
            for (int c = 0; c < n - 1; c++)
                if (m[r, c] == m[r, c + 1] && m[r, c] == m[r + 1, c] && m[r, c] == m[r + 1, c + 1])
                    total += 3;

        // N3: Finder-like patterns
        int[] p1 = { 1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0 };
        int[] p2 = { 0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1 };
        for (int r = 0; r < n; r++)
            for (int c = 0; c <= n - 11; c++)
                if (MatchRow(m, r, c, p1) || MatchRow(m, r, c, p2)) total += 40;
        for (int c = 0; c < n; c++)
            for (int r = 0; r <= n - 11; r++)
                if (MatchCol(m, r, c, p1) || MatchCol(m, r, c, p2)) total += 40;

        // N4: Balance
        int dark = 0;
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                if (m[r, c]) dark++;
        int percent = (dark * 100 + (n * n / 2)) / (n * n);
        total += (Math.Abs(percent - 50) / 5) * 10;

        return total;
    }

    private static bool MatchRow(bool[,] m, int r, int c, int[] pat)
    {
        for (int k = 0; k < pat.Length; k++) if ((m[r, c + k] ? 1 : 0) != pat[k]) return false;
        return true;
    }
    private static bool MatchCol(bool[,] m, int r, int c, int[] pat)
    {
        for (int k = 0; k < pat.Length; k++) if ((m[r + k, c] ? 1 : 0) != pat[k]) return false;
        return true;
    }

    // ---------- Format & Version info ----------

    private static void WriteFormatInfo(bool[,] m, bool[,] res, RadzenQREcc ecc, int mask)
    {
        // ECL bits: L=01, M=00, Q=11, H=10
        int ecl = ecc switch { RadzenQREcc.Low => 1, RadzenQREcc.Medium => 0, RadzenQREcc.Quartile => 3, RadzenQREcc.High => 2, _ => 0 };
        int data = (ecl << 3) | (mask & 7);

        // BCH(15,5) with generator 0x537, then XOR with 0x5412 (as you computed)
        int fmt = BchEncode(data, 0x537, 15, 5) ^ 0x5412;

        bool GetBit(int i) => ((fmt >> (14 - i)) & 1) != 0; // i=0 is MSB

        int n = m.GetLength(0);

        // ---- First copy (around TL finder/timing), bits 0..14 ----
        // row 8, cols 0..5 (bits 0..5)
        for (int i = 0; i <= 5; i++) Set(m, res, 8, i, GetBit(i));
        // row 8, col 7 (bit 6)
        Set(m, res, 8, 7, GetBit(6));
        // row 8, col 8 (bit 7)
        Set(m, res, 8, 8, GetBit(7));
        // row 7, col 8 (bit 8)
        Set(m, res, 7, 8, GetBit(8));
        // rows 5..0, col 8 (bits 9..14)
        for (int i = 9; i <= 14; i++) Set(m, res, 14 - i, 8, GetBit(i));

        // ---- Second copy (right/bottom), bits 0..14 again ----
        // col 8, rows n-1..n-7 (bits 0..6)
        for (int i = 0; i <= 6; i++) Set(m, res, n - 1 - i, 8, GetBit(i));
        // row 8, cols n-8..n-1 (bits 7..14)
        for (int i = 7; i <= 14; i++) Set(m, res, 8, n - 15 + i, GetBit(i));
    }

    private static int BchEncode(int data, int gen, int totalBits, int dataBits)
    {
        int d = data << (totalBits - dataBits);
        for (int i = totalBits - 1; i >= (totalBits - dataBits); i--)
            if (((d >> i) & 1) != 0) d ^= gen << (i - (totalBits - dataBits));
        return (data << (totalBits - dataBits)) | (d & ((1 << (totalBits - dataBits)) - 1));
    }

    private static void Set(bool[,] m, bool[,] res, int y, int x, bool v) { m[y, x] = v; res[y, x] = true; }

    // ---------- Tables ----------

    // Alignment pattern positions per version. For v1: none.
    private static readonly int[][] AlignPos = BuildAlignmentPositions();
    private static int[] AlignmentPatternPositions(int ver) => AlignPos[ver];

    private static int[][] BuildAlignmentPositions()
    {
        // From QR spec table (compressed).
        // Each row is the list of center positions for that version.
        // v1 has no alignment patterns.
        return new int[][]
        {
            Array.Empty<int>(),
            Array.Empty<int>(),
            new[]{6,18},
            new[]{6,22},
            new[]{6,26},
            new[]{6,30},
            new[]{6,34},
            new[]{6,22,38},
            new[]{6,24,42},
            new[]{6,26,46},
            new[]{6,28,50},
            new[]{6,30,54},
            new[]{6,32,58},
            new[]{6,34,62},
            new[]{6,26,46,66},
            new[]{6,26,48,70},
            new[]{6,26,50,74},
            new[]{6,30,54,78},
            new[]{6,30,56,82},
            new[]{6,30,58,86},
            new[]{6,34,62,90},
            new[]{6,28,50,72,94},
            new[]{6,26,50,74,98},
            new[]{6,30,54,78,102},
            new[]{6,28,54,80,106},
            new[]{6,32,58,84,110},
            new[]{6,30,58,86,114},
            new[]{6,34,62,90,118},
            new[]{6,26,50,74,98,122},
            new[]{6,30,54,78,102,126},
            new[]{6,26,52,78,104,130},
            new[]{6,30,56,82,108,134},
            new[]{6,34,60,86,112,138},
            new[]{6,30,58,86,114,142},
            new[]{6,34,62,90,118,146},
            new[]{6,30,54,78,102,126,150},
            new[]{6,24,50,76,102,128,154},
            new[]{6,28,54,80,106,132,158},
            new[]{6,32,58,84,110,136,162},
            new[]{6,26,54,82,110,138,166},
            new[]{6,30,58,86,114,142,170}
        };
    }

    // Error correction parameters for each version & ECC:
    // Returns (totalDataCw, ecPerBlock, grp1Blocks, grp1DataCw, grp2Blocks, grp2DataCw)
    static (int totalDataCw, int ecPerBlock, int grp1Blocks, int grp1DataCw, int grp2Blocks, int grp2DataCw)
        EcParams(int ver, RadzenQREcc ecc)
    {
        if (ver < 1 || ver > 40)
            throw new ArgumentOutOfRangeException(nameof(ver), "Version must be 1..40");

        int eccIndex = ecc switch
        {
            RadzenQREcc.Low => 0, // L
            RadzenQREcc.Medium => 1, // M
            RadzenQREcc.Quartile => 2, // Q
            RadzenQREcc.High => 3, // H
            _ => throw new ArgumentOutOfRangeException(nameof(ecc))
        };

        // EcTable[ver-1][eccIndex] = { ecPerBlock, g1Blocks, g1DataCw, g2Blocks, g2DataCw }
        var row = EcTable[ver - 1][eccIndex];

        int ecPerBlock = row[0];
        int grp1Blocks = row[1];
        int grp1DataCw = row[2];
        int grp2Blocks = row[3];
        int grp2DataCw = row[4];

        int totalDataCw = grp1Blocks * grp1DataCw + grp2Blocks * grp2DataCw;
        return (totalDataCw, ecPerBlock, grp1Blocks, grp1DataCw, grp2Blocks, grp2DataCw);
    }

    private static readonly int[][][] EcTable = BuildEcTable();

    private static int[][][] BuildEcTable()
    {
        // To keep this file reasonable, the table is still large but compact.
        // Format per version:
        // {
        //   L: {ecPerBlock, g1Blocks, g1DataCw, g2Blocks, g2DataCw},
        //   M: {...},
        //   Q: {...},
        //   H: {...}
        // }
        // Source: standard QR capacity tables (ISO/IEC 18004:2015), widely reproduced.
        // NOTE: These entries are carefully transcribed. If anything seems off in your tests, ping me.

        // Due to message size constraints, the full 40-version table is included but trimmed for readability here.
        // (It’s still complete.)
        return new int[][][]
        {
            // V1
            new[]{
                new[]{7,1,19,0,0},   // L: 1 block, 19 data cw, 7 ec per block
                new[]{10,1,16,0,0},  // M: 1x16
                new[]{13,1,13,0,0},  // Q: 1x13
                new[]{17,1,9,0,0},   // H: 1x9
            },
            // V2
            new[]{
                new[]{10,1,34,0,0},
                new[]{16,1,28,0,0},
                new[]{22,1,22,0,0},
                new[]{28,1,16,0,0},
            },
            // V3
            new[]{
                new[]{15,1,55,0,0},
                new[]{26,1,44,0,0},
                new[]{18,2,17,0,0}, // 2 blocks of 17 (still single group in spec presentation)
                new[]{22,2,13,0,0},
            },
            // V4
            new[]{
                new[]{20,1,80,0,0},
                new[]{18,2,32,0,0}, // 2x32
                new[]{26,2,24,0,0},
                new[]{16,4,9,0,0},  // 4x9
            },
            // V5
            new[]{
                new[]{26,1,108,0,0},
                new[]{24,2,43,0,0},
                new[]{18,2,15,2,16}, // (2x15,2x16)
                new[]{22,2,11,2,12},
            },
            // V6
            new[]{
                new[]{18,2,68,0,0}, // 2x68
                new[]{16,4,27,0,0},
                new[]{24,4,19,0,0},
                new[]{28,4,15,0,0},
            },
            // V7
            new[]{
                new[]{20,2,78,0,0},
                new[]{18,4,31,0,0},
                new[]{18,2,14,4,15},
                new[]{26,4,13,1,14},
            },
            // V8
            new[]{
                new[]{24,2,97,0,0},
                new[]{22,2,38,2,39},
                new[]{22,4,18,2,19},
                new[]{26,4,14,2,15},
            },
            // V9
            new[]{
                new[]{30,2,116,0,0},
                new[]{22,3,36,2,37},
                new[]{20,4,16,4,17},
                new[]{24,4,12,4,13},
            },
            // V10
            new[]{
                new[]{18,2,68,2,69},
                new[]{26,4,43,1,44},
                new[]{24,6,19,2,20},
                new[]{28,6,15,2,16},
            },
            // V11
            new[]{
                new[]{20,4,81,0,0},
                new[]{30,1,50,4,51},
                new[]{28,4,22,4,23},
                new[]{24,3,12,8,13},
            },
            // V12
            new[]{
                new[]{24,2,92,2,93},
                new[]{22,6,36,2,37},
                new[]{26,4,20,6,21},
                new[]{28,7,14,4,15},
            },
            // V13
            new[]{
                new[]{26,4,107,0,0},
                new[]{22,8,37,1,38},
                new[]{24,8,20,4,21},
                new[]{22,12,11,4,12},
            },
            // V14
            new[]{
                new[]{30,3,115,1,116},
                new[]{24,4,40,5,41},
                new[]{20,11,16,5,17},
                new[]{24,11,12,5,13},
            },
            // V15
            new[]{
                new[]{22,5,87,1,88},
                new[]{24,5,41,5,42},
                new[]{30,5,24,7,25},
                new[]{24,11,12,7,13},
            },
            // V16
            new[]{
                new[]{24,5,98,1,99},
                new[]{28,7,45,3,46},
                new[]{24,15,19,2,20},
                new[]{30,3,15,13,16},
            },
            // V17
            new[]{
                new[]{28,1,107,5,108},
                new[]{28,10,46,1,47},
                new[]{28,1,22,15,23},
                new[]{28,2,14,17,15},
            },
            // V18
            new[]{
                new[]{30,5,120,1,121},
                new[]{26,9,43,4,44},
                new[]{28,17,22,1,23},
                new[]{28,2,14,19,15},
            },
            // V19
            new[]{
                new[]{28,3,113,4,114},
                new[]{26,3,44,11,45},
                new[]{26,17,21,4,22},
                new[]{26,9,13,16,14},
            },
            // V20
            new[]{
                new[]{28,3,107,5,108},
                new[]{26,3,41,13,42},
                new[]{30,15,24,5,25},
                new[]{28,15,15,10,16},
            },
            // V21
            new[]{
                new[]{28,4,116,4,117},
                new[]{26,17,42,0,0},
                new[]{28,17,22,6,23},
                new[]{30,19,16,6,17},
            },
            // V22
            new[]{
                new[]{28,2,111,7,112},
                new[]{28,17,46,0,0},
                new[]{30,7,24,16,25},
                new[]{24,34,13,0,0},
            },
            // V23
            new[]{
                new[]{30,4,121,5,122},
                new[]{28,4,47,14,48},
                new[]{30,11,24,14,25},
                new[]{30,16,15,14,16},
            },
            // V24
            new[]{
                new[]{30,6,117,4,118},
                new[]{28,6,45,14,46},
                new[]{30,11,24,16,25},
                new[]{30,30,16,2,17},
            },
            // V25
            new[]{
                new[]{26,8,106,4,107},
                new[]{28,8,47,13,48},
                new[]{30,7,24,22,25},
                new[]{30,22,15,13,16},
            },
            // V26
            new[]{
                new[]{28,10,114,2,115},
                new[]{28,19,46,4,47},
                new[]{28,28,22,6,23},
                new[]{30,33,16,4,17},
            },
            // V27
            new[]{
                new[]{30,8,122,4,123},
                new[]{28,22,45,3,46},
                new[]{30,8,23,26,24},
                new[]{30,12,15,28,16},
            },
            // V28
            new[]{
                new[]{30,3,117,10,118},
                new[]{28,3,45,23,46},
                new[]{30,4,24,31,25},
                new[]{30,11,15,31,16},
            },
            // V29
            new[]{
                new[]{30,7,116,7,117},
                new[]{28,21,45,7,46},
                new[]{30,1,23,37,24},
                new[]{30,19,15,26,16},
            },
            // V30
            new[]{
                new[]{30,5,115,10,116},
                new[]{28,19,47,10,48},
                new[]{30,15,24,25,25},
                new[]{30,23,15,25,16},
            },
            // V31
            new[]{
                new[]{30,13,115,3,116},
                new[]{28,2,46,29,47},
                new[]{30,42,24,1,25},
                new[]{30,23,15,28,16},
            },
            // V32
            new[]{
                new[]{30,17,115,0,0},
                new[]{28,10,46,23,47},
                new[]{30,10,24,35,25},
                new[]{30,19,15,35,16},
            },
            // V33
            new[]{
                new[]{30,17,115,1,116},
                new[]{28,14,46,21,47},
                new[]{30,29,24,19,25},
                new[]{30,11,15,46,16},
            },
            // V34
            new[]{
                new[]{30,13,115,6,116},
                new[]{28,14,46,23,47},
                new[]{30,44,24,7,25},
                new[]{30,59,16,1,17},
            },
            // V35
            new[]{
                new[]{30,12,121,7,122},
                new[]{28,12,47,26,48},
                new[]{30,39,24,14,25},
                new[]{30,22,15,41,16},
            },
            // V36
            new[]{
                new[]{30,6,121,14,122},
                new[]{28,6,47,34,48},
                new[]{30,46,24,10,25},
                new[]{30,2,15,64,16},
            },
            // V37
            new[]{
                new[]{30,17,122,4,123},
                new[]{28,29,46,14,47},
                new[]{30,49,24,10,25},
                new[]{30,24,15,46,16},
            },
            // V38
            new[]{
                new[]{30,4,122,18,123},
                new[]{28,13,46,32,47},
                new[]{30,48,24,14,25},
                new[]{30,42,15,32,16},
            },
            // V39
            new[]{
                new[]{30,20,117,4,118},
                new[]{28,40,47,7,48},
                new[]{30,43,24,22,25},
                new[]{30,10,15,67,16},
            },
            // V40
            new[]{
                new[]{30,19,118,6,119},
                new[]{28,18,47,31,48},
                new[]{30,34,24,34,25},
                new[]{30,20,15,61,16},
            },
        };
    }

    // ---------- Helpers ----------

    private sealed class BitBuffer : List<int>
    {
        public int Length => Count;
        public void AppendBits(int val, int len)
        {
            for (int i = len - 1; i >= 0; i--) this.Add((val >> i) & 1);
        }
    }
}
