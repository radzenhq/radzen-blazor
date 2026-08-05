#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;
using Radzen.Blazor.Tests.Isolated;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class LayoutCorrectnessRegressionTests
{
    private const string Family = TableLayoutSupport.Family;

    private static Cell Fill(Cell cell, string text, double size = 12)
        => TableLayoutSupport.Fill(cell, text, size);

    [Fact]
    public void ContinuationLineTallerThanPage_PaginationTerminates()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(500, 200);

        var paragraph = new Paragraph();
        var top = paragraph.Inlines.Add("top\n");
        top.Font.Family = Family;
        top.Font.Size = 12;
        var huge = paragraph.Inlines.Add("H");
        huge.Font.Family = Family;
        huge.Font.Size = 600;
        section.Blocks.Add(paragraph);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(2, pages.Length);
        Assert.All(pages, page => Assert.Equal(1, page.Body.Lines.Length));
    }

    [Fact]
    public void BuildWithOversizedContinuationLine_Terminates()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(500), Unit.FromPoint(200));
        section.Margins.SetAll(Unit.FromPoint(0));

        var paragraph = new Paragraph();
        var top = paragraph.Inlines.Add("top\n");
        top.Font.Family = BuildTestSupport.Latin;
        top.Font.Size = 12;
        var huge = paragraph.Inlines.Add("HUGE");
        huge.Font.Family = BuildTestSupport.Latin;
        huge.Font.Size = 600;
        section.Blocks.Add(paragraph);

        Assert.NotEmpty(new DocumentRenderer().ToArray(document));
    }

    [Fact]
    public void ColumnSpanBeyondLastColumn_IsClampedNotDropped()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row = table.Rows.Add();
        Fill(row.Cells[0], "A");
        Fill(row.Cells[1], "WIDE");
        row.Cells[1].ColumnSpan = 5;

        var layout = IsolatedTableLayout.LayoutIsolated(table, 400, fonts);

        var wide = layout.Cells.SingleOrDefault(c => c.Column == 1);
        Assert.True(wide is not null, "cell with overflowing ColumnSpan was dropped from the layout");
        Assert.Equal(1, wide!.Column);
        Assert.Equal(2, wide.ColumnSpan);
        Assert.True(wide.Bounds.Left + wide.Bounds.Width <= layout.Width + 1e-6,
            "clamped cell must stay inside the table");
        Assert.Contains(wide.Lines, l => l.Line.Fragments.Any(f => f.Text.Contains("WIDE", StringComparison.Ordinal)));
    }

    [Fact]
    public void ColumnSpanBeyondLastColumn_TextSurvivesBuild()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();

        var table = section.Blocks.Add(new Table());
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row = table.Rows.Add();
        Fill(row.Cells[0], "First");
        Fill(row.Cells[1], "Spanned");
        row.Cells[1].ColumnSpan = 5;

        var text = BuildTestSupport.Reload(document).ExtractText();
        Assert.Contains("First", text, StringComparison.Ordinal);
        Assert.Contains("Spanned", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TableWithRowsButNoColumns_DoesNotRenderEmpty()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();

        var table = section.Blocks.Add(new Table());
        var row = table.Rows.Add();
        Fill(row.Cells.AddCell(), "Orphan");

        var text = BuildTestSupport.Reload(document).ExtractText();
        Assert.Contains("Orphan", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UnbreakableTokenWiderThanColumn_DoesNotBleedOutsideCell()
    {
        var fonts = TableLayoutSupport.Fonts();
        var token = new string('W', 24);

        Table MakeTable(Table table)
        {
            table.Columns.Add(Unit.FromPoint(60));
            table.Columns.Add(Unit.FromPoint(200));
            var row = table.Rows.Add();
            Fill(row.Cells[0], token);
            Fill(row.Cells[1], "Neighbor");
            return table;
        }

        var layout = IsolatedTableLayout.LayoutIsolated(MakeTable(new Table()), 260, fonts);
        var cell = TableLayoutSupport.CellAt(layout, 0, 0);
        Assert.True(cell.Lines.Length > 0);
        var allLinesFit = cell.Lines.All(l => l.Line.Width <= cell.ContentBox.Width + 0.5);

        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        MakeTable(section.Blocks.Add(new Table()));

        var reader = BuildTestSupport.Read(document);
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, 0));
        var hasClip = content.Contains(" W n", StringComparison.Ordinal)
            || content.Contains(" W\nn", StringComparison.Ordinal);

        Assert.True(allLinesFit || hasClip,
            "over-wide token must be broken to the column width or clipped to the cell box");
    }

    [Fact]
    public void RowSpanCellContent_FitsWithinCoveredRows()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row0 = table.Rows.Add();
        Fill(row0.Cells[0], "S1\nS2\nS3\nS4");
        row0.Cells[0].RowSpan = 2;
        Fill(row0.Cells[1], "R0");

        var row1 = table.Rows.Add();
        Fill(row1.Cells[0], "R1");

        var layout = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var span = layout.Cells.Single(c => c.RowSpan == 2);
        var padding = 2 * row0.Cells[0].Padding.Point;
        var contentHeight = span.Lines.Sum(l => l.Line.Height);

        Assert.True(span.Bounds.Height + 1e-6 >= contentHeight + padding,
            $"spanned cell bounds {span.Bounds.Height:F2} cannot hold its content {contentHeight + padding:F2}");
        Assert.True(layout.RowHeights[0] + layout.RowHeights[1] + 1e-6 >= contentHeight + padding,
            "covered rows together must absorb the spanning cell's height");
    }

    [Fact]
    public void RowSpanGroup_IsNotSplitAcrossTableFragments()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight();

        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row0 = table.Rows.Add();
        Fill(row0.Cells[0], "A0");
        Fill(row0.Cells[1], "B0");

        var row1 = table.Rows.Add();
        Fill(row1.Cells[0], "SPAN");
        row1.Cells[0].RowSpan = 2;
        Fill(row1.Cells[1], "B1");

        var row2 = table.Rows.Add();
        Fill(row2.Cells[0], "B2");

        var row3 = table.Rows.Add();
        Fill(row3.Cells[0], "A3");
        Fill(row3.Cells[1], "B3");

        var layout = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var fragments = IsolatedTablePaginator.Paginate(layout, table, 2.4 * lh);

        int FragmentOf(int sourceRow)
        {
            for (var i = 0; i < fragments.Count; i++)
            {
                if (fragments[i].Rows.Any(r => r.SourceRow == sourceRow))
                {
                    return i;
                }
            }

            return -1;
        }

        Assert.NotEqual(-1, FragmentOf(1));
        Assert.NotEqual(-1, FragmentOf(2));
        Assert.Equal(FragmentOf(1), FragmentOf(2));
    }

    [Fact]
    public void FixedWidthsExceedingAvailable_AutoColumnsClampToNonNegative()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(300));
        table.Columns.Add(Unit.FromPoint(300));
        table.Columns.Add();

        var row = table.Rows.Add();
        Fill(row.Cells[0], "a");
        Fill(row.Cells[1], "b");
        Fill(row.Cells[2], "c");

        var layout = IsolatedTableLayout.LayoutIsolated(table, 400, fonts);

        Assert.All(layout.ColumnWidths, w => Assert.True(w >= 0, $"column width {w:F2} is negative"));
    }
}
