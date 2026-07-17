#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// cmap's numTables (up to 65535) drives a loop that fully parses each record's
// subtableOffset, and every record may name the same fat subtable. Only the
// best-scoring subtable survives, so the other N-1 parses are pure waste an
// attacker controls. These tests pin that the parse cost stops scaling with
// numTables. The probes stay small on purpose: they demonstrate the growth
// curve rather than exhausting the host.
public class CmapSubtableFanoutTests
{
    private const int MaxSegCount = 32767;

    // A cmap whose numTables records all point at one format 4 subtable with
    // segCount segments. The subtable is laid out once; only the record array
    // grows with numTables.
    private static byte[] BuildFanoutCmap(int numTables, int segCount)
    {
        var bytes = new List<byte>();

        void U16(int v) { bytes.Add((byte)(v >> 8)); bytes.Add((byte)v); }
        void U32(long v)
        {
            bytes.Add((byte)(v >> 24));
            bytes.Add((byte)(v >> 16));
            bytes.Add((byte)(v >> 8));
            bytes.Add((byte)v);
        }

        var subtableOffset = 4 + (numTables * 8);

        U16(0);
        U16(numTables);

        for (var i = 0; i < numTables; i++)
        {
            U16(3);
            U16(1);
            U32(subtableOffset);
        }

        // format 4 subtable: every segment maps nothing, but parsing it still
        // allocates four segCount-sized arrays and reads 8*segCount bytes.
        var length = 16 + (segCount * 8);
        U16(4);
        U16(length);
        U16(0);
        U16(segCount * 2);  // segCountX2
        U16(0);
        U16(0);
        U16(0);

        for (var i = 0; i < segCount; i++)
        {
            U16(0xFFFF);    // endCode
        }

        U16(0);             // reservedPad

        for (var i = 0; i < segCount; i++)
        {
            U16(0xFFFF);    // startCode
        }

        for (var i = 0; i < segCount; i++)
        {
            U16(0);         // idDelta
        }

        for (var i = 0; i < segCount; i++)
        {
            U16(0);         // idRangeOffset
        }

        return bytes.ToArray();
    }

    private static TimeSpan TimeParse(int numTables, int segCount)
    {
        var font = BuildFanoutCmap(numTables, segCount);
        Cmap.Parse(font);       // warm

        var watch = Stopwatch.StartNew();
        Cmap.Parse(font);
        watch.Stop();
        return watch.Elapsed;
    }

    // The defect, measured rather than assumed: with the subtable held fixed and
    // only numTables growing, parse cost must stay flat. Before the fix it grew
    // linearly in numTables (each record re-parsed the same 262 KB subtable), so
    // the 32x record increase cost ~32x the time. The 8x allowance is slack for a
    // noisy host; the pre-fix ratio clears it by a wide margin.
    [Fact]
    public void ParseCostDoesNotScaleWithNumTables()
    {
        var few = TimeParse(numTables: 64, segCount: MaxSegCount);
        var many = TimeParse(numTables: 2048, segCount: MaxSegCount);

        Assert.True(
            many < TimeSpan.FromTicks(Math.Max(few.Ticks, TimeSpan.FromMilliseconds(1).Ticks) * 8),
            $"Parse cost scaled with numTables: 64 records took {few.TotalMilliseconds:F1} ms, "
            + $"2048 records took {many.TotalMilliseconds:F1} ms.");
    }

    // The direct statement of the fix: N records naming one subtable cause one
    // parse, not N. A cmap this size is ~800 KB and would otherwise demand
    // billions of reads.
    [Fact]
    public void EveryRecordNamingTheSameSubtableParsesItOnce()
    {
        var font = BuildFanoutCmap(numTables: ushort.MaxValue, segCount: MaxSegCount);

        var watch = Stopwatch.StartNew();
        var mapper = Cmap.Parse(font);
        watch.Stop();

        Assert.NotNull(mapper);
        Assert.True(
            watch.Elapsed < TimeSpan.FromSeconds(5),
            $"65535 records over one subtable took {watch.Elapsed.TotalSeconds:F1} s.");
    }

    // A subtable that loses on score must not be parsed at all: score is decided
    // by platform/encoding/format, all readable without touching the segments.
    [Fact]
    public void ALosingSubtableIsNeverParsed()
    {
        var bytes = new List<byte>();

        void U16(int v) { bytes.Add((byte)(v >> 8)); bytes.Add((byte)v); }
        void U32(long v)
        {
            bytes.Add((byte)(v >> 24));
            bytes.Add((byte)(v >> 16));
            bytes.Add((byte)(v >> 8));
            bytes.Add((byte)v);
        }

        // Two records. The winner (3,1 format 4) is a real one-segment subtable.
        // The loser (1,0 format 4) claims segments that run off the end of the
        // buffer, so parsing it would throw. It must be scored, not parsed.
        const int winnerLength = 24;    // 14-byte header + five 2-byte segment fields
        var winnerOffset = 4 + 16;
        U16(0);
        U16(2);
        U16(3); U16(1); U32(winnerOffset);
        U16(1); U16(0); U32(winnerOffset + winnerLength);

        // winner: one segment, U+0041 -> glyph 0x41 (idDelta 0)
        U16(4); U16(winnerLength); U16(0); U16(2); U16(2); U16(0); U16(0);
        U16(0x0041);        // endCode
        U16(0);             // reservedPad
        U16(0x0041);        // startCode
        U16(0);             // idDelta
        U16(0);             // idRangeOffset

        Assert.Equal(winnerOffset + winnerLength, bytes.Count);

        // loser: segCountX2 claims 32767 segments the truncated buffer cannot supply.
        U16(4); U16(0xFFFF); U16(0); U16(MaxSegCount * 2); U16(0); U16(0); U16(0);

        var mapper = Cmap.Parse(bytes.ToArray());

        Assert.Equal(0x41, (int)mapper.GetGlyphId(0x41));
    }

    // Positive control: the real fixtures must still map text. A legitimate cmap
    // has a handful of subtables, so the winner-only parse changes nothing.
    [Theory]
    [InlineData("LiberationSans-Regular.ttf")]
    [InlineData("LiberationSans-Bold.ttf")]
    [InlineData("LiberationSans-BoldItalic.ttf")]
    [InlineData("LiberationSerif-Regular.ttf")]
    [InlineData("NotoSansSC-Subset.otf")]
    public void RealFontsStillResolveGlyphs(string file)
    {
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes($"Fonts/{file}"));

        Assert.NotEqual(0, font.GetGlyphId('A'));
        Assert.NotEqual(0, font.GetGlyphId('z'));
        Assert.NotEqual(0, font.GetGlyphId('0'));
    }

    [Fact]
    public void RealCollectionStillResolvesGlyphs()
    {
        var faces = SfntFont.ParseCollection(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-RegBold.ttc"));

        Assert.NotEmpty(faces);
        foreach (var face in faces)
        {
            Assert.NotEqual(0, face.GetGlyphId('A'));
        }
    }
}
