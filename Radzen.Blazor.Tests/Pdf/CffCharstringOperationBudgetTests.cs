#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Fonts.Sfnt;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CffCharstringOperationBudgetTests
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
        bytes.Add((byte)(subr + 32));
        bytes.Add(10);
    }

    private static byte[] FanOutFont(int levels, int k, params byte[] tail)
    {
        var subrs = new List<byte[]>();
        subrs.Add(new byte[] { 11 });

        for (var d = 1; d <= levels; d++)
        {
            var body = new List<byte>();
            if (d < levels)
            {
                for (var i = 0; i < k; i++)
                {
                    CallLocalSubr(body, d + 1);
                }
            }

            body.Add(11);
            subrs.Add([.. body]);
        }

        var main = new List<byte>();
        CallLocalSubr(main, 1);
        main.AddRange(tail);

        return BuildFont([[.. main]], [.. subrs]);
    }

    [Fact]
    public void BudgetBoundsTotalOperationsNotOperationsPerSubr()
    {
        var font = CffFont.Parse(
            FanOutFont(levels: 9, k: 5, 0x8C, 14),
            new ReaderLimits { MaxCharstringOperations = 200 });

        Assert.Equal(100, font.GetAdvanceWidth(0));
    }

    [Theory]
    [InlineData(50, 100)]
    [InlineData(5000, 201)]
    public void BudgetIsWalkWideSoAFlatCharstringOfManyOperationsIsAlsoBounded(int budget, int expected)
    {
        var main = new List<byte>();
        for (var i = 0; i < 500; i++)
        {
            main.AddRange([12, 18]);
        }

        main.Add(0x8C);
        main.Add(14);

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]), new ReaderLimits { MaxCharstringOperations = budget });

        Assert.Equal(expected, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void SeacWalkSharesTheSameBudget()
    {
        var font = CffFont.Parse(FanOutFont(levels: 9, k: 8, 0x8B, 0x8B, 0x8B, 0x8B, 14));

        Assert.False(font.UsesSeacEndchar(0));
    }

    [Fact]
    public void ABudgetLargeEnoughForTheWorkStillResolvesTheWidth()
    {
        var subrs = new List<byte[]>();
        subrs.Add(new byte[] { 11 });
        subrs.Add(new byte[] { 0x8C, 0x8B, 0x8B, 21, 11 });
        var font = CffFont.Parse(
            BuildFont([[33, 10, 14]], [.. subrs]),
            new ReaderLimits { MaxCharstringOperations = 1000 });

        Assert.Equal(201, font.GetAdvanceWidth(0));
    }

    private static CffFont NotoCff(ReaderLimits? limits = null)
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cff));
        return CffFont.Parse(cff, limits);
    }

    [Fact]
    public void EveryRealGlyphResolvesIdenticallyUnderTheDefaultBudget()
    {
        var unbounded = NotoCff(new ReaderLimits { MaxCharstringOperations = int.MaxValue });
        var byDefault = NotoCff();

        Assert.Equal(658, unbounded.GlyphCount);
        for (var g = 0; g < unbounded.GlyphCount; g++)
        {
            Assert.Equal(unbounded.GetAdvanceWidth(g), byDefault.GetAdvanceWidth(g));
            Assert.Equal(unbounded.UsesSeacEndchar(g), byDefault.UsesSeacEndchar(g));
        }
    }

}
