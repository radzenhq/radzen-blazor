#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Type 2 caps the argument stack at 48 entries. Charstrings arrive from untrusted embedded
// fonts, and subr nesting multiplies a handful of bytes into an unbounded number of pushes,
// so the cap is what keeps that from becoming an allocation the font can dictate.
public class CffCharstringStackLimitTests
{
    private static byte[] BuildFont(byte[][] charStrings, byte[][] localSubrs)
    {
        var subrIndex = CffIndex.Write(localSubrs);
        var dict = new List<byte>();
        CffFixtureBuilder.Int5(dict, 100);
        dict.Add(20); // defaultWidthX
        CffFixtureBuilder.Int5(dict, 200);
        dict.Add(21); // nominalWidthX
        var dictLen = dict.Count + 6;
        CffFixtureBuilder.Int5(dict, dictLen);
        dict.Add(19); // Subrs, relative to the Private DICT

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

    // Local subr numbers are biased; under 1240 subrs the bias is 107.
    private static void CallLocalSubr(List<byte> bytes, int subr)
    {
        bytes.Add((byte)(subr - 107 + 139)); // single-byte operand, value subr - 107
        bytes.Add(10);
    }

    private static void Push(List<byte> bytes, int count)
    {
        for (var i = 0; i < count; i++)
        {
            bytes.Add(139); // operand 0
        }
    }

    // subr 0 pushes and returns without an operator, so nothing clears the stack; each
    // outer level re-invokes the level below fanOut times, so pushes grow as fanOut^levels
    // from a font of a few dozen bytes.
    private static byte[] AmplifyingFont(int levels, int fanOut, int pushesPerLeaf)
    {
        var subrs = new List<byte[]>();

        var leaf = new List<byte>();
        Push(leaf, pushesPerLeaf);
        leaf.Add(11); // return
        subrs.Add([.. leaf]);

        for (var level = 1; level <= levels; level++)
        {
            var body = new List<byte>();
            for (var i = 0; i < fanOut; i++)
            {
                CallLocalSubr(body, level - 1);
            }

            body.Add(11);
            subrs.Add([.. body]);
        }

        var main = new List<byte>();
        CallLocalSubr(main, levels);
        main.Add(14); // endchar

        return BuildFont([[.. main]], [.. subrs]);
    }

    [Fact]
    public void DeeplyAmplifiedPushesDoNotGrowTheOperandStackWithoutBound()
    {
        // 4^7 * 8 = 131072 pushes demanded by a ~60 byte font. Uncapped, the walk performs
        // every one of them; capped at 48, it stops at the first overflow.
        var font = CffFont.Parse(AmplifyingFont(levels: 7, fanOut: 4, pushesPerLeaf: 8));

        var before = GC.GetAllocatedBytesForCurrentThread();
        font.GetAdvanceWidth(0);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // A 48-entry List<double> and its growth never reaches 64 KB; an uncapped walk
        // allocates megabytes growing a 131072-entry list.
        Assert.True(allocated < 64 * 1024, $"walk allocated {allocated} bytes");
    }

    [Fact]
    public void OperandStackOverflowStopsTheWalkAndFallsBackToDefaultWidth()
    {
        var main = new List<byte>();
        Push(main, 49);
        main.Add(14); // endchar

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]));

        // 49 operands overflow the 48-entry stack, so no width is resolved and the glyph
        // falls back to defaultWidthX rather than reading a bogus stack[0].
        Assert.Equal(100, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void FortyEightOperandsRemainWithinTheStackLimit()
    {
        var main = new List<byte>();
        Push(main, 47);
        main.Add(139); // operand 0, the 48th entry
        main.Add(14);

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]));

        // 48 entries is the limit, not one past it: an even operand count carries no width,
        // so endchar resolves defaultWidthX by the normal rule rather than by overflow.
        Assert.Equal(100, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void WidthStillResolvesFromAStackWithinTheLimit()
    {
        var main = new List<byte>();
        main.Add(139 + 5); // width operand, value 5
        Push(main, 2);
        main.Add(14);

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]));

        // An odd operand count carries a leading width: nominalWidthX 200 + 5.
        Assert.Equal(205, font.GetAdvanceWidth(0));
    }
}
