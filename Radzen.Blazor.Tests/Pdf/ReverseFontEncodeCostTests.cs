#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Radzen.Documents.Pdf.Fonts;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// TryEncode is called once per character by redaction and per segment by replacement, so its
// cost must not scale with the size of the font's code -> Unicode map (a CJK /ToUnicode CMap
// holds tens of thousands of entries). The reverse map is derived from an immutable field and
// must be built once, not per call, and lookups must not scan every entry.
public class ReverseFontEncodeCostTests
{
    private static ReverseFont LargeFont(int entries)
    {
        var gidToUnicode = new Dictionary<ushort, int>(entries);
        for (var i = 0; i < entries; i++)
        {
            gidToUnicode[(ushort)(i + 1)] = 0x4E00 + i;
        }

        return ReverseFont.FromGlyphIds(gidToUnicode);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void RepeatedEncode_DoesNotAllocatePerCallWithMapSize()
    {
        var font = LargeFont(20000);
        var text = char.ConvertFromUtf32(0x4E00);

        Assert.True(font.TryEncode(text, out _));

        var bytes = Measure(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                font.TryEncode(text, out _);
            }
        });

        // 960 bytes measured, against ~6,000,000 if each call rebuilt the 20000-entry map.
        Assert.True(bytes < 40_960, $"TryEncode allocated {bytes} bytes for 10 single-character calls.");
    }

    [Fact]
    public void EncodeCost_DoesNotScaleWithMapSize()
    {
        var small = LargeFont(200);
        var large = LargeFont(20000);
        var text = char.ConvertFromUtf32(0x4E00);

        Assert.True(small.TryEncode(text, out _));
        Assert.True(large.TryEncode(text, out _));

        var smallBytes = Measure(() => small.TryEncode(text, out _));
        var largeBytes = Measure(() => large.TryEncode(text, out _));

        // Both measure 96 bytes (the closure), independent of the map. Rebuilding the reverse
        // map per call would cost the 20000-entry font ~600,000 bytes against the 200-entry
        // font's ~6,000, so a band that only an algorithmic regression can breach - an exact
        // equality here would instead turn on whether a JIT promotion landed in the window.
        Assert.True(largeBytes < smallBytes + 50_000,
            $"TryEncode allocated {largeBytes} bytes against {smallBytes} for a map 100x smaller: cost scales with map size.");
    }

    [Fact]
    public void MultiCharMappings_StillEncodeAsSingleCode()
    {
        var font = ReverseFont.FromGlyphIds(new Dictionary<ushort, int>
        {
            [1] = 'f',
            [2] = 0x10400,
        });

        Assert.True(font.TryEncode("f\U00010400", out var codes));
        Assert.Equal([0, 1, 0, 2], codes);
        Assert.False(font.TryEncode("fx", out _));
    }
}
