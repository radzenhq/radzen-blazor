#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// The SfntFont.ParseCollection guard bounds the ttcf offset TABLE (4 bytes per
// face must fit the buffer). The quantity that actually grows is the face PARSE
// work: each of numFonts offsets triggers a full SfntFont construction (head,
// maxp, hhea, name, hmtx and the whole cmap), and every offset may name the same
// face. numFonts is only bounded by data.Length/4, so an N-byte collection buys
// N/4 full parses of an N-byte font.
public class TtcFaceFanoutTests
{
    // A real face relocated so its table directory sits at faceOffset: the ttc
    // face offset is absolute, and so are the directory's table offsets, so the
    // whole face is simply shifted and the records rewritten.
    private static byte[] RelocatedFace(byte[] font, int faceOffset)
    {
        var moved = new byte[faceOffset + font.Length];
        Array.Copy(font, 0, moved, faceOffset, font.Length);

        var numTables = (moved[faceOffset + 4] << 8) | moved[faceOffset + 5];
        for (var i = 0; i < numTables; i++)
        {
            var record = faceOffset + 12 + (i * 16) + 8;
            var offset = ((uint)moved[record] << 24) | ((uint)moved[record + 1] << 16)
                | ((uint)moved[record + 2] << 8) | moved[record + 3];
            var shifted = offset + (uint)faceOffset;
            moved[record] = (byte)(shifted >> 24);
            moved[record + 1] = (byte)(shifted >> 16);
            moved[record + 2] = (byte)(shifted >> 8);
            moved[record + 3] = (byte)shifted;
        }

        return moved;
    }

    private static byte[] BuildProbe(int numFonts)
    {
        var font = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var faceOffset = 12 + (numFonts * 4);
        var relocated = RelocatedFace(font, faceOffset);

        var bytes = new List<byte>();
        void U32(long v)
        {
            bytes.Add((byte)(v >> 24));
            bytes.Add((byte)(v >> 16));
            bytes.Add((byte)(v >> 8));
            bytes.Add((byte)v);
        }

        U32(0x74746366);
        U32(0x00010000);
        U32(numFonts);
        for (var i = 0; i < numFonts; i++)
        {
            U32(faceOffset);
        }

        var result = new byte[relocated.Length];
        Array.Copy(relocated, result, relocated.Length);
        bytes.CopyTo(result, 0);
        return result;
    }

    private static TimeSpan TimeParse(int numFonts)
    {
        var data = BuildProbe(numFonts);
        SfntFont.ParseCollection(data);     // warm

        var watch = Stopwatch.StartNew();
        SfntFont.ParseCollection(data);
        watch.Stop();
        return watch.Elapsed;
    }

    // Bounded probe: hold the face fixed, grow only numFonts. Before the fix the
    // cost grew linearly with numFonts because every offset re-parsed the same
    // face; the cap makes the collection reject instead.
    [Fact]
    public void FaceCountBeyondTheCapIsRejected()
    {
        var data = BuildProbe(SfntFont.MaxCollectionFaces + 1);

        var error = Assert.Throws<InvalidDataException>(() => SfntFont.ParseCollection(data));

        Assert.Contains("face count", error.Message);
    }

    [Fact]
    public void FaceCountAtTheCapStillParses()
    {
        var data = BuildProbe(SfntFont.MaxCollectionFaces);

        var faces = SfntFont.ParseCollection(data);

        Assert.Equal(SfntFont.MaxCollectionFaces, faces.Count);
    }

    // The growth this bounds, measured: parsing MaxCollectionFaces copies of one
    // real face is the worst case the cap now permits, and it must stay quick.
    [Fact]
    public void ParsingTheCapWorthOfFacesStaysBounded()
    {
        var elapsed = TimeParse(SfntFont.MaxCollectionFaces);

        Assert.True(
            elapsed < TimeSpan.FromSeconds(5),
            $"Parsing {SfntFont.MaxCollectionFaces} faces took {elapsed.TotalSeconds:F1} s.");
    }

    // Positive control: the committed collections must still parse in full.
    [Theory]
    [InlineData("LiberationSans-RegBold.ttc")]
    [InlineData("Liberation-Subset.ttc")]
    public void RealCollectionsStillParse(string file)
    {
        var faces = SfntFont.ParseCollection(PdfTestResources.ReadAllBytes($"Fonts/{file}"));

        Assert.NotEmpty(faces);
        Assert.True(faces.Count <= SfntFont.MaxCollectionFaces);
        foreach (var face in faces)
        {
            Assert.NotEqual(0, face.GetGlyphId('A'));
        }
    }
}
