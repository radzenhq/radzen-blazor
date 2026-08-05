#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

internal static class TableLayoutSupport
{
    public const string Family = PaginationSupport.Family;

    public static FontCollection Fonts() => PaginationSupport.Fonts();

    public static Font FontAt(double size) => PaginationSupport.FontAt(size);

    public static double Measure(FontCollection fonts, string text, double size)
        => PaginationSupport.Measure(fonts, text, size);

    public static double LineHeight(double size = 12) => PaginationSupport.LineHeight(size);

    public static Cell Fill(Cell cell, string text, double size = 12)
    {
        cell.Blocks.Clear();
        var p = cell.Blocks.Add(new Paragraph());
        var run = p.Inlines.Add(text);
        run.Font.Family = Family;
        run.Font.Size = size;
        return cell;
    }

    public static LaidOutCell CellAt(LaidOutTable table, int row, int column)
        => table.Cells.Single(c => c.Row == row && c.Column == column);
}
