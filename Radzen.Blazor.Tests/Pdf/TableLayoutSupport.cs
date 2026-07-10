#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// Shared fixture for the L4a table-layout contract tests. Everything numeric is
// derived from the already-merged FontCollection + LineBreaker using deterministic
// Liberation Sans metrics (unitsPerEm 2048, hhea ascent 1854 / descent -434 /
// lineGap 67); nothing is hardcoded from memory. No PDF bytes are produced.
//
// Pinned INTERNAL table layout (namespace Radzen.Documents.Pdf, reachable via
// InternalsVisibleTo). It resolves an L1 Table against an available width and a
// FontCollection into a fully positioned, immutable model:
//
//   static LaidOutTable TableLayout.Layout(Table table, double availableWidth,
//       FontCollection fonts)
//
//   LaidOutTable {
//       IReadOnlyList<double> ColumnWidths;   // resolved, in points, one per column
//       IReadOnlyList<double> RowHeights;     // resolved, in points, one per row
//       double Width;                          // = sum(ColumnWidths)
//       double Height;                         // = sum(RowHeights)
//       IReadOnlyList<LaidOutCell> Cells;      // visible cells only (covered slots omitted)
//   }
//
//   LaidOutCell {
//       Cell Cell;            // source DOM cell
//       int Row; int Column;  // 0-based top-left grid slot it occupies
//       int ColumnSpan; int RowSpan;
//       Rect Bounds;          // full cell rectangle in table-local coords (origin top-left)
//       Rect ContentBox;      // Bounds inset by Cell.Padding on every edge
//       IReadOnlyList<LaidOutLine> Lines;   // laid-out content lines, top to bottom
//   }
//
//   LaidOutLine {
//       LineBox Line;    // the L2 line box (fragments carry alignment via XOffset)
//       Block Source;    // originating block
//       double X;        // absolute x of the line origin (== ContentBox.Left)
//       double Y;        // absolute y of the line top, in table-local coords
//   }
//
// Coordinate space: table-local, origin at the table's top-left corner, y increases
// downward. Column x = sum of the widths of the columns to its left. Row y = sum of
// the heights of the rows above it.
//
// PINNED SEMANTICS
//
// Column sizing (content-independent):
//   * fixedSum = sum of the Width.Point of every column whose Width is non-null.
//   * total = table.Width?.Point ?? availableWidth, EXCEPT when there are no auto
//     columns, in which case total = fixedSum (fixed widths are honored verbatim and
//     availableWidth is ignored).
//   * Every AUTO column (Width == null) receives exactly (total - fixedSum) / autoCount
//     points - a pure equal split with no rounding, so the remainder is distributed
//     evenly and fractionally. A null Width IS auto.
//
// Grid placement (HTML-style sequential occupancy):
//   * Cells fill the grid left-to-right, top-to-bottom. Each cell consumes ColumnSpan
//     columns starting at the first free column in its row, and reserves that block of
//     columns for the next (RowSpan - 1) rows. Cells that would spill past the last
//     column are dropped (omitted from Cells).
//   * A covered slot produces no LaidOutCell.
//
// Row height:
//   * A cell's content height = the sum of the heights of all its laid-out lines
//     (every paragraph block broken at the cell content-box width).
//   * rowHeight[r] = max over cells with RowSpan == 1 anchored in row r of
//     (contentHeight + 2 * Padding). Cells with RowSpan > 1 do NOT influence any single
//     row's height.
//
// Cell geometry:
//   * Bounds spans its column block (summed ColumnSpan widths) x its row block
//     (summed RowSpan heights).
//   * ContentBox = Bounds inset by Cell.Padding on all four edges.
//   * Content lines are line-broken at ContentBox.Width.
//
// Vertical alignment (VerticalAlignment on the cell):
//   * factor = Top 0, Middle 0.5, Bottom 1.
//   * The content block (height = sum of line heights) is offset within ContentBox by
//     (ContentBox.Height - contentHeight) * factor. The first line's Y = ContentBox.Top
//     + that offset; each subsequent line Y adds the previous line's height.
//
// Horizontal alignment:
//   * effective = Column.Alignment ?? Cell.Alignment. It is applied when breaking the
//     cell content at ContentBox.Width, so LineBox fragment XOffset reflects it
//     (Left -> 0, Right -> width - lineWidth, Center -> (width - lineWidth) / 2).
internal static class TableLayoutSupport
{
    public const string Family = "Liberation Sans";

    public static FontCollection Fonts()
    {
        var fonts = new FontCollection();
        fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return fonts;
    }

    public static Font FontAt(double size) => new() { Name = Family, Size = size };

    public static double Measure(FontCollection fonts, string text, double size)
        => fonts.MeasureText(text, FontAt(size));

    // Single-line height for a Liberation paragraph, taken straight from the L2
    // LineBreaker so product and test share metrics.
    public static double LineHeight(FontCollection fonts, double size = 12)
    {
        var p = new Paragraph();
        var run = p.Inlines.Add("Xg");
        run.Font.Name = Family;
        run.Font.Size = size;
        return LineBreaker.Break(p, 100000, fonts)[0].Height;
    }

    // Replaces a cell's content with a single Liberation-family paragraph.
    public static Cell Fill(Cell cell, string text, double size = 12)
    {
        cell.Blocks.Clear();
        var p = cell.Blocks.AddParagraph();
        var run = p.Inlines.Add(text);
        run.Font.Name = Family;
        run.Font.Size = size;
        return cell;
    }

    public static LaidOutCell CellAt(LaidOutTable table, int row, int column)
        => table.Cells.Single(c => c.Row == row && c.Column == column);
}
