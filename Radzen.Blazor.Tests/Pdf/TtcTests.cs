#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

public class TtcTests
{
    private static byte[] Ttc() => PdfTestResources.ReadAllBytes("Fonts/Liberation-Subset.ttc");

    [Fact]
    public void ParseCollection_ReturnsTwoFacesInOrder()
    {
        var faces = SfntFont.ParseCollection(Ttc());

        Assert.Equal(2, faces.Count);
        Assert.Equal("Liberation Sans", faces[0].FamilyName);
        Assert.Equal("Liberation Serif", faces[1].FamilyName);
    }

    [Fact]
    public void ParseCollection_FacesHaveWorkingCmap()
    {
        var faces = SfntFont.ParseCollection(Ttc());

        Assert.Equal(34, (int)faces[0].GetGlyphId('A'));
        Assert.Equal(1, (int)faces[0].GetGlyphId(' '));
        Assert.Equal(34, (int)faces[1].GetGlyphId('A'));
        Assert.Equal(1, (int)faces[1].GetGlyphId(' '));
    }

    [Fact]
    public void Parse_ByFamilyName_PicksMatchingFace()
    {
        var font = SfntFont.Parse(Ttc(), "Liberation Serif");
        Assert.Equal("Liberation Serif", font.FamilyName);
    }

    [Fact]
    public void Parse_ByFamilyName_NoMatchThrowsWithRequestedName()
    {
        var ex = Assert.ThrowsAny<Exception>(() => SfntFont.Parse(Ttc(), "Nonexistent"));
        Assert.Contains("Nonexistent", ex.Message);
    }

    [Fact]
    public void ParseCollection_HostileNumFonts_ThrowsBeforeAllocating()
    {
        byte[] header =
        [
            0x74, 0x74, 0x63, 0x66,
            0x00, 0x01, 0x00, 0x00,
            0x7F, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x00,
        ];

        Assert.Throws<InvalidDataException>(() => SfntFont.ParseCollection(header));
    }

    [Fact]
    public void ParseCollection_OnPlainTtf_ReturnsSingleFace()
    {
        var faces = SfntFont.ParseCollection(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

        Assert.Single(faces);
        Assert.Equal("Liberation Sans", faces[0].FamilyName);
    }
}
