#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// R3 layout-correctness regressions: pagination progress guards, column-span clamping,
// tables without columns, unbreakable-token overflow, rowspan height/split, and
// negative auto column widths. Each test fails on the pre-fix engine.
public class LayoutCorrectnessRegressionTests
{
    private const string Family = TableLayoutSupport.Family;

    private static Cell Fill(Cell cell, string text, double size = 12)
        => TableLayoutSupport.Fill(cell, text, size);

    // (a) A paragraph continuation line taller than the page must not make the paginator
    // emit empty pages forever: it either places the oversized line (one per page) or
    // throws a clear exception. Pre-fix this loops indefinitely, so the work runs on a
    // watchdog task and the test fails on timeout.
    [Fact]
    public void ContinuationLineTallerThanPage_PaginationTerminates()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(500, 200);

        var paragraph = new Paragraph();
        var top = paragraph.Inlines.Add("top\n");
        top.Font.Name = Family;
        top.Font.Size = 12;
        var huge = paragraph.Inlines.Add("H");
        huge.Font.Name = Family;
        huge.Font.Size = 600;
        section.Blocks.Add(paragraph);

        var task = Task.Run(() =>
        {
            try
            {
                return (object)Paginator.Paginate(section, fonts);
            }
            catch (Exception exception)
            {
                return exception;
            }
        });

        Assert.True(
            task.Wait(TimeSpan.FromSeconds(5)),
            "Paginator did not terminate for a continuation line taller than the page.");

        if (task.Result is IReadOnlyList<PaginatedPage> pages)
        {
            Assert.InRange(pages.Count, 1, 3);
            Assert.Equal(2, pages.Sum(p => p.Lines.Count));
            Assert.All(pages, p => Assert.True(p.Lines.Count > 0, "no page may be empty"));
        }
        else
        {
            Assert.IsAssignableFrom<Exception>(task.Result);
        }
    }

    // (a) Same guard end-to-end through Build(): the document must be produced (or a
    // clear exception thrown), never an unbounded page explosion.
    [Fact]
    public void BuildWithOversizedContinuationLine_Terminates()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(500), Unit.FromPoint(200));
        section.Margin = Unit.FromPoint(0);

        var paragraph = new Paragraph();
        var top = paragraph.Inlines.Add("top\n");
        top.Font.Name = BuildTestSupport.Latin;
        top.Font.Size = 12;
        var huge = paragraph.Inlines.Add("HUGE");
        huge.Font.Name = BuildTestSupport.Latin;
        huge.Font.Size = 600;
        section.Blocks.Add(paragraph);

        var task = Task.Run(() =>
        {
            try
            {
                return (object)builder.ToArray();
            }
            catch (Exception exception)
            {
                return exception;
            }
        });

        Assert.True(
            task.Wait(TimeSpan.FromSeconds(10)),
            "Build() did not terminate for a continuation line taller than the page.");

        if (task.Result is byte[] bytes)
        {
            Assert.True(bytes.Length > 0);
        }
        else
        {
            Assert.IsAssignableFrom<Exception>(task.Result);
        }
    }

    // (b) A cell whose ColumnSpan exceeds the remaining columns must be clamped and
    // still laid out, not silently dropped.
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

        var layout = TableLayout.Layout(table, 400, fonts);

        var wide = layout.Cells.SingleOrDefault(c => ReferenceEquals(c.Cell, row.Cells[1]));
        Assert.True(wide is not null, "cell with overflowing ColumnSpan was dropped from the layout");
        Assert.Equal(1, wide!.Column);
        Assert.Equal(2, wide.ColumnSpan);
        Assert.True(wide.Bounds.Left + wide.Bounds.Width <= layout.Width + 1e-6,
            "clamped cell must stay inside the table");
        Assert.Contains(wide.Lines, l => l.Line.Fragments.Any(f => f.Text.Contains("WIDE", StringComparison.Ordinal)));
    }

    // (b) End-to-end: the over-spanning cell's text must survive into the produced PDF.
    [Fact]
    public void ColumnSpanBeyondLastColumn_TextSurvivesBuild()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row = table.Rows.Add();
        Fill(row.Cells[0], "First");
        Fill(row.Cells[1], "Spanned");
        row.Cells[1].ColumnSpan = 5;

        var text = BuildTestSupport.Reload(builder).ExtractText();
        Assert.Contains("First", text, StringComparison.Ordinal);
        Assert.Contains("Spanned", text, StringComparison.Ordinal);
    }

    // (b) A table with rows but no defined columns must not silently render empty:
    // either columns are derived from the widest row or a clear exception is thrown.
    [Fact]
    public void TableWithRowsButNoColumns_DoesNotRenderEmpty()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();

        var table = section.Blocks.AddTable();
        var row = table.Rows.Add();
        Fill(row.Cells.AddCell(), "Orphan");

        try
        {
            var text = BuildTestSupport.Reload(builder).ExtractText();
            Assert.Contains("Orphan", text, StringComparison.Ordinal);
        }
        catch (InvalidOperationException)
        {
            // A clear exception is an acceptable outcome; silent empty output is not.
        }
    }

    // (c) A single token wider than its column must not overpaint the neighbor cell:
    // either it is broken at the column width (every laid-out line fits the content box)
    // or the cell content is clipped in the emitted content stream ("W n").
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

        var layout = TableLayout.Layout(MakeTable(new Table()), 260, fonts);
        var cell = TableLayoutSupport.CellAt(layout, 0, 0);
        Assert.True(cell.Lines.Count > 0);
        var allLinesFit = cell.Lines.All(l => l.Line.Width <= cell.ContentBox.Width + 0.5);

        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        MakeTable(section.Blocks.AddTable());

        var reader = BuildTestSupport.Read(builder);
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, 0));
        var hasClip = content.Contains(" W n", StringComparison.Ordinal)
            || content.Contains(" W\nn", StringComparison.Ordinal);

        Assert.True(allLinesFit || hasClip,
            "over-wide token must be broken to the column width or clipped to the cell box");
    }

    // (d) A RowSpan cell's content must contribute to the combined height of the rows
    // it covers; pre-fix only RowSpan == 1 cells size rows, so tall spanned content
    // overflows its bounds.
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

        var layout = TableLayout.Layout(table, 200, fonts);
        var span = layout.Cells.Single(c => c.RowSpan == 2);
        var padding = 2 * span.Cell.Padding.Point;
        var contentHeight = span.Lines.Sum(l => l.Line.Height);

        Assert.True(span.Bounds.Height + 1e-6 >= contentHeight + padding,
            $"spanned cell bounds {span.Bounds.Height:F2} cannot hold its content {contentHeight + padding:F2}");
        Assert.True(layout.RowHeights[0] + layout.RowHeights[1] + 1e-6 >= contentHeight + padding,
            "covered rows together must absorb the spanning cell's height");
    }

    // (d) A rowspan group must not be split across page fragments: every row covered by
    // a RowSpan > 1 cell moves as a whole to the fragment that holds its start row.
    [Fact]
    public void RowSpanGroup_IsNotSplitAcrossTableFragments()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight(fonts);

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

        var layout = TableLayout.Layout(table, 200, fonts);
        var fragments = TablePaginator.Paginate(layout, table, 2.4 * lh);

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

    // (e) Fixed column widths larger than the available width must not drive auto
    // columns negative; they clamp to zero or more.
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

        var layout = TableLayout.Layout(table, 400, fonts);

        Assert.All(layout.ColumnWidths, w => Assert.True(w >= 0, $"column width {w:F2} is negative"));
    }
}
