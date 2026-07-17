#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The width walk and the seac walk share one Type 2 interpreter and differ only in the
// answer they collect. The manifest test below pins the answers of a pseudo-random
// charstring corpus so that any change to the shared walk that moves either answer -
// including the subr-recursion and hintmask-skip paths - fails loudly.
public class CffCharstringWalkTests
{
    private static byte[] BuildFont(byte[][] charStrings, byte[][] localSubrs)
    {
        // Private size covers the DICT only; the local Subr INDEX sits directly after it,
        // which is what the Subrs operand's private-relative offset addresses.
        var subrIndex = CffIndex.Write(localSubrs);
        var dict = new List<byte>();
        CffFixtureBuilder.Int5(dict, 100);
        dict.Add(20); // defaultWidthX
        CffFixtureBuilder.Int5(dict, 200);
        dict.Add(21); // nominalWidthX
        var dictLen = dict.Count + 6;
        CffFixtureBuilder.Int5(dict, dictLen);
        dict.Add(19); // Subrs, relative to the Private DICT
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
                bytes.Add((byte)random.Next(32, 247)); // subr number
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

    // 0x8B..0x8F encode operands 0..4; the fixture's Private DICT sets defaultWidthX=100
    // and nominalWidthX=200. Subr 0 is addressed by -107, the single-byte form of 0 - bias.
    private const byte Zero = 0x8B;
    private const byte One = 0x8C;
    private const byte Subr0 = 32;

    private static CffFont Font(byte[] charString, params byte[][] subrs) =>
        CffFont.Parse(BuildFont([charString], subrs.Length == 0 ? [[]] : subrs));

    [Theory]
    [InlineData(4, false, 100)] // endchar seac: 4 operands, no leading width
    [InlineData(5, true, 201)] // endchar seac: leading width plus the 4 seac operands
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
        // The mask byte is 0x0E, which would decode as endchar if it were not skipped -
        // that would end the walk early and miss the real seac endchar behind it.
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

    // Charstrings come from untrusted embedded fonts. A truncated operand at the end of one
    // must surface as a parse error, matching the convention ReadByteAt/ReadCard16 and
    // CffRealOperandTests already hold the DICT path to - never IndexOutOfRangeException.
    [Theory]
    [InlineData(new byte[] { 255 })] // 16.16 fixed, wants 4 more bytes
    [InlineData(new byte[] { 255, 1, 2, 3 })]
    [InlineData(new byte[] { 28 })] // short int, wants 2 more bytes
    [InlineData(new byte[] { 28, 1 })]
    [InlineData(new byte[] { 247 })] // two-byte positive, wants 1 more byte
    [InlineData(new byte[] { 251 })] // two-byte negative, wants 1 more byte
    [InlineData(new byte[] { 12 })] // escape with no sub-operator byte
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

    // Type 2 arithmetic escapes leave their result on the stack; only the drawing escapes
    // (flex and friends) consume their operands. Clearing the stack for "div" would drop
    // the leading width operand before hmoveto could resolve it.
    [Fact]
    public void DivLeavesItsResultOnTheStackSoTheWidthStillResolves()
    {
        // width(1) 4 2 div hmoveto -> stack [1, 2] at hmoveto, so the width operand is present.
        var font = Font([One, 0x8F, 0x8D, 12, 12, 22]);

        Assert.Equal(201, font.GetAdvanceWidth(0));
    }

    [Theory]
    [InlineData(10, 3)] // add: 1 2 add -> 3
    [InlineData(11, -1)] // sub: 1 2 sub -> -1
    [InlineData(24, 2)] // mul: 1 2 mul -> 2
    public void ArithmeticEscapesLeaveOneResultOnTheStack(byte escape, int expected)
    {
        // 1 2 <escape> endchar: the result is the only operand, so it reads as the width.
        var font = Font([One, 0x8D, 12, escape, 14]);

        Assert.Equal(200 + expected, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void DrawingEscapesStillConsumeTheirOperands()
    {
        // width(1) then a 13-operand flex (12 35), then endchar with an empty stack.
        var cs = new List<byte> { One };
        for (var i = 0; i < 13; i++)
        {
            cs.Add(Zero);
        }

        cs.AddRange([12, 35, 14]);

        // flex clears, so endchar sees no leading width operand: defaultWidthX applies.
        Assert.Equal(100, Font([.. cs]).GetAdvanceWidth(0));
    }

    // Re-pinned when the arithmetic escapes stopped discarding their result. Exactly 2 of the
    // 785 answers moved, both widths the old walk had lost to an unconditional stack clear:
    // 9:2 100 -> 628 (abs, 12 9, before hstem: 3 operands left, so the leading width counts)
    // 279:0 100 -> -25821 (add, 12 10, before rmoveto). Both verified against an independent
    // decode of those charstrings. No answer moved to an exception: the corpus never
    // truncates an operand, so the bounds guard is not what re-pinned this.
    private const string PinnedManifestHash = "1E262A6DB348AB5736217E20572BE45F41D15EFD656639B9AED28FDF361CD554";
}
