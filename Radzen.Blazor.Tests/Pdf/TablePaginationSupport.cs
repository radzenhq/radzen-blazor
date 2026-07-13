#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Shared fixture for the L4b table-pagination contract tests. Builds on the already-merged
// L4a table layout (TableLayout.Layout -> LaidOutTable) and reuses TableLayoutSupport for
// deterministic Liberation Sans metrics; nothing is hardcoded from memory. No PDF bytes.
//
// PINNED INTERNAL API (namespace Radzen.Documents.Pdf, reachable via InternalsVisibleTo):
//
//   static IReadOnlyList<TableFragment> TablePaginator.Paginate(
//       LaidOutTable layout, Table source, double availableHeight)
//
//   TableFragment {
//       int Number;                              // 1-based fragment index
//       IReadOnlyList<FragmentRow> Rows;         // repeated header rows first, then body rows
//       int HeaderRowCount;                      // count of leading repeated header rows
//       double Height;                           // == sum of Rows[].Height (bottom border sits here)
//   }
//
//   FragmentRow {
//       int SourceRow;    // index into source.Rows and layout.RowHeights
//       bool IsHeader;    // true for the repeated header rows carried at the top of the fragment
//       double Y;         // fragment-local top, origin 0, increasing downward
//       double Height;    // == layout.RowHeights[SourceRow] (rows are ATOMIC, never split)
//   }
//
// PINNED SEMANTICS (all numeric):
//   * Header set = every source row with Row.IsHeader == true, taken in source order. These rows
//     REPEAT verbatim (same SourceRow, same Height) at the top of every fragment.
//   * Body set = the remaining rows, in source order. Each body row appears exactly once across all
//     fragments, in order, never duplicated and never split (atomic; KeepTogether is implicit at row
//     granularity).
//   * Packing: a fragment begins with all header rows, then greedily appends body rows while the
//     running height (headers + already-placed body) plus the next row's height stays <= availableHeight.
//     A body row that would overflow starts a new fragment.
//   * Oversized row: a single body row whose height alone (with the repeated headers) exceeds
//     availableHeight still occupies its own fragment; that fragment's Height may exceed availableHeight.
//     The pagination never loops and never drops a row.
//   * Fragment-local geometry: Rows[0].Y == 0; each subsequent Y == previous Y + previous Height;
//     Height == last row bottom == sum of row heights. The header rows always occupy [0, HeaderRowCount)
//     and the first body row's Y == sum of header heights.
//   * Border resolution at a break: the bottom border of a fragment sits at y == Height (its last row's
//     bottom), and the next fragment redraws the header at its top (Rows[0].IsHeader, Y == 0).
internal static class TablePaginationSupport
{
    public const string Family = TableLayoutSupport.Family;

    public static FontCollection Fonts() => TableLayoutSupport.Fonts();

    public static double LineHeight(FontCollection fonts) => TableLayoutSupport.LineHeight(fonts);

    // One fixed 300pt column, `headers` header rows then `bodies` body rows. Every cell is a single
    // short Liberation line with zero padding, so the column never wraps and every row height == lh.
    public static Table Build(int headers, int bodies)
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(300));
        for (var i = 0; i < headers; i++)
        {
            var row = table.Rows.Add();
            row.IsHeader = true;
            TableLayoutSupport.Fill(row.Cells[0], $"H{i}");
        }

        for (var i = 0; i < bodies; i++)
        {
            var row = table.Rows.Add();
            TableLayoutSupport.Fill(row.Cells[0], $"R{i}");
        }

        return table;
    }

    // Appends a body row that is `lines` single-line paragraphs tall -> row height == lines * lh.
    public static Row AddTallRow(Table table, int lines, string prefix)
    {
        var row = table.Rows.Add();
        var cell = row.Cells[0];
        cell.Blocks.Clear();
        for (var i = 0; i < lines; i++)
        {
            var p = cell.Blocks.AddParagraph();
            var run = p.Inlines.Add($"{prefix}{i}");
            run.Font.Name = Family;
            run.Font.Size = 12;
        }

        return row;
    }

    // Available height that fits exactly `rows` unit-height rows with float slack.
    public static double Capacity(double lh, int rows) => (rows + 0.4) * lh;

    public static IReadOnlyList<int> BodyRows(TableFragment fragment)
        => [.. fragment.Rows.Where(r => !r.IsHeader).Select(r => r.SourceRow)];

    public static IReadOnlyList<int> HeaderRows(TableFragment fragment)
        => [.. fragment.Rows.Where(r => r.IsHeader).Select(r => r.SourceRow)];
}
