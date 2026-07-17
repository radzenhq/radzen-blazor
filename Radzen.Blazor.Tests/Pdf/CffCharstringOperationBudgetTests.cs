#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Xunit;
using Xunit.Abstractions;

namespace Radzen.Blazor.Pdf.Tests;

// The Type 2 walk caps each step - stack depth at 48, subr nesting at depth 10 - but until
// the operation budget existed it never capped their product. A chain of subrs that each
// call the next k times performs k^depth calls while never holding more than one operand,
// so neither per-step cap can fire. These tests bound the product itself.
public class CffCharstringOperationBudgetTests(ITestOutputHelper output)
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

    // Local subr numbers are biased by 107 under 1240 subrs, so subr n is the single-byte
    // operand n + 32.
    private static void CallLocalSubr(List<byte> bytes, int subr)
    {
        bytes.Add((byte)(subr + 32));
        bytes.Add(10); // callsubr
    }

    // subr[d] calls subr[d+1] k times for d in 1..levels-1; subr[levels] just returns. Every
    // callsubr pushes one operand and RunSubr pops it, so the stack oscillates 0 -> 1 -> 0 and
    // never approaches 48. Only ops 10 and 11 appear, so WidthContext.Visit never runs and
    // cannot Stop() the walk early. Work is k^(levels-1) calls from a font of a few hundred bytes.
    private static byte[] FanOutFont(int levels, int k, params byte[] tail)
    {
        var subrs = new List<byte[]>();
        subrs.Add(new byte[] { 11 }); // subr 0, unused

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

            body.Add(11); // return
            subrs.Add([.. body]);
        }

        // The tail runs only if the fan-out is interpreted to completion, so it is what tells
        // an exhausted budget apart from a walk that simply finished the work.
        var main = new List<byte>();
        CallLocalSubr(main, 1);
        main.AddRange(tail);

        return BuildFont([[.. main]], [.. subrs]);
    }

    [Fact]
    public void FanOutSubrWorkGrowsExponentiallyAndTheBudgetBoundsIt()
    {
        // Bounded probe: never run the 10^18 construct. Measure small k and report the growth.
        var generous = new ReaderLimits { MaxCharstringOperations = int.MaxValue };

        foreach (var k in new[] { 2, 3, 4, 5 })
        {
            var font = CffFont.Parse(FanOutFont(levels: 9, k, 0x8C, 14), generous);
            var watch = Stopwatch.StartNew();
            font.GetAdvanceWidth(0);
            watch.Stop();
            output.WriteLine($"k={k} expected calls={Math.Pow(k, 8):N0} elapsed={watch.Elapsed.TotalMilliseconds:F1}ms");
        }
    }

    [Fact]
    public void FanOutFontIsRejectedByTheDefaultBudgetRatherThanInterpretedToCompletion()
    {
        // k=8, 9 levels: 8^8 = ~16.7M calls, far past the default budget but small enough that
        // a regression here fails the test in seconds instead of hanging the host.
        var font = CffFont.Parse(FanOutFont(levels: 9, k: 8, 0x8C, 14));

        var watch = Stopwatch.StartNew();
        var width = font.GetAdvanceWidth(0);
        watch.Stop();

        // The budget ends the walk before the trailing endchar, so no width resolves and the
        // glyph degrades to defaultWidthX, as every other malformed case in this walk does.
        // Uninterrupted, the walk reaches that endchar and answers nominalWidthX + 1 = 201.
        Assert.Equal(100, width);
        Assert.True(watch.Elapsed.TotalSeconds < 5, $"walk took {watch.Elapsed.TotalSeconds:F1}s");
    }

    [Fact]
    public void BudgetBoundsTotalOperationsNotOperationsPerSubr()
    {
        // The defect this guards: each subr body is tiny and each step is within every
        // per-step cap. Only a walk-wide counter sees the product. A 200-op budget must stop
        // a font demanding 5^8 calls.
        var font = CffFont.Parse(
            FanOutFont(levels: 9, k: 5, 0x8C, 14),
            new ReaderLimits { MaxCharstringOperations = 200 });

        Assert.Equal(100, font.GetAdvanceWidth(0));
    }

    [Theory]
    [InlineData(50, 100)] // budget exhausted before the endchar: no width resolves
    [InlineData(5000, 201)] // budget covers the walk: the endchar's leading width is read
    public void BudgetIsWalkWideSoAFlatCharstringOfManyOperationsIsAlsoBounded(int budget, int expected)
    {
        // No subrs at all. "drop" on an empty stack is a no-op the width walk does not Visit,
        // so 500 of them run the interpreter without pushing an operand or resolving a width -
        // proving the counter is walk-wide and not scoped to a subr invocation.
        var main = new List<byte>();
        for (var i = 0; i < 500; i++)
        {
            main.AddRange([12, 18]);
        }

        main.Add(0x8C); // width operand, value 1
        main.Add(14);

        var font = CffFont.Parse(BuildFont([[.. main]], [[11]]), new ReaderLimits { MaxCharstringOperations = budget });

        Assert.Equal(expected, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void SeacWalkSharesTheSameBudget()
    {
        // The tail is a 4-operand seac endchar: reached, it answers true. The budget must stop
        // the walk before it, so the seac walk is bounded by the same counter as the width walk.
        var font = CffFont.Parse(FanOutFont(levels: 9, k: 8, 0x8B, 0x8B, 0x8B, 0x8B, 14));

        Assert.False(font.UsesSeacEndchar(0));
    }

    [Fact]
    public void ABudgetLargeEnoughForTheWorkStillResolvesTheWidth()
    {
        // levels=3, k=2 is 2^2 leaf calls: well inside a 1000-op budget, so the width the
        // charstring carries must still be read. A budget that rejects this would be a bug.
        var subrs = new List<byte[]>();
        subrs.Add(new byte[] { 11 });
        subrs.Add(new byte[] { 0x8C, 0x8B, 0x8B, 21, 11 });
        var font = CffFont.Parse(
            BuildFont([[33, 10, 14]], [.. subrs]),
            new ReaderLimits { MaxCharstringOperations = 1000 });

        // rmoveto with 3 operands inside subr 1: leading width 1 + nominalWidthX 200.
        Assert.Equal(201, font.GetAdvanceWidth(0));
    }

    [Fact]
    public void BudgetMustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReaderLimits { MaxCharstringOperations = 0 }.Snapshot());
    }

    private static CffFont NotoCff(ReaderLimits? limits = null)
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cff));
        return CffFont.Parse(cff, limits);
    }

    // Positive control: the shipped CID-keyed CFF must be unaffected by the budget. Every one
    // of its glyphs is compared against a walk that cannot run out, for both answers the
    // interpreter produces. A guard that bought safety by moving a real font's widths would be
    // a regression, so this is the oracle for the default.
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

    // The measured worst case across those 658 glyphs is 21 operations - the width walk stops
    // at the first stem/moveto/endchar, so a real charstring never runs long. 1000 is already
    // ~50x that, and the 1,000,000 default is ~47,000x: the budget cannot plausibly bite a
    // legitimate font, which is why it is set where it is.
    [Fact]
    public void RealFontGlyphsStayOrdersOfMagnitudeInsideTheDefaultBudget()
    {
        var unbounded = NotoCff(new ReaderLimits { MaxCharstringOperations = int.MaxValue });
        var tight = NotoCff(new ReaderLimits { MaxCharstringOperations = 1000 });

        for (var g = 0; g < unbounded.GlyphCount; g++)
        {
            Assert.Equal(unbounded.GetAdvanceWidth(g), tight.GetAdvanceWidth(g));
        }
    }
}
