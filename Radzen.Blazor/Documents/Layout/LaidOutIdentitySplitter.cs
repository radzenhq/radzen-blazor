using System;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class LaidOutIdentitySplitter
{
    public static LaidOutDocument Split(LaidOutDocument document)
        => new()
        {
            Fonts = document.Fonts,
            Pages = Map(document.Pages, Page),
            Semantics = document.Semantics,
            Info = document.Info,
        };

    private static PaginatedPage Page(PaginatedPage page)
        => page with
        {
            Body = Layer(page.Body),
            HeaderLayer = Layer(page.HeaderLayer),
            FooterLayer = Layer(page.FooterLayer),
        };

    private static PageLayer Layer(PageLayer layer)
        => layer with
        {
            Tables = Map(layer.Tables, PositionedTable),
            Boxes = Map(layer.Boxes, PositionedBox),
        };

    private static PositionedTableFragment PositionedTable(PositionedTableFragment positioned)
        => positioned with
        {
            Layout = Table(positioned.Layout),
            Fragment = Fragment(positioned.Fragment),
            Rows = Map(positioned.Rows, PlacedRow),
        };

    private static PositionedBox PositionedBox(PositionedBox box)
        => box with { Content = Content(box.Content) };

    private static PlacedRow PlacedRow(PlacedRow row)
        => row with { Cells = Map(row.Cells, PlacedCell) };

    private static PlacedCell PlacedCell(PlacedCell placed)
        => placed with { Cell = Cell(placed.Cell) };

    private static TableFragment Fragment(TableFragment fragment)
        => fragment with { };

    private static LaidOutTable Table(LaidOutTable table)
        => new()
        {
            ColumnWidths = table.ColumnWidths,
            RowHeights = table.RowHeights,
            Width = table.Width,
            Height = table.Height,
            Cells = Map(table.Cells, Cell),
            Decoration = table.Decoration,
            Source = table.Source,
        };

    private static LaidOutCell Cell(LaidOutCell cell)
        => cell with
        {
            Tables = Map(cell.Tables, NestedTable),
            Boxes = Map(cell.Boxes, NestedBox),
        };

    private static LaidOutBoxContent Content(in LaidOutBoxContent content)
        => new()
        {
            Height = content.Height,
            Lines = content.Lines,
            Images = content.Images,
            CodeSymbols = content.CodeSymbols,
            Tables = Map(content.Tables, NestedTable),
            Boxes = Map(content.Boxes, NestedBox),
        };

    private static LaidOutNestedTable NestedTable(LaidOutNestedTable table)
        => table with { Layout = Table(table.Layout) };

    private static LaidOutNestedBox NestedBox(LaidOutNestedBox box)
        => box with { Content = Content(box.Content) };

    private static ImmutableArray<TResult> Map<TSource, TResult>(
        ImmutableArray<TSource> source,
        Func<TSource, TResult> map)
    {
        var result = ImmutableArray.CreateBuilder<TResult>(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            result.Add(map(source[i]));
        }

        return result.MoveToImmutable();
    }
}
