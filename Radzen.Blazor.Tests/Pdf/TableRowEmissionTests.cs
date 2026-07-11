#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The per-row cell grouping that replaced the O(rows*cells) full scan in table
// emission must not change output: cells still emit row-major in their layout order,
// and the produced content stream is byte-for-byte stable across builds.
public class TableRowEmissionTests
{
    private static DocumentBuilder Author()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(400));
        section.Margin = Unit.FromPoint(20);

        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        for (var r = 0; r < 4; r++)
        {
            var row = table.Rows.Add();
            row.Cells[0].Blocks.AddParagraph($"r{r}c0");
            row.Cells[1].Blocks.AddParagraph($"r{r}c1");
        }

        return builder;
    }

    private static byte[] PageBytes(DocumentBuilder builder)
        => ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0);

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
