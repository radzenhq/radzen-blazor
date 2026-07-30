#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class InlineImageFlowTests
{
    private static InlineImage AddImage(Paragraph paragraph)
    {
        var image = paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(40);
        image.Height = Unit.FromPoint(30);
        return image;
    }

    private static List<(double X, double Y)> ImagePlacements(Document document)
    {
        var reader = BuildTestSupport.Read(document);
        var leaves = BuildTestSupport.PageLeaves(reader);
        var joined = new StringBuilder();
        foreach (var (page, _) in leaves)
        {
            joined.Append(Encoding.Latin1.GetString(BuildTestSupport.Content(reader, page)));
            joined.Append('\n');
        }

        var result = new List<(double, double)>();
        foreach (Match match in Regex.Matches(
            joined.ToString(),
            @"40(?:\.0+)?\s+0\S*\s+0\S*\s+30(?:\.0+)?\s+([-\d.]+)\s+([-\d.]+)\s+cm"))
        {
            result.Add((
                double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return result;
    }

    [Fact]
    public void TwoInlineImages_ShareLine_SecondAdvancedByFirstWidth()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(500), Unit.FromPoint(500));
        section.Margins.SetAll(Unit.FromPoint(0));

        var paragraph = section.Blocks.AddParagraph();
        AddImage(paragraph);
        AddImage(paragraph);

        var placements = ImagePlacements(document);
        Assert.Equal(2, placements.Count);

        Assert.Equal(placements[0].Y, placements[1].Y, 3);
        Assert.Equal(placements[0].X + 40, placements[1].X, 3);
    }

    [Fact]
    public void InlineImage_WrapsToNextLine_WhenItDoesNotFit()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(60), Unit.FromPoint(500));
        section.Margins.SetAll(Unit.FromPoint(0));

        var paragraph = section.Blocks.AddParagraph();
        AddImage(paragraph);
        AddImage(paragraph);

        var placements = ImagePlacements(document);
        Assert.Equal(2, placements.Count);

        Assert.Equal(0, placements[0].X, 3);
        Assert.Equal(0, placements[1].X, 3);
        Assert.True(placements[1].Y < placements[0].Y - 1, "second image wrapped to a lower line");
    }
}
