#nullable enable
using System;
using System.IO;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class SimpleShaperTests
{
    private static FontCollection LiberationSans()
    {
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return fonts;
    }

    private static SfntFont SansFace()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    private static double Advance(SfntFont face, char c, double size)
        => face.GetAdvanceWidth(face.GetGlyphId(c)) * size / face.UnitsPerEm;

    [Fact]
    public void Shape_TwoGlyphs_CorrectIdsAndClusters()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var face = SansFace();
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape("AV", font, out _);

        Assert.Equal(2, glyphs.Count);

        Assert.Equal(face.GetGlyphId('A'), glyphs[0].GlyphId);
        Assert.Equal(0, glyphs[0].Cluster);
        Assert.Equal(Advance(face, 'A', 12), glyphs[0].Advance, 10);

        Assert.Equal(face.GetGlyphId('V'), glyphs[1].GlyphId);
        Assert.Equal(1, glyphs[1].Cluster);
        Assert.Equal(Advance(face, 'V', 12), glyphs[1].Advance, 10);
    }

    [Fact]
    public void Shape_TotalAdvanceEqualsMeasureText()
    {
        var fonts = LiberationSans();
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        shaper.Shape("AV", font, out var advance);

        Assert.Equal(fonts.MeasureText("AV", font), advance, 10);
    }

    [Fact]
    public void Shape_NoKerning_AdvanceIsSumOfGlyphAdvances()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape("AV", font, out var advance);

        Assert.Equal(glyphs[0].Advance + glyphs[1].Advance, advance, 10);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MeasureText_EqualsShapedAdvance_Exactly(bool kerning)
    {
        var fonts = LiberationSans();
        fonts.EnableKerning = kerning;
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        foreach (var text in new[]
        {
            "", "A", "AV", "AVATAR", "Wave To Vary", "Measurement, 1234.56 EUR",
            "Liberation Sans - the quick brown fox jumps over the lazy dog",
            "ÄÖÜ Привет", "tab\tseparated", "😀 emoji",
        })
        {
            var shaped = fonts.Shaper().Shape(text, font, out var advance);
            Assert.Equal(advance, fonts.MeasureText(text, font));
            Assert.Equal(shaped.Sum(g => g.Advance), fonts.MeasureText(text, font), 10);
        }
    }

    private static readonly string[] WidthCorpus =
    [
        "", "A", "AV", "AVATAR", "Wave To Vary", "Measurement, 1234.56 EUR",
        "Liberation Sans - the quick brown fox jumps over the lazy dog",
        "ÄÖÜ Привет", "tab\tseparated", "😀 emoji", "VA.VA.VA", "To,Yo.We",
    ];

    public static TheoryData<bool, double, long[]> PinnedWidths => new()
    {
        { false, 7, [0, 4616942783519784960, 4621446383147155456, 4628576441175375872, 4631437782747709440, 4635943066002259968, 4640925898080518144, 4630934137702711296, 4630948087756488704, 4627426764329582592, 4629673204024082432, 4629452889381666816] },
        { false, 12, [0, 4620695416705384448, 4625199016332754944, 4631953866017996800, 4635049815883907072, 4639554858620289024, 4644469228919848960, 4634618120131051520, 4634630077320003584, 4630968428721602560, 4632893948459745280, 4632705107337674752] },
        { false, 13.5, [0, 4621258641536712704, 4625762241164083200, 4632798497106558976, 4635718490752286720, 4640223713877295104, 4645189430510878720, 4635232833030324224, 4635246284867895296, 4631689880148115456, 4633856089853526016, 4633643643591196672] },
        { true, 7, [0, 4616942783519784960, 4621153913054167040, 4627991500989399040, 4631218911214305280, 4635943066002259968, 4640925898080518144, 4630890363396030464, 4630948087756488704, 4627426764329582592, 4629234498884599808, 4629018032532881408] },
        { true, 12, [0, 4620695416705384448, 4624699838053744640, 4631452488715730944, 4634862211712417792, 4639554858620289024, 4644469228919848960, 4634580599296753664, 4634630077320003584, 4630968428721602560, 4632517915483045888, 4632332372895858688] },
        { true, 13.5, [0, 4621258641536712704, 4625480216431558656, 4632234447641509888, 4635507436059361280, 4640223713877295104, 4645189430510878720, 4635190622091739136, 4635246284867895296, 4631689880148115456, 4633433052754739200, 4633224317344153600] },
    };

    [Theory]
    [MemberData(nameof(PinnedWidths))]
    public void MeasureAdvance_AndShape_MatchPreMergeWidths_BitForBit(bool kerning, double size, long[] expected)
    {
        var fonts = LiberationSans();
        fonts.EnableKerning = kerning;
        var font = new Font { Family = "Liberation Sans", Size = size };

        for (var i = 0; i < WidthCorpus.Length; i++)
        {
            var measured = fonts.Shaper().MeasureAdvance(WidthCorpus[i], font);
            fonts.Shaper().Shape(WidthCorpus[i], font, out var shaped);

            Assert.Equal(expected[i], BitConverter.DoubleToInt64Bits(measured));
            Assert.Equal(expected[i], BitConverter.DoubleToInt64Bits(shaped));
        }
    }

    [Fact]
    public void Shape_UnknownFamily_ThrowsWithNameInMessage()
    {
        var shaper = new SimpleShaper(new FontCollection());
        var font = new Font { Family = "Nonexistent Font", Size = 12 };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            shaper.Shape("AV", font, out _));
        Assert.Contains("Nonexistent Font", ex.Message);
    }
}
