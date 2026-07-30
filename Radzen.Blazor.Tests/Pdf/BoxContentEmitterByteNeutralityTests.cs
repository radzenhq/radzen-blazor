#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class BoxContentEmitterByteNeutralityTests
{
    private static void Fill(Cell cell, string text)
    {
        var paragraph = cell.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Family = BuildTestSupport.Latin;
        run.Font.Size = 10;
    }

    private static Document BuildDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        section.Blocks.AddParagraph("body one");
        section.Blocks.AddPageBreak();
        section.Blocks.AddParagraph("body two");

        var footer = section.Footer.Blocks.AddTable();
        footer.Columns.Add(Unit.FromPoint(160));
        footer.Columns.Add(Unit.FromPoint(160));
        var band = footer.Rows.Add();

        var rounded = band.Cells[0];
        rounded.Background = Color.FromRgb(230, 240, 250);
        rounded.Borders.Width = 0.5;
        var field = rounded.Blocks.AddParagraph();
        var page = field.Inlines.Add("Page ");
        page.Font.Family = BuildTestSupport.Latin;
        page.Font.Size = 10;
        var number = field.Inlines.Add(new PageNumberField());
        number.Font.Family = BuildTestSupport.Latin;
        number.Font.Size = 10;
        var of = field.Inlines.Add(" of ");
        of.Font.Family = BuildTestSupport.Latin;
        of.Font.Size = 10;
        var count = field.Inlines.Add(new PageCountField());
        count.Font.Family = BuildTestSupport.Latin;
        count.Font.Size = 10;

        var media = band.Cells[1];
        var image = media.Blocks.AddImage(
            new MemoryStream(PdfTestResources.ReadAllBytes("Images/rgb.png")));
        image.Width = Unit.FromPoint(40);
        image.Height = Unit.FromPoint(20);

        var body = section.Blocks.AddTable();
        body.Borders.Width = 0.5;
        body.Columns.Add(Unit.FromPoint(150));
        body.Columns.Add(Unit.FromPoint(150));
        var row = body.Rows.Add();

        var host = row.Cells[0];
        host.Background = Color.FromRgb(250, 235, 235);
        Fill(host, "rounded host");
        var nested = host.Blocks.AddTable();
        nested.Borders.Width = 0.5;
        nested.Columns.Add(Unit.FromPoint(60));
        nested.Columns.Add(Unit.FromPoint(60));
        var nestedRow = nested.Rows.Add();
        Fill(nestedRow.Cells[0], "n1");
        Fill(nestedRow.Cells[1], "nested wrapping text");

        Fill(row.Cells[1], "plain neighbor");

        return document;
    }

    [Fact]
    public void BandFieldsImagesNestedTablesAndRoundedCells_BuildByteIdenticalTwice()
    {
        var golden = new DocumentRenderer().ToArray(BuildDocument());
        var again = new DocumentRenderer().ToArray(BuildDocument());

        Assert.Equal(golden, again);
    }
}
