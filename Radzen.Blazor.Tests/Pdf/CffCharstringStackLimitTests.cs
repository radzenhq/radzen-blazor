#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class CffCharstringStackLimitTests
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

    private static void CallLocalSubr(List<byte> bytes, int subr)
    {
        bytes.Add((byte)(subr - 107 + 139));
        bytes.Add(10);
    }

    private static void Push(List<byte> bytes, int count)
    {
        for (var i = 0; i < count; i++)
        {
            bytes.Add(139);
        }
    }

    private static byte[] AmplifyingFont(int levels, int fanOut, int pushesPerLeaf)
    {
        var subrs = new List<byte[]>();

        var leaf = new List<byte>();
        Push(leaf, pushesPerLeaf);
        leaf.Add(11);
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
        main.Add(14);

        return BuildFont([[.. main]], [.. subrs]);
    }

    [Fact]
    public void DeeplyAmplifiedPushesDoNotGrowTheOperandStackWithoutBound()
    {
        var font = CffFont.Parse(AmplifyingFont(levels: 7, fanOut: 4, pushesPerLeaf: 8));

        var before = GC.GetAllocatedBytesForCurrentThread();
        font.GetAdvanceWidth(0);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 64 * 1024, $"walk allocated {allocated} bytes");
    }

    [Fact]
    public void OperandStackOverflowStopsTheWalkAndFallsBackToDefaultWidth()
    {
        var main = new List<byte>();
        Push(main, 49);
        main.Add(14);

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]));

        Assert.Equal(100, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void FortyEightOperandsRemainWithinTheStackLimit()
    {
        var main = new List<byte>();
        Push(main, 47);
        main.Add(139);
        main.Add(14);

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]));

        Assert.Equal(100, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void WidthStillResolvesFromAStackWithinTheLimit()
    {
        var main = new List<byte>();
        main.Add(139 + 5);
        Push(main, 2);
        main.Add(14);

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]));

        Assert.Equal(205, font.GetAdvanceWidth(0));
    }
}
