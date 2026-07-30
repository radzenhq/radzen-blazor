#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CffCharstringWalkTests
{
    private static byte[] BuildFont(byte[][] charStrings, byte[][] localSubrs)
    {
        var subrIndex = CffIndex.Write(localSubrs);
        var dict = new List<byte>();
        CffFixtureBuilder.Int5(dict, 100);
        dict.Add(20);
        CffFixtureBuilder.Int5(dict, 200);
        dict.Add(21);
        var dictLen = dict.Count + 6;
        CffFixtureBuilder.Int5(dict, dictLen);
        dict.Add(19);
        Assert.Equal(dictLen, dict.Count);

        var privateBody = new List<byte>(dict);
        privateBody.AddRange(subrIndex);

        return CffFixtureBuilder.Build([.. privateBody], charStrings, (cs, priv) =>
        {
            var top = new List<byte>();
            CffFixtureBuilder.Int5(top, cs);
            top.Add(17);
            CffFixtureBuilder.Int5(top, dictLen);
            CffFixtureBuilder.Int5(top, priv);
            top.Add(18);
            return [.. top];
        });
    }

    private static readonly int[] Operators = [1, 3, 18, 23, 19, 20, 21, 4, 22, 14, 10, 29, 11, 12];

    private static byte[] RandomCharString(Random random)
    {
        var bytes = new List<byte>();
        var pieces = random.Next(1, 7);
        for (var p = 0; p < pieces; p++)
        {
            var operands = random.Next(0, 6);
            for (var o = 0; o < operands; o++)
            {
                switch (random.Next(0, 5))
                {
                    case 0:
                        bytes.AddRange([28, (byte)random.Next(256), (byte)random.Next(256)]);
                        break;
                    case 1:
                        bytes.Add((byte)random.Next(32, 247));
                        break;
                    case 2:
                        bytes.AddRange([(byte)random.Next(247, 251), (byte)random.Next(256)]);
                        break;
                    case 3:
                        bytes.AddRange([(byte)random.Next(251, 255), (byte)random.Next(256)]);
                        break;
                    default:
                        bytes.AddRange([255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)]);
                        break;
                }
            }

            var op = Operators[random.Next(Operators.Length)];
            if (op == 10 || op == 29)
            {
                bytes.Add((byte)random.Next(32, 247));
            }

            bytes.Add((byte)op);
            if (op == 12)
            {
                bytes.Add((byte)random.Next(0, 40));
            }
        }

        return [.. bytes];
    }

    [Fact]
    public void WidthAndSeacWalksMatchPinnedManifest()
    {
        var random = new Random(20260716);
        var manifest = new StringBuilder();

        for (var iteration = 0; iteration < 400; iteration++)
        {
            var charStrings = new byte[random.Next(1, 4)][];
            for (var g = 0; g < charStrings.Length; g++)
            {
                charStrings[g] = RandomCharString(random);
            }

            var subrs = new byte[random.Next(1, 4)][];
            for (var s = 0; s < subrs.Length; s++)
            {
                subrs[s] = RandomCharString(random);
            }

            CffFont font;
            try
            {
                font = CffFont.Parse(BuildFont(charStrings, subrs));
            }
            catch (Exception e)
            {
                manifest.Append(iteration).Append(":parse:").Append(e.GetType().Name).Append('\n');
                continue;
            }

            for (var g = 0; g < font.GlyphCount; g++)
            {
                manifest.Append(iteration).Append(':').Append(g).Append(':');
                manifest.Append(Answer(() => font.GetAdvanceWidth(g).ToString(CultureInfo.InvariantCulture)));
                manifest.Append(':');
                manifest.Append(Answer(() => font.UsesSeacEndchar(g).ToString()));
                manifest.Append('\n');
            }
        }

        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(manifest.ToString())));

        Assert.Equal(PinnedManifestHash, hash);
    }

    private static string Answer(Func<string> f)
    {
        try
        {
            return f();
        }
        catch (Exception e)
        {
            return "EX:" + e.GetType().Name;
        }
    }

    private const byte Zero = 0x8B;
    private const byte One = 0x8C;
    private const byte Subr0 = 32;

    private static CffFont Font(byte[] charString, params byte[][] subrs) =>
        CffFont.Parse(BuildFont([charString], subrs.Length == 0 ? [[]] : subrs));

    [Theory]
    [InlineData(4, false, 100)]
    [InlineData(5, true, 201)]
    public void EndcharSeacOperandsAreReadAsWidthAndSeac(int operands, bool hasWidth, int width)
    {
        var cs = new List<byte>();
        for (var i = 0; i < operands; i++)
        {
            cs.Add(hasWidth && i == 0 ? One : Zero);
        }

        cs.Add(14);

        var font = Font([.. cs]);
        Assert.True(font.UsesSeacEndchar(0));
        Assert.Equal(width, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void HintmaskMaskBytesAreSkippedRatherThanReadAsOperators()
    {
        byte[] cs = [Zero, Zero, 1, 19, 0x0E, Zero, Zero, Zero, Zero, 14];

        Assert.True(Font(cs).UsesSeacEndchar(0));
    }

    [Fact]
    public void WidthIsRecoveredFromInsideACalledSubr()
    {
        var font = Font([Subr0, 10], [One, Zero, Zero, 21]);

        Assert.Equal(201, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void SeacIsDetectedFromInsideACalledSubr()
    {
        var font = Font([Subr0, 10], [Zero, Zero, Zero, Zero, 14]);

        Assert.True(font.UsesSeacEndchar(0));
    }

    [Theory]
    [InlineData(new byte[] { 255 })]
    [InlineData(new byte[] { 255, 1, 2, 3 })]
    [InlineData(new byte[] { 28 })]
    [InlineData(new byte[] { 28, 1 })]
    [InlineData(new byte[] { 247 })]
    [InlineData(new byte[] { 251 })]
    [InlineData(new byte[] { 12 })]
    public void TruncatedOperandSurfacesAsParseError(byte[] charString)
    {
        var font = Font(charString);

        Assert.Throws<InvalidDataException>(() => font.GetAdvanceWidth(0));
        Assert.Throws<InvalidDataException>(() => font.UsesSeacEndchar(0));
    }

    [Fact]
    public void TruncatedOperandInsideASubrSurfacesAsParseError()
    {
        var font = Font([Subr0, 10], [[255, 1]]);

        Assert.Throws<InvalidDataException>(() => font.GetAdvanceWidth(0));
    }

    [Fact]
    public void DivLeavesItsResultOnTheStackSoTheWidthStillResolves()
    {
        var font = Font([One, 0x8F, 0x8D, 12, 12, 22]);

        Assert.Equal(201, font.GetAdvanceWidth(0));
    }

    [Theory]
    [InlineData(10, 3)]
    [InlineData(11, -1)]
    [InlineData(24, 2)]
    public void ArithmeticEscapesLeaveOneResultOnTheStack(byte escape, int expected)
    {
        var font = Font([One, 0x8D, 12, escape, 14]);

        Assert.Equal(200 + expected, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void DrawingEscapesStillConsumeTheirOperands()
    {
        var cs = new List<byte> { One };
        for (var i = 0; i < 13; i++)
        {
            cs.Add(Zero);
        }

        cs.AddRange([12, 35, 14]);

        Assert.Equal(100, Font([.. cs]).GetAdvanceWidth(0));
    }

    private const string PinnedManifestHash = "1E262A6DB348AB5736217E20572BE45F41D15EFD656639B9AED28FDF361CD554";
}
