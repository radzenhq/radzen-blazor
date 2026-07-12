#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Layer 2a guard: extracting the cell content measure/position primitive into
// BoxContentLayout must be byte-neutral. This document leans on every path the
// primitive owns: line breaking with cell/column/row alignment fallbacks,
// spacing gaps, vertical alignment offsets, row/column spans, list expansion,
// a nested table and a container. The generator emits no /ID or dates, so two
// independent builds must produce identical bytes.
public class BoxContentLayoutByteNeutralityTests
{
    private static void Fill(Cell cell, string text)
    {
        var paragraph = cell.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = BuildTestSupport.Latin;
        run.Font.Size = 10;
    }

    private static DocumentBuilder BuildTableHeavyDocument()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();

        var panel = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(6),
            Background = Color.FromRgb(240, 240, 255),
        });
        panel.Borders.Width = 0.5;
        var caption = panel.Blocks.AddParagraph();
        caption.Alignment = HorizontalAlignment.Center;
        caption.SpacingAfter = Unit.FromPoint(4);
        var run = caption.Inlines.Add("Container above a table-heavy body");
        run.Font.Name = BuildTestSupport.Latin;
        run.Font.Size = 10;

        var table = section.Blocks.AddTable();
        table.Borders.Width = 0.5;
        table.Columns.Add(Unit.FromPoint(150));
        table.Columns.Add(Unit.FromPoint(150));
        table.Columns[1].Alignment = HorizontalAlignment.Right;

        var first = table.Rows.Add();
        var spanning = first.Cells[0];
        spanning.ColumnSpan = 2;
        spanning.Alignment = HorizontalAlignment.Center;
        Fill(spanning, "Spanning header that wraps onto several lines when the combined column width runs out of room");

        var second = table.Rows.Add();
        var tall = second.Cells[0];
        tall.RowSpan = 2;
        tall.VerticalAlignment = VerticalAlignment.Middle;
        Fill(tall, "middle-aligned rowspan");
        var spaced = second.Cells[1];
        var para = spaced.Blocks.AddParagraph();
        para.SpacingBefore = Unit.FromPoint(6);
        para.SpacingAfter = Unit.FromPoint(3);
        var spacedRun = para.Inlines.Add("spaced right-aligned");
        spacedRun.Font.Name = BuildTestSupport.Latin;
        spacedRun.Font.Size = 10;

        var third = table.Rows.Add();
        var bottom = third.Cells[1];
        bottom.VerticalAlignment = VerticalAlignment.Bottom;
        Fill(bottom, "bottom");

        var fourth = table.Rows.Add();
        var listCell = fourth.Cells[0];
        var list = listCell.Blocks.AddList();
        foreach (var text in new[] { "alpha", "beta" })
        {
            var item = list.Items.Add();
            var itemRun = item.Inlines.Add(text);
            itemRun.Font.Name = BuildTestSupport.Latin;
            itemRun.Font.Size = 10;
        }

        var host = fourth.Cells[1];
        var nested = host.Blocks.AddTable();
        nested.Borders.Width = 0.5;
        nested.Columns.Add(Unit.FromPoint(60));
        nested.Columns.Add(Unit.FromPoint(60));
        var nestedRow = nested.Rows.Add();
        Fill(nestedRow.Cells[0], "n1");
        var nestedRight = nestedRow.Cells[1];
        nestedRight.VerticalAlignment = VerticalAlignment.Bottom;
        Fill(nestedRight, "nested wrapping text");

        return builder;
    }

    [Fact]
    public void TableHeavyDocument_BuildsByteIdenticalTwice()
    {
        var golden = BuildTableHeavyDocument().ToArray();
        var again = BuildTableHeavyDocument().ToArray();

        Assert.Equal(golden, again);
    }
}
