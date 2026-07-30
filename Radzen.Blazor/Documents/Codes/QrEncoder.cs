using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Codes;

/// <summary>
/// Error correction.
/// </summary>
public enum QrErrorCorrection 
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

/// <summary>
/// QR code module shape.
/// </summary>
public enum QrModuleShape
{
    /// <summary>
    /// Square.
    /// </summary>
    Square,
    /// <summary>
    /// Rounded.
    /// </summary>
    Rounded,
    /// <summary>
    /// Circle.
    /// </summary>
    Circle
}

/// <summary>
/// QR code eye shape.
/// </summary>
public enum QrEyeShape
{
    /// <summary>
    /// Square.
    /// </summary>
    Square,
    /// <summary>
    /// Rounded.
    /// </summary>
    Rounded,
    /// <summary>
    /// Framed.
    /// </summary>
    Framed
}

/// <summary>
/// Provides QR encoding utilities for UTF-8 strings and raw bytes.
/// </summary>
public static class QrEncoder
{
    // ISO/IEC 18004:2015 - byte mode indicator.
    private const int ByteModeIndicator = 0b0100;
    private const int ModeIndicatorBits = 4;
    private const int TerminatorBits = 4;
    private const int QuietZoneModules = 4;
    private const int TimingLine = 6;

    // ISO/IEC 18004:2015 - character count indicator length for byte mode.
    private static int ByteModeCharacterCountBits(int version) => version <= 9 ? 8 : 16;

    /// <summary>Encode a UTF-8 string into a QR module matrix.</summary>
    public static bool[,] EncodeUtf8(string value, QrErrorCorrection ecc, int minVersion = 1, int maxVersion = 40)
        => EncodeBytes(Encoding.UTF8.GetBytes(value ?? string.Empty), ecc, minVersion, maxVersion);

