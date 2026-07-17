#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
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

    // Captured from the pre-merge implementation (two independent dispatch loops).
    private const string PinnedManifestHash = "C9DD3F21FBDF8B90539C65863BE6839E853919678CF8FD5082FCD23B0CDF7726";
}
