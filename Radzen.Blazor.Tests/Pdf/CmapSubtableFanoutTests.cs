#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

public class CmapSubtableFanoutTests
{
    private const int MaxSegCount = 32767;

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

        var length = 16 + (segCount * 8);
        U16(4);
        U16(length);
        U16(0);
        U16(segCount * 2);
        U16(0);
        U16(0);
        U16(0);

        for (var i = 0; i < segCount; i++)
        {
            U16(0xFFFF);
        }

        U16(0);

        for (var i = 0; i < segCount; i++)
        {
            U16(0xFFFF);
        }

        for (var i = 0; i < segCount; i++)
        {
            U16(0);
        }

        for (var i = 0; i < segCount; i++)
        {
            U16(0);
        }

        return bytes.ToArray();
    }

    private static TimeSpan TimeParse(int numTables, int segCount)
    {
        var font = BuildFanoutCmap(numTables, segCount);
        Cmap.Parse(font);

        var watch = Stopwatch.StartNew();
        Cmap.Parse(font);
        watch.Stop();
        return watch.Elapsed;
    }

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

        const int winnerLength = 24;
        var winnerOffset = 4 + 16;
        U16(0);
        U16(2);
        U16(3); U16(1); U32(winnerOffset);
        U16(1); U16(0); U32(winnerOffset + winnerLength);

        U16(4); U16(winnerLength); U16(0); U16(2); U16(2); U16(0); U16(0);
        U16(0x0041);
        U16(0);
        U16(0x0041);
        U16(0);
        U16(0);

        Assert.Equal(winnerOffset + winnerLength, bytes.Count);

        U16(4); U16(0xFFFF); U16(0); U16(MaxSegCount * 2); U16(0); U16(0); U16(0);

        var mapper = Cmap.Parse(bytes.ToArray());

        Assert.Equal(0x41, (int)mapper.GetGlyphId(0x41));
    }

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