    /// <summary>Encode raw bytes into a QR module matrix.</summary>
    public static bool[,] EncodeBytes(byte[] data, QrErrorCorrection ecc = QrErrorCorrection.Medium, int minVersion = 1, int maxVersion = 40)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (minVersion < 1 || maxVersion > 40 || minVersion > maxVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(minVersion), "Version range must be within 1..40");
        }

        for (int ver = minVersion; ver <= maxVersion; ver++)
        {
            int charCountBits = ByteModeCharacterCountBits(ver);
            var bb = new BitBuffer();
            bb.AppendBits(ByteModeIndicator, ModeIndicatorBits);
            bb.AppendBits(data.Length, charCountBits);
            foreach (byte b in data)
            {
                bb.AppendBits(b, 8);
            }

            var (dataCw, ecPerBlock, grp1Blocks, grp1DataCw, grp2Blocks, grp2DataCw) = EcParams(ver, ecc);
            int capacityBits = dataCw * 8;

            if (bb.Length > capacityBits)
            {
                continue;
            }

            int needed = bb.Length + Math.Min(TerminatorBits, capacityBits - bb.Length);
            needed += (8 - (needed % 8)) % 8;

            if (needed > capacityBits)
            {
                continue;
            }

            var dataCwBytes = BuildDataCodewords(bb, dataCw);
            var blocks = BuildBlocks(dataCwBytes, ecPerBlock, grp1Blocks, grp1DataCw, grp2Blocks, grp2DataCw);
            var final = Interleave(blocks);

            var (m, reserved) = BuildBaseMatrix(ver);
            PlaceData(m, reserved, final);

            int bestMask = ChooseBestMask(m, reserved);
            ApplyMask(m, reserved, bestMask);
            WriteFormatInfo(m, reserved, ecc, bestMask);
            if (ver >= 7)
            {
                WriteVersionInfo(m, reserved, ver);
            }

            return m;
        }

        throw new ArgumentException($"Data too long for versions {minVersion}..{maxVersion} at ECC={ecc}.");
    }

    /// <summary>Render a module matrix into an SVG string with a 4-module quiet zone.</summary>
    /// <exception cref="ArgumentException">A color argument is not a valid CSS color.</exception>
    public static string ToSvg(
        bool[,] modules,
        int moduleSize = 8,
        string foreground = "#000000",
        string background = "#FFFFFF",
        QrModuleShape moduleShape = QrModuleShape.Square,
        QrEyeShape eyeShape = QrEyeShape.Square,
        QrEyeShape? eyeShapeTopLeft = null,
        QrEyeShape? eyeShapeTopRight = null,
        QrEyeShape? eyeShapeBottomLeft = null,
        string? eyeColor = null,
        string? eyeColorTopLeft = null,
        string? eyeColorTopRight = null,
        string? eyeColorBottomLeft = null,
        string? image = null,
        string imageBackground = "#FFF")
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(foreground);
        ArgumentNullException.ThrowIfNull(background);
        ArgumentNullException.ThrowIfNull(imageBackground);

        var moduleFill = SvgAttributes.Color(foreground, nameof(foreground));
        var imageBackgroundFill = SvgAttributes.Color(imageBackground, nameof(imageBackground));
        var imageHref = image is null ? null : SvgAttributes.Href(image, nameof(image));

        int n = modules.GetLength(0);
        int vb = n + 2 * QuietZoneModules;
        int px = vb * moduleSize;

        var sb = new StringBuilder(n * n + 1024);
        sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{px}\" height=\"{px}\" viewBox=\"0 0 {vb} {vb}\" shape-rendering=\"crispEdges\">");
        var backgroundFill = GetSvgFillParts(background, nameof(background));
        sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"0\" y=\"0\" width=\"{vb}\" height=\"{vb}\" fill=\"{backgroundFill.Color}\" fill-opacity=\"{Format(backgroundFill.Opacity)}\"/>");

        var baseEyeFill = eyeColor is null ? moduleFill : SvgAttributes.Color(eyeColor, nameof(eyeColor));
        AppendEye(sb, QuietZoneModules, QuietZoneModules, eyeShapeTopLeft ?? eyeShape, EyeFill(eyeColorTopLeft, baseEyeFill, nameof(eyeColorTopLeft)), "eye-mask-0");
        AppendEye(sb, vb - 11, QuietZoneModules, eyeShapeTopRight ?? eyeShape, EyeFill(eyeColorTopRight, baseEyeFill, nameof(eyeColorTopRight)), "eye-mask-1");
        AppendEye(sb, QuietZoneModules, vb - 11, eyeShapeBottomLeft ?? eyeShape, EyeFill(eyeColorBottomLeft, baseEyeFill, nameof(eyeColorBottomLeft)), "eye-mask-2");

        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                if (!modules[r, c])
                {
                    continue;
                }

                if (IsFinderCell(r, c, n))
                {
                    continue;
                }

                var x = c + QuietZoneModules;
                var y = r + QuietZoneModules;

                if (moduleShape == QrModuleShape.Circle)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{Format(x + 0.5)}\" cy=\"{Format(y + 0.5)}\" r=\"0.5\" fill=\"{moduleFill}\"/>");
                }
                else if (moduleShape == QrModuleShape.Rounded)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"1\" height=\"1\" rx=\"0.25\" ry=\"0.25\" fill=\"{moduleFill}\"/>");
                }
                else
                {
                    sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"1\" height=\"1\" fill=\"{moduleFill}\"/>");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(image))
        {
            const double imageSizePercent = 20;
            const double imagePaddingModules = 1.0;
            const double imageCornerRadius = 0.75;
            const double imageBackgroundOpacity = 1.0;

            double boxModules = Math.Max(5, Math.Round(n * (imageSizePercent / 100.0)));

            double cutoutW = boxModules + 2 * imagePaddingModules;
            double cutoutH = boxModules + 2 * imagePaddingModules;

            double centerX = vb / 2.0;
            double centerY = vb / 2.0;

            double cutoutX = centerX - cutoutW / 2.0;
            double cutoutY = centerY - cutoutH / 2.0;
            double imgX = centerX - boxModules / 2.0;
            double imgY = centerY - boxModules / 2.0;

            sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(cutoutX)}\" y=\"{Format(cutoutY)}\" width=\"{Format(cutoutW)}\" height=\"{Format(cutoutH)}\" rx=\"{Format(imageCornerRadius)}\" ry=\"{Format(imageCornerRadius)}\" fill=\"{imageBackgroundFill}\" fill-opacity=\"{Format(Math.Clamp(imageBackgroundOpacity, 0, 1))}\"/>");
            sb.Append(CultureInfo.InvariantCulture, $"<image x=\"{Format(imgX)}\" y=\"{Format(imgY)}\" width=\"{Format(boxModules)}\" height=\"{Format(boxModules)}\" preserveAspectRatio=\"xMidYMid meet\" href=\"{imageHref}\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static (bool[,] m, bool[,] res) BuildBaseMatrix(int ver)
    {
        int n = 21 + 4 * (ver - 1);
        var m = new bool[n, n];
        var reserved = new bool[n, n];

        DrawFinder(m, reserved, 0, 0);
        DrawFinder(m, reserved, n - 7, 0);
        DrawFinder(m, reserved, 0, n - 7);

        DrawTiming(m, reserved);
        DrawAlignmentPatterns(m, reserved, ver);
        DrawDarkModule(m, reserved, ver);

        ReserveFormat(reserved);

        if (ver >= 7)
        {
            ReserveVersion(reserved);
        }

        return (m, reserved);
    }

    private static void DrawTiming(bool[,] m, bool[,] res)
    {
        int n = m.GetLength(0);
        for (int i = 8; i < n - 8; i++)
        {
            m[TimingLine, i] = (i % 2) == 0; res[TimingLine, i] = true;
            m[i, TimingLine] = (i % 2) == 0; res[i, TimingLine] = true;
        }
    }

    private static void DrawAlignmentPatterns(bool[,] m, bool[,] res, int ver)
    {
        int n = m.GetLength(0);
        var positions = AlignmentPatternPositions(ver);
        foreach (int y in positions)
        {
            foreach (int x in positions)
            {
                bool overlapsFinder = (x < 9 && y < 9) || (x > n - 9 && y < 9) || (x < 9 && y > n - 9);
                if (!overlapsFinder)
                {
                    DrawAlignment(m, res, x, y);
                }
            }
        }
    }

    private static void DrawDarkModule(bool[,] m, bool[,] res, int ver)
    {
        m[4 * ver + 9, 8] = true;
        res[4 * ver + 9, 8] = true;
    }

    private static void DrawFinder(bool[,] m, bool[,] res, int x, int y)
    {
        for (int r = -1; r <= 7; r++)
        {
            for (int c = -1; c <= 7; c++)
            {
                int rr = y + r, cc = x + c;
                if (rr < 0 || cc < 0 || rr >= m.GetLength(0) || cc >= m.GetLength(1))
                {
                    continue;
                }

                bool in7 = r >= 0 && r < 7 && c >= 0 && c < 7;
                if (in7)
                {
                    bool on = r == 0 || r == 6 || c == 0 || c == 6 || (r >= 2 && r <= 4 && c >= 2 && c <= 4);
                    m[rr, cc] = on;
                    res[rr, cc] = true;
                }
                else
                {
                    res[rr, cc] = true;
                }
            }
        }
    }

    private static void DrawAlignment(bool[,] m, bool[,] res, int cx, int cy)
    {
        for (int r = -2; r <= 2; r++)
        {
            for (int c = -2; c <= 2; c++)
            {
                int rr = cy + r, cc = cx + c;
                m[rr, cc] = Math.Max(Math.Abs(r), Math.Abs(c)) != 1;
                res[rr, cc] = true;
            }
        }
    }

    private static void ReserveFormat(bool[,] res)
    {
        int n = res.GetLength(0);

        for (int c = 0; c <= 5; c++)
        {
            res[8, c] = true;
        }

        res[8, 7] = true;
        res[8, 8] = true;

        for (int r = 0; r <= 5; r++)
        {
            res[r, 8] = true;
        }

        res[7, 8] = true;

        for (int i = 0; i < 7; i++)
        {
            res[n - 1 - i, 8] = true;
        }

        for (int i = 0; i < 8; i++)
        {
            res[8, n - 8 + i] = true;
        }
    }

    private static void ReserveVersion(bool[,] res)
    {
        int n = res.GetLength(0);

        for (int r = n - 11; r <= n - 9; r++)
        {
            for (int c = 0; c <= 5; c++)
            {
                res[r, c] = true;
            }
        }

        for (int r = 0; r <= 5; r++)
        {
            for (int c = n - 11; c <= n - 9; c++)
            {
                res[r, c] = true;
            }
        }
    }

    private static void WriteVersionInfo(bool[,] m, bool[,] res, int ver)
    {
        // ISO/IEC 18004:2015 - version information is BCH(18,6) with generator 0x1F25, least significant bit first.
        int bits = BchEncode(ver, 0x1F25, 18, 6);
        int n = m.GetLength(0);

        for (int col = 0; col < 6; col++)
        {
            for (int row = 0; row < 3; row++)
            {
                int bitIndex = row + col * 3;
                bool bit = ((bits >> bitIndex) & 1) != 0;

                Set(m, res, n - 11 + row, col, bit);
            }
        }

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int bitIndex = row * 3 + col;
                bool bit = ((bits >> bitIndex) & 1) != 0;

                Set(m, res, row, n - 11 + col, bit);
            }
        }
    }

    private static byte[] BuildDataCodewords(BitBuffer bb, int dataCw)
    {
        int totalBits = dataCw * 8;
        int remaining = totalBits - bb.Length;
        bb.AppendBits(0, Math.Min(4, remaining));
        while (bb.Length % 8 != 0)
        {
            bb.AppendBits(0, 1);
        }
        var bytes = new List<byte>(dataCw);
        for (int i = 0; i < bb.Length; i += 8)
        {
            if (bytes.Count == dataCw)
            {
                break;
            }

            int b = 0;
            for (int j = 0; j < 8; j++)
            {
                b = (b << 1) | bb[i + j];
            }

            bytes.Add((byte)b);
        }
        byte[] pads = { 0xEC, 0x11 }; int p = 0;
        while (bytes.Count < dataCw)
        {
            bytes.Add(pads[(p++) & 1]);
        }

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
        int totalLen = 0;
        int maxData = 0, maxEc = 0;
        foreach (var b in blocks)
        {
            totalLen += b.Data.Length + b.Ec.Length;
            if (b.Data.Length > maxData)
            {
                maxData = b.Data.Length;
            }

            if (b.Ec.Length > maxEc)
            {
                maxEc = b.Ec.Length;
            }
        }
        var r = new byte[totalLen];
        int k = 0;
        for (int i = 0; i < maxData; i++)
        {
            foreach (var b in blocks)
            {
                if (i < b.Data.Length)
                {
                    r[k++] = b.Data[i];
                }
            }
        }

        for (int i = 0; i < maxEc; i++)
        {
            foreach (var b in blocks)
            {
                if (i < b.Ec.Length)
                {
                    r[k++] = b.Ec[i];
                }
            }
        }

        return r;
    }

    private static byte[] RsGenerator(int ecCount)
    {
        var gen = new List<byte> { 1 };
        for (int i = 0; i < ecCount; i++)
        {
            gen = PolyMul(gen, new List<byte> { 1, GfPow(2, i) });
        }

        return gen.ToArray();
    }

    private static byte[] ReedSolomon(byte[] data, int ecCount)
    {
        var gen = RsGenerator(ecCount);
        var msg = new byte[data.Length + ecCount];
        Array.Copy(data, msg, data.Length);

        for (int i = 0; i < data.Length; i++)
        {
            int factor = msg[i];
            if (factor == 0)
            {
                continue;
            }

            for (int j = 0; j < gen.Length; j++)
            {
                msg[i + j] ^= GfMul((byte)factor, gen[j]);
            }
        }

        var ec = new byte[ecCount];
        Array.Copy(msg, data.Length, ec, 0, ecCount);
        return ec;
    }

    private static List<byte> PolyMul(List<byte> a, List<byte> b)
    {
        var r = new byte[a.Count + b.Count - 1];
        for (int i = 0; i < a.Count; i++)
        {
            for (int j = 0; j < b.Count; j++)
            {
                r[i + j] ^= GfMul(a[i], b[j]);
            }
        }

        return new List<byte>(r);
    }

    private const int GF_POLY = 0x11D;
    private static byte GfMul(byte x, byte y)
    {
        int r = 0;
        while (y > 0)
        {
            if ((y & 1) != 0)
            {
                r ^= x;
            }

            y >>= 1;
            x = (byte)((x << 1) ^ ((x & 0x80) != 0 ? GF_POLY : 0));
        }
        return (byte)(r & 0xFF);
    }

    private static byte GfPow(byte a, int e)
    {
        byte r = 1;
        for (int i = 0; i < e; i++)
        {
            r = GfMul(r, a);
        }

        return r;
    }

    private static void PlaceData(bool[,] m, bool[,] res, byte[] codewords)
    {
        int n = m.GetLength(0);
        int totalBits = codewords.Length * 8;
        int bitIndex = 0;
        const int upward = -1;
        int direction = upward;

        for (int col = n - 1; col > 0; col -= 2)
        {
            if (col == TimingLine)
            {
                col--;
            }

            int rowStart = (direction < 0) ? n - 1 : 0;
            for (int i = 0; i < n; i++)
            {
                int r = rowStart + i * direction;
                for (int c = 0; c < 2; c++)
                {
                    int cc = col - c;
                    if (res[r, cc])
                    {
                        continue;
                    }

                    if (bitIndex < totalBits)
                    {
                        m[r, cc] = ((codewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                        bitIndex++;
                    }
                }
            }
            direction *= -1;
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
        {
            for (int c = 0; c < n; c++)
            {
                bool v = src[r, c];
                if (!res[r, c] && Mask(mask, r, c))
                {
                    v = !v;
                }

                dst[r, c] = v;
            }
        }

        return dst;
    }

    private static void ApplyMask(bool[,] m, bool[,] res, int mask)
    {
        int n = m.GetLength(0);
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                if (!res[r, c] && Mask(mask, r, c))
                {
                    m[r, c] = !m[r, c];
                }
            }
        }
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
        => AdjacentRunPenalty(m) + BlockPenalty(m) + FinderLikePenalty(m) + DarkBalancePenalty(m);

    private static int AdjacentRunPenalty(bool[,] m)
    {
        int n = m.GetLength(0);
        int total = 0;

        for (int r = 0; r < n; r++)
        {
            int run = 1;
            for (int c = 1; c < n; c++)
            {
                if (m[r, c] == m[r, c - 1])
                {
                    run++;
                }
                else { if (run >= 5) { total += 3 + (run - 5); } run = 1; }
            }
            if (run >= 5)
            {
                total += 3 + (run - 5);
            }
        }
        for (int c = 0; c < n; c++)
        {
            int run = 1;
            for (int r = 1; r < n; r++)
            {
                if (m[r, c] == m[r - 1, c])
                {
                    run++;
                }
                else { if (run >= 5) { total += 3 + (run - 5); } run = 1; }
            }
            if (run >= 5)
            {
                total += 3 + (run - 5);
            }
        }

        return total;
    }

    private static int BlockPenalty(bool[,] m)
    {
        int n = m.GetLength(0);
        int total = 0;

        for (int r = 0; r < n - 1; r++)
        {
            for (int c = 0; c < n - 1; c++)
            {
                if (m[r, c] == m[r, c + 1] && m[r, c] == m[r + 1, c] && m[r, c] == m[r + 1, c + 1])
                {
                    total += 3;
                }
            }
        }

        return total;
    }

    private static int FinderLikePenalty(bool[,] m)
    {
        int n = m.GetLength(0);
        int total = 0;

        int[] p1 = { 1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0 };
        int[] p2 = { 0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1 };
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c <= n - 11; c++)
            {
                if (MatchRow(m, r, c, p1) || MatchRow(m, r, c, p2))
                {
                    total += 40;
                }
            }
        }

        for (int c = 0; c < n; c++)
        {
            for (int r = 0; r <= n - 11; r++)
            {
                if (MatchCol(m, r, c, p1) || MatchCol(m, r, c, p2))
                {
                    total += 40;
                }
            }
        }

        return total;
    }

    private static int DarkBalancePenalty(bool[,] m)
    {
        int n = m.GetLength(0);

        int dark = 0;
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                if (m[r, c])
                {
                    dark++;
                }
            }
        }

        int percent = (dark * 100 + (n * n / 2)) / (n * n);
        return (Math.Abs(percent - 50) / 5) * 10;
    }

    private static bool MatchRow(bool[,] m, int r, int c, int[] pat)
    {
        for (int k = 0; k < pat.Length; k++)
        {
            if ((m[r, c + k] ? 1 : 0) != pat[k])
            {
                return false;
            }
        }

        return true;
    }
    private static bool MatchCol(bool[,] m, int r, int c, int[] pat)
    {
        for (int k = 0; k < pat.Length; k++)
        {
            if ((m[r + k, c] ? 1 : 0) != pat[k])
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteFormatInfo(bool[,] m, bool[,] res, QrErrorCorrection ecc, int mask)
    {
        // ISO/IEC 18004:2015 - error correction level indicators L=01, M=00, Q=11, H=10.
        int ecl = ecc switch { QrErrorCorrection.Low => 1, QrErrorCorrection.Medium => 0, QrErrorCorrection.Quartile => 3, QrErrorCorrection.High => 2, _ => 0 };
        int data = (ecl << 3) | (mask & 7);

        // ISO/IEC 18004:2015 - format information is BCH(15,5) with generator 0x537, masked with 0x5412.
        int fmt = BchEncode(data, 0x537, 15, 5) ^ 0x5412;

        bool GetBitMsbFirst(int i) => ((fmt >> (14 - i)) & 1) != 0;

        int n = m.GetLength(0);

        for (int i = 0; i <= 5; i++)
        {
            Set(m, res, 8, i, GetBitMsbFirst(i));
        }

        Set(m, res, 8, 7, GetBitMsbFirst(6));
        Set(m, res, 8, 8, GetBitMsbFirst(7));
        Set(m, res, 7, 8, GetBitMsbFirst(8));

        for (int i = 9; i <= 14; i++)
        {
            Set(m, res, 14 - i, 8, GetBitMsbFirst(i));
        }

        for (int i = 0; i <= 6; i++)
        {
            Set(m, res, n - 1 - i, 8, GetBitMsbFirst(i));
        }

        for (int i = 7; i <= 14; i++)
        {
            Set(m, res, 8, n - 15 + i, GetBitMsbFirst(i));
        }
    }

    private static int BchEncode(int data, int gen, int totalBits, int dataBits)
    {
        int d = data << (totalBits - dataBits);
        for (int i = totalBits - 1; i >= (totalBits - dataBits); i--)
        {
            if (((d >> i) & 1) != 0)
            {
                d ^= gen << (i - (totalBits - dataBits));
            }
        }

        return (data << (totalBits - dataBits)) | (d & ((1 << (totalBits - dataBits)) - 1));
    }

    private static void Set(bool[,] m, bool[,] res, int y, int x, bool v) { m[y, x] = v; res[y, x] = true; }

    private static readonly int[][] AlignPos = BuildAlignmentPositions();
    private static int[] AlignmentPatternPositions(int ver) => AlignPos[ver];

    private static int[][] BuildAlignmentPositions()
    {
        // ISO/IEC 18004:2015 - alignment pattern centre coordinates, indexed by version.
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

    private static (int totalDataCw, int ecPerBlock, int grp1Blocks, int grp1DataCw, int grp2Blocks, int grp2DataCw)
        EcParams(int ver, QrErrorCorrection ecc)
    {
        if (ver < 1 || ver > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(ver), "Version must be 1..40");
        }

        int eccIndex = ecc switch
        {
            QrErrorCorrection.Low => 0,
            QrErrorCorrection.Medium => 1,
            QrErrorCorrection.Quartile => 2,
            QrErrorCorrection.High => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(ecc))
        };

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
        // ISO/IEC 18004:2015 - error correction characteristics, indexed by version then by error correction level,
        // each row holding { ecPerBlock, group1Blocks, group1DataCodewords, group2Blocks, group2DataCodewords }.
        return new int[][][]
        {
            new[]{
                new[]{7,1,19,0,0},
                new[]{10,1,16,0,0},
                new[]{13,1,13,0,0},
                new[]{17,1,9,0,0},
            },
            new[]{
                new[]{10,1,34,0,0},
                new[]{16,1,28,0,0},
                new[]{22,1,22,0,0},
                new[]{28,1,16,0,0},
            },
            new[]{
                new[]{15,1,55,0,0},
                new[]{26,1,44,0,0},
                new[]{18,2,17,0,0},
                new[]{22,2,13,0,0},
            },
            new[]{
                new[]{20,1,80,0,0},
                new[]{18,2,32,0,0},
                new[]{26,2,24,0,0},
                new[]{16,4,9,0,0},
            },
            new[]{
                new[]{26,1,108,0,0},
                new[]{24,2,43,0,0},
                new[]{18,2,15,2,16},
                new[]{22,2,11,2,12},
            },
            new[]{
                new[]{18,2,68,0,0},
                new[]{16,4,27,0,0},
                new[]{24,4,19,0,0},
                new[]{28,4,15,0,0},
            },
            new[]{
                new[]{20,2,78,0,0},
                new[]{18,4,31,0,0},
                new[]{18,2,14,4,15},
                new[]{26,4,13,1,14},
            },
            new[]{
                new[]{24,2,97,0,0},
                new[]{22,2,38,2,39},
                new[]{22,4,18,2,19},
                new[]{26,4,14,2,15},
            },
            new[]{
                new[]{30,2,116,0,0},
                new[]{22,3,36,2,37},
                new[]{20,4,16,4,17},
                new[]{24,4,12,4,13},
            },
            new[]{
                new[]{18,2,68,2,69},
                new[]{26,4,43,1,44},
                new[]{24,6,19,2,20},
                new[]{28,6,15,2,16},
            },
            new[]{
                new[]{20,4,81,0,0},
                new[]{30,1,50,4,51},
                new[]{28,4,22,4,23},
                new[]{24,3,12,8,13},
            },
            new[]{
                new[]{24,2,92,2,93},
                new[]{22,6,36,2,37},
                new[]{26,4,20,6,21},
                new[]{28,7,14,4,15},
            },
            new[]{
                new[]{26,4,107,0,0},
                new[]{22,8,37,1,38},
                new[]{24,8,20,4,21},
                new[]{22,12,11,4,12},
            },
            new[]{
                new[]{30,3,115,1,116},
                new[]{24,4,40,5,41},
                new[]{20,11,16,5,17},
                new[]{24,11,12,5,13},
            },
            new[]{
                new[]{22,5,87,1,88},
                new[]{24,5,41,5,42},
                new[]{30,5,24,7,25},
                new[]{24,11,12,7,13},
            },
            new[]{
                new[]{24,5,98,1,99},
                new[]{28,7,45,3,46},
                new[]{24,15,19,2,20},
                new[]{30,3,15,13,16},
            },
            new[]{
                new[]{28,1,107,5,108},
                new[]{28,10,46,1,47},
                new[]{28,1,22,15,23},
                new[]{28,2,14,17,15},
            },
            new[]{
                new[]{30,5,120,1,121},
                new[]{26,9,43,4,44},
                new[]{28,17,22,1,23},
                new[]{28,2,14,19,15},
            },
            new[]{
                new[]{28,3,113,4,114},
                new[]{26,3,44,11,45},
                new[]{26,17,21,4,22},
                new[]{26,9,13,16,14},
            },
            new[]{
                new[]{28,3,107,5,108},
                new[]{26,3,41,13,42},
                new[]{30,15,24,5,25},
                new[]{28,15,15,10,16},
            },
            new[]{
                new[]{28,4,116,4,117},
                new[]{26,17,42,0,0},
                new[]{28,17,22,6,23},
                new[]{30,19,16,6,17},
            },
            new[]{
                new[]{28,2,111,7,112},
                new[]{28,17,46,0,0},
                new[]{30,7,24,16,25},
                new[]{24,34,13,0,0},
            },
            new[]{
                new[]{30,4,121,5,122},
                new[]{28,4,47,14,48},
                new[]{30,11,24,14,25},
                new[]{30,16,15,14,16},
            },
            new[]{
                new[]{30,6,117,4,118},
                new[]{28,6,45,14,46},
                new[]{30,11,24,16,25},
                new[]{30,30,16,2,17},
            },
            new[]{
                new[]{26,8,106,4,107},
                new[]{28,8,47,13,48},
                new[]{30,7,24,22,25},
                new[]{30,22,15,13,16},
            },
            new[]{
                new[]{28,10,114,2,115},
                new[]{28,19,46,4,47},
                new[]{28,28,22,6,23},
                new[]{30,33,16,4,17},
            },
            new[]{
                new[]{30,8,122,4,123},
                new[]{28,22,45,3,46},
                new[]{30,8,23,26,24},
                new[]{30,12,15,28,16},
            },
            new[]{
                new[]{30,3,117,10,118},
                new[]{28,3,45,23,46},
                new[]{30,4,24,31,25},
                new[]{30,11,15,31,16},
            },
            new[]{
                new[]{30,7,116,7,117},
                new[]{28,21,45,7,46},
                new[]{30,1,23,37,24},
                new[]{30,19,15,26,16},
            },
            new[]{
                new[]{30,5,115,10,116},
                new[]{28,19,47,10,48},
                new[]{30,15,24,25,25},
                new[]{30,23,15,25,16},
            },
            new[]{
                new[]{30,13,115,3,116},
                new[]{28,2,46,29,47},
                new[]{30,42,24,1,25},
                new[]{30,23,15,28,16},
            },
            new[]{
                new[]{30,17,115,0,0},
                new[]{28,10,46,23,47},
                new[]{30,10,24,35,25},
                new[]{30,19,15,35,16},
            },
            new[]{
                new[]{30,17,115,1,116},
                new[]{28,14,46,21,47},
                new[]{30,29,24,19,25},
                new[]{30,11,15,46,16},
            },
            new[]{
                new[]{30,13,115,6,116},
                new[]{28,14,46,23,47},
                new[]{30,44,24,7,25},
                new[]{30,59,16,1,17},
            },
            new[]{
                new[]{30,12,121,7,122},
                new[]{28,12,47,26,48},
                new[]{30,39,24,14,25},
                new[]{30,22,15,41,16},
            },
            new[]{
                new[]{30,6,121,14,122},
                new[]{28,6,47,34,48},
                new[]{30,46,24,10,25},
                new[]{30,2,15,64,16},
            },
            new[]{
                new[]{30,17,122,4,123},
                new[]{28,29,46,14,47},
                new[]{30,49,24,10,25},
                new[]{30,24,15,46,16},
            },
            new[]{
                new[]{30,4,122,18,123},
                new[]{28,13,46,32,47},
                new[]{30,48,24,14,25},
                new[]{30,42,15,32,16},
            },
            new[]{
                new[]{30,20,117,4,118},
                new[]{28,40,47,7,48},
                new[]{30,43,24,22,25},
                new[]{30,10,15,67,16},
            },
            new[]{
                new[]{30,19,118,6,119},
                new[]{28,18,47,31,48},
                new[]{30,34,24,34,25},
                new[]{30,20,15,61,16},
            },
        };
    }

    private sealed class BitBuffer : List<int>
    {
        public int Length => Count;
        public void AppendBits(int val, int len)
        {
            for (int i = len - 1; i >= 0; i--)
            {
                this.Add((val >> i) & 1);
            }
        }
    }

    private static bool IsFinderCell(int r, int c, int n)
    {
        bool inTL = r < 7 && c < 7;
        bool inTR = r < 7 && c >= n - 7;
        bool inBL = r >= n - 7 && c < 7;
        return inTL || inTR || inBL;
    }

    private static void AppendEye(StringBuilder sb, double x, double y, QrEyeShape shape, string color, string maskId)
    {
        switch (shape)
        {
            case QrEyeShape.Rounded:
                sb.Append(CultureInfo.InvariantCulture, $"<defs><mask id=\"{maskId}\" maskUnits=\"userSpaceOnUse\">");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"7\" height=\"7\" rx=\"1.25\" ry=\"1.25\" fill=\"white\"/>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x + 1)}\" y=\"{Format(y + 1)}\" width=\"5\" height=\"5\" rx=\"0.25\" ry=\"0.25\" fill=\"black\"/>");
                sb.Append("</mask></defs>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"7\" height=\"7\" rx=\"1.25\" ry=\"1.25\" fill=\"{color}\" mask=\"url(#{maskId})\"/>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x + 2)}\" y=\"{Format(y + 2)}\" width=\"3\" height=\"3\" rx=\"0.75\" ry=\"0.75\" fill=\"{color}\"/>");
                break;
            case QrEyeShape.Framed:
                sb.Append(CultureInfo.InvariantCulture, $"<defs><mask id=\"{maskId}\" maskUnits=\"userSpaceOnUse\">");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"7\" height=\"7\" fill=\"white\"/>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x + 1.2)}\" y=\"{Format(y + 1.2)}\" width=\"{Format(7 - 2 * 1.2)}\" height=\"{Format(7 - 2 * 1.2)}\" fill=\"black\"/>");
                sb.Append("</mask></defs>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"7\" height=\"7\" fill=\"{color}\" mask=\"url(#{maskId})\"/>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x + 2)}\" y=\"{Format(y + 2)}\" width=\"3\" height=\"3\" fill=\"{color}\"/>");
                break;
            case QrEyeShape.Square:
            default:
                sb.Append(CultureInfo.InvariantCulture, $"<defs><mask id=\"{maskId}\" maskUnits=\"userSpaceOnUse\">");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"7\" height=\"7\" fill=\"white\"/>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x + 1)}\" y=\"{Format(y + 1)}\" width=\"5\" height=\"5\" fill=\"black\"/>");
                sb.Append("</mask></defs>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"7\" height=\"7\" fill=\"{color}\" mask=\"url(#{maskId})\"/>");
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Format(x + 2)}\" y=\"{Format(y + 2)}\" width=\"3\" height=\"3\" fill=\"{color}\"/>");
                break;
        }
    }

    private static string EyeFill(string? color, string fallback, string parameterName)
        => color is null ? fallback : SvgAttributes.Color(color, parameterName);

    private static (string Color, double Opacity) GetSvgFillParts(string? color, string parameterName)
        => SvgAttributes.Fill(color, parameterName);

    private static string Format(double v) => SvgAttributes.Number(v);
}
