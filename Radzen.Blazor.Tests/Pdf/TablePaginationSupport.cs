#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

internal static class TablePaginationSupport
{
    public const string Family = TableLayoutSupport.Family;

    public static FontCollection Fonts() => TableLayoutSupport.Fonts();

    public static double LineHeight(FontCollection fonts) => TableLayoutSupport.LineHeight(fonts);

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

    public static double Capacity(double lh, int rows) => (rows + 0.4) * lh;

    public static IReadOnlyList<int> BodyRows(TableFragment fragment)
        => [.. fragment.Rows.Where(r => !r.IsHeader).Select(r => r.SourceRow)];

    public static IReadOnlyList<int> HeaderRows(TableFragment fragment)
        => [.. fragment.Rows.Where(r => r.IsHeader).Select(r => r.SourceRow)];
}
