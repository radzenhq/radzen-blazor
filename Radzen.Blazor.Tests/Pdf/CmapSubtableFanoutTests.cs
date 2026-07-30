#nullable enable
using System.Collections.Generic;

using Xunit;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CmapSubtableFanoutTests
{
    private const int MaxSegCount = 32767;

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
