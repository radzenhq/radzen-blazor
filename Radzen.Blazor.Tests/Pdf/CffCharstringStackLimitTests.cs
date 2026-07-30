#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;
using Radzen.Documents;

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

    private static void Push(List<byte> bytes, int count)
    {
        for (var i = 0; i < count; i++)
        {
            bytes.Add(139);
        }
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
