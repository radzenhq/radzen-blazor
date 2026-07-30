#nullable enable
using System;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class TtcStyleSelectionTests
{
    private const string Family = "Liberation Sans";

    private static MemoryStream Ttc()
        => new(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-RegBold.ttc"));

    [Fact]
    public void RegisterFromTtc_BoldRequest_PicksBoldFace()
    {
        var fonts = new FontCollection();
        fonts.Register(Family, Ttc(), bold: true, italic: false);

        var face = fonts.ResolvePrimarySfnt(new Font { Family = Family, Bold = true });
        Assert.True(face.Bold);
        Assert.False(face.Italic);
    }

    [Fact]
    public void RegisterFromTtc_RegularRequest_PicksRegularFace()
    {
        var fonts = new FontCollection();
        fonts.Register(Family, Ttc(), bold: false, italic: false);

        var face = fonts.ResolvePrimarySfnt(new Font { Family = Family });
        Assert.False(face.Bold);

        fonts.Register(Family, Ttc(), bold: true, italic: false);
        var bold = fonts.ResolvePrimarySfnt(new Font { Family = Family, Bold = true });
        Assert.True(bold.Bold);
    }

    [Fact]
    public void RegisterFromTtc_StyledFaces_MeasureWithStyledAdvances()
    {
        var fonts = new FontCollection();
        fonts.Register(Family, Ttc(), bold: false, italic: false);
        fonts.Register(Family, Ttc(), bold: true, italic: false);

        var regular = fonts.MeasureText("A", new Font { Family = Family, Size = 2048 });
        var bold = fonts.MeasureText("A", new Font { Family = Family, Size = 2048, Bold = true });

        Assert.Equal(1366.0, regular, 0.01);
        Assert.Equal(1479.0, bold, 0.01);
    }

    [Fact]
    public void BoldRunFromTtc_EmbedsSubsetOfBoldFace()
    {
        var document = new Document();
        document.Fonts.Register(Family, Ttc(), bold: false, italic: false);
        document.Fonts.Register(Family, Ttc(), bold: true, italic: false);

        var section = document.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("AB");
        run.Font.Family = Family;
        run.Font.Size = 12;
        run.Font.Bold = true;

        var reader = BuildTestSupport.Read(document);
        var top = Assert.Single(BuildTestSupport.Type0Fonts(reader));
        var descendants = (ArrayObject)reader.Resolve(top["DescendantFonts"]);
        var descendant = (DictionaryObject)reader.Resolve(descendants[0]);
        var widths = Type0EmbedSupport.ParseWidths(reader, (ArrayObject)reader.Resolve(descendant["W"]));

        Assert.Contains(722, widths.Values);
        Assert.DoesNotContain(667, widths.Values);

        var content = CascadeTestSupport.FirstPageContent(document);
        Assert.DoesNotContain("2 Tr", content, StringComparison.Ordinal);

        Assert.Contains("AB", BuildTestSupport.Reload(document).ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void SelectFaceForAbsentStylePrefersRegularOverFirstFace()
    {
        var faces = SfntFont.ParseCollection(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-RegBold.ttc"));
        var regular = faces.Single(face => !face.Bold && !face.Italic);
        var boldFirst = faces.OrderByDescending(face => face.Bold).ToList();

        var selected = SfntFont.SelectFace(boldFirst, regular.FamilyName, bold: false, italic: true);

        Assert.False(selected.Bold);
        Assert.False(selected.Italic);
    }
}
