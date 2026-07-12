#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

// Standard-14 AFM pair kerning. Expected values are read straight from the raw AFM
// fixtures (KPX <nameA> <nameB> <value>), never from the generated Base14KernData, so a
// data-generation bug cannot self-confirm. Values are in 1/1000 em (AFM units).
public class Base14KerningTests
{
    private static Font MakeFont(string name) => new() { Name = name };

    // Reads the KPX value for a glyph-name pair from the raw AFM fixture; 0 when absent.
    private static int AfmKern(string afmFile, string leftGlyph, string rightGlyph)
    {
        using var stream = PdfTestResources.Open($"Afm/{afmFile}.afm");
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!line.StartsWith("KPX ", StringComparison.Ordinal))
            {
                continue;
            }

            var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 4 && p[1] == leftGlyph && p[2] == rightGlyph)
            {
                return int.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return 0;
    }

    [Theory]
    [InlineData("Helvetica", "Helvetica", 'A', "A", 'V', "V")]
    [InlineData("Helvetica", "Helvetica", 'T', "T", 'o', "o")]
    [InlineData("Helvetica", "Helvetica", 'W', "W", 'a', "a")]
    [InlineData("Times-Roman", "Times-Roman", 'A', "A", 'V', "V")]
    [InlineData("Times-Roman", "Times-Roman", 'T', "T", 'o', "o")]
    [InlineData("Times-Roman", "Times-Roman", 'W', "W", 'a', "a")]
    public void GetKerning_MatchesAfmForClassicPairs(
        string psName, string afmFile, char left, string leftGlyph, char right, string rightGlyph)
    {
        var expected = AfmKern(afmFile, leftGlyph, rightGlyph);
        Assert.True(expected < 0, "AV/To/Wa are negative in the reference AFM");

        var metrics = Base14Metrics.Resolve(MakeFont(psName));
        Assert.NotNull(metrics);
        Assert.Equal(expected, metrics!.GetKerning(left, right));
    }

    [Fact]
    public void GetKerning_ClassicPairsAreNegative()
    {
        var helvetica = Base14Metrics.Resolve(MakeFont("Helvetica"))!;
        Assert.True(helvetica.GetKerning('A', 'V') < 0);
        Assert.True(helvetica.GetKerning('T', 'o') < 0);
        Assert.True(helvetica.GetKerning('W', 'a') < 0);
    }

    [Fact]
    public void GetKerning_UnkernedPairIsZero()
    {
        var helvetica = Base14Metrics.Resolve(MakeFont("Helvetica"))!;
        // 'H' 'H' is not a kern pair in the Helvetica AFM.
        Assert.Equal(0, AfmKern("Helvetica", "H", "H"));
        Assert.Equal(0.0, helvetica.GetKerning('H', 'H'));
    }

    [Fact]
    public void GetKerning_CourierHasNoPairs()
    {
        var courier = Base14Metrics.Resolve(MakeFont("Courier"))!;
        Assert.Equal(0.0, courier.GetKerning('A', 'V'));
    }

    [Fact]
    public void GetKerning_StyledFacesResolveTheirOwnPairs()
    {
        var bold = Base14Metrics.Resolve(new Font { Name = "Helvetica", Bold = true })!;
        Assert.Equal(AfmKern("Helvetica-Bold", "A", "V"), bold.GetKerning('A', 'V'));
    }
}
