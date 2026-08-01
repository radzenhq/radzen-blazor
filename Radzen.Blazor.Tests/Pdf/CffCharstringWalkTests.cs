#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    private static readonly int[] ArithmeticFreeOperators = [1, 3, 18, 23, 19, 20, 21, 4, 22, 14, 10, 29, 11];

    private const int DeclaredDefaultWidthX = 100;

    private const int DeclaredNominalWidthX = 200;

    private const int MaxOperandMagnitude = 32768;

    private const int Corpus = 400;

    private static byte[] RandomCharString(Random random) => RandomCharString(random, Operators);

    private static byte[] RandomCharString(Random random, int[] operators)
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

            var op = operators[random.Next(operators.Length)];
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
    public void RandomCharstringWalksTerminateAndFailOnlyAsParseErrors()
    {
        var random = new Random(20260716);
        var stopwatch = Stopwatch.StartNew();
        var walked = 0;

        for (var iteration = 0; iteration < Corpus; iteration++)
        {
            if (RandomFont(random, Operators) is not { } font)
            {
                continue;
            }

            for (var glyph = 0; glyph < font.GlyphCount; glyph++)
            {
                var index = glyph;

                Assert.Equal(
                    Walk(() => font.GetAdvanceWidth(index)),
                    Walk(() => font.GetAdvanceWidth(index)));
                Assert.Equal(
                    Walk(() => font.UsesSeacEndchar(index) ? 1 : 0),
                    Walk(() => font.UsesSeacEndchar(index) ? 1 : 0));

                walked++;
            }
        }

        Assert.True(walked >= Corpus, $"Only {walked} glyph walks were exercised.");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"The walk took {stopwatch.Elapsed}.");
    }

    [Fact]
    public void ArithmeticFreeCharstringWidthsStayWithinTheDeclaredRange()
    {
        var random = new Random(20260716);
        var measured = 0;

        for (var iteration = 0; iteration < Corpus; iteration++)
        {
            if (RandomFont(random, ArithmeticFreeOperators) is not { } font)
            {
                continue;
            }

            for (var glyph = 0; glyph < font.GlyphCount; glyph++)
            {
                int width;
                try
                {
                    width = font.GetAdvanceWidth(glyph);
                }
                catch (InvalidDataException)
                {
                    continue;
                }

                Assert.InRange(
                    width,
                    Math.Min(DeclaredDefaultWidthX, DeclaredNominalWidthX - MaxOperandMagnitude),
                    Math.Max(DeclaredDefaultWidthX, DeclaredNominalWidthX + MaxOperandMagnitude));

                measured++;
            }
        }

        Assert.True(measured >= Corpus, $"Only {measured} widths were measured.");
    }

    [Fact]
    public void TruncatedCharstringsNeverEscapeAsAnythingButAParseError()
    {
        var random = new Random(20260716);
        var thrown = 0;
        var walked = 0;

        for (var iteration = 0; iteration < Corpus; iteration++)
        {
            if (RandomFont(random, Operators, truncate: true) is not { } font)
            {
                continue;
            }

            for (var glyph = 0; glyph < font.GlyphCount; glyph++)
            {
                var index = glyph;

                if (Walk(() => font.GetAdvanceWidth(index)) is null)
                {
                    thrown++;
                }

                if (Walk(() => font.UsesSeacEndchar(index) ? 1 : 0) is null)
                {
                    thrown++;
                }

                walked++;
            }
        }

        Assert.True(walked >= Corpus, $"Only {walked} glyph walks were exercised.");
        Assert.True(thrown > 0, "Truncation produced no malformed charstrings.");
    }

    private static CffFont? RandomFont(Random random, int[] operators, bool truncate = false)
    {
        var charStrings = new byte[random.Next(1, 4)][];
        for (var g = 0; g < charStrings.Length; g++)
        {
            charStrings[g] = Maybe(random, RandomCharString(random, operators), truncate);
        }

        var subrs = new byte[random.Next(1, 4)][];
        for (var s = 0; s < subrs.Length; s++)
        {
            subrs[s] = Maybe(random, RandomCharString(random, operators), truncate);
        }

        try
        {
            return CffFont.Parse(BuildFont(charStrings, subrs));
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static byte[] Maybe(Random random, byte[] charString, bool truncate)
        => truncate ? charString[..random.Next(0, charString.Length + 1)] : charString;

    private static int? Walk(Func<int> walk)
    {
        try
        {
            return walk();
        }
        catch (InvalidDataException)
        {
            return null;
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
}
