#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class TableRowEmissionTests
{
    private static Document Author()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(20));

        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        for (var r = 0; r < 4; r++)
        {
            var row = table.Rows.Add();
            row.Cells[0].Blocks.AddParagraph($"r{r}c0");
            row.Cells[1].Blocks.AddParagraph($"r{r}c1");
        }

        return document;
    }

    private static byte[] PageBytes(Document document)
        => ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0);

    private static List<string> TextOrder(byte[] content)
    {
        var text = Encoding.Latin1.GetString(content);
        var order = new List<string>();
        foreach (Match m in Regex.Matches(text, @"\((r\d+c\d+)\)\s*Tj"))
        {
            order.Add(m.Groups[1].Value);
        }

        return order;
    }

    [Fact]
    public void MultiRowTable_EmitsCellsRowMajorInLayoutOrder()
    {
        var order = TextOrder(PageBytes(Author()));
        Assert.Equal(
            new[] { "r0c0", "r0c1", "r1c0", "r1c1", "r2c0", "r2c1", "r3c0", "r3c1" },
            order);
    }

    [Fact]
    public void MultiRowTable_ContentStreamIsByteStableAcrossBuilds()
        => Assert.Equal(PageBytes(Author()), PageBytes(Author()));
}
