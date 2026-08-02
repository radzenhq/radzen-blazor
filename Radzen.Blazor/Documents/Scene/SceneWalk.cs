using System;
using System.Collections.Immutable;
using Radzen.Documents.Core;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Scene;

internal readonly record struct SceneFrame
{
    public required double Left { get; init; }

    public required double Delta { get; init; }
}

internal readonly record struct SceneClip
{
    public required Rect Bounds { get; init; }

    public required bool ClipsLines { get; init; }

    public required bool ClipsInline { get; init; }
}

internal enum SceneLayerKind
{
    Body,
    Header,
    Footer,
}

internal interface ISceneVisitor
{
    void BeginDocument(LaidOutDocument document)
    {
    }

    void EndDocument(LaidOutDocument document)
    {
    }

    void BeginPage(LaidOutPage page, int index)
    {
    }

    void EndPage(LaidOutPage page, int index)
    {
    }

    void EnterLayer(SceneLayerKind kind, double top)
    {
    }

    void LeaveLayer(SceneLayerKind kind)
    {
    }

    void BeginItem(int zOrder)
    {
    }

    void EndItem()
    {
    }

    void Line(in LaidOutLine line, in SceneFrame frame)
    {
    }

    void Image(in LaidOutImage image, in SceneFrame frame)
    {
    }

    void CodeSymbol(in LaidOutCodeSymbol codeSymbol, in SceneFrame frame)
    {
    }

    void EnterBox(LaidOutBox box, in SceneFrame frame, in SceneClip clip)
    {
    }

    void LeaveBox(LaidOutBox box, in SceneFrame frame)
    {
    }

    void EnterFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
    }

    void LeaveFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
    }

    void EnterTable(in LaidOutTablePlacement table, in SceneFrame frame)
    {
    }

    void LeaveTable(in LaidOutTablePlacement table, in SceneFrame frame)
    {
    }

    void EnterRow(in LaidOutRow row, in SceneFrame frame)
    {
    }

    void LeaveRow(in LaidOutRow row, in SceneFrame frame)
    {
    }

    void EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneClip clip)
    {
    }

    void LeaveCell(LaidOutCell cell, in SceneFrame frame)
    {
    }

    void Link(in LaidOutLink link)
    {
    }

    void Anchor(in LaidOutAnchor anchor)
    {
    }

    void Watermark(LaidOutWatermark watermark)
    {
    }
}

internal static class SceneWalk
{
    private const double OverflowTolerance = 0.01;

    public static void Document(LaidOutDocument document, ISceneVisitor visitor)
    {
        visitor.BeginDocument(document);
        for (var index = 0; index < document.Pages.Length; index++)
        {
            Page(document.Pages[index], index, visitor);
        }

        visitor.EndDocument(document);
    }

    public static void Page(LaidOutPage page, ISceneVisitor visitor) => Page(page, 0, visitor);

    public static void Page(LaidOutPage page, int index, ISceneVisitor visitor)
    {
        visitor.BeginPage(page, index);

        Layer(SceneLayerKind.Body, page.Body, page, visitor);
        Layer(SceneLayerKind.Header, page.HeaderLayer, page, visitor);
        Layer(SceneLayerKind.Footer, page.FooterLayer, page, visitor);

        foreach (var link in page.Links)
        {
            visitor.Link(link);
        }

        foreach (var anchor in page.Anchors)
        {
            visitor.Anchor(anchor);
        }

        if (page.Watermark is { } watermark)
        {
            visitor.Watermark(watermark);
        }

        visitor.EndPage(page, index);
    }

    private static double LayerTop(LaidOutPage page, SceneLayerKind kind)
        => kind switch
        {
            SceneLayerKind.Body => page.ContentBox.Y,
            SceneLayerKind.Header => page.HeaderTop,
            _ => page.FooterTop,
        };

    private static void Layer(SceneLayerKind kind, LaidOutLayer layer, LaidOutPage page, ISceneVisitor visitor)
    {
        var frame = new SceneFrame { Left = page.ContentBox.X, Delta = 0 };
        visitor.EnterLayer(kind, LayerTop(page, kind));

        foreach (var line in layer.Lines)
        {
            visitor.BeginItem(line.ZOrder);
            visitor.Line(line, frame);
            visitor.EndItem();
        }

        Fragments(layer, frame, visitor);
        Inline(layer, frame, visitor);

        visitor.LeaveLayer(kind);
    }

    private static void Inline(LaidOutLayer layer, in SceneFrame frame, ISceneVisitor visitor)
    {
        foreach (var image in layer.Images)
        {
            visitor.BeginItem(image.ZOrder);
            visitor.Image(image, frame);
            visitor.EndItem();
        }

        foreach (var codeSymbol in layer.CodeSymbols)
        {
            visitor.BeginItem(codeSymbol.ZOrder);
            visitor.CodeSymbol(codeSymbol, frame);
            visitor.EndItem();
        }
    }

    private static void Fragments(LaidOutLayer layer, in SceneFrame frame, ISceneVisitor visitor)
    {
        var cursor = OrderedMerge.ByOrder(layer.Tables, static t => t.ZOrder, layer.Boxes, static b => b.ZOrder);
        while (cursor.MoveNext())
        {
            if (cursor.IsTable)
            {
                var fragment = layer.Tables[cursor.TableIndex];
                visitor.BeginItem(fragment.ZOrder);
                Fragment(fragment, frame, visitor);
                visitor.EndItem();
            }
            else
            {
                var box = layer.Boxes[cursor.BoxIndex];
                visitor.BeginItem(box.ZOrder);
                Box(box, frame, contentInParentSpace: true, visitor);
                visitor.EndItem();
            }
        }
    }

    private static void Fragment(in LaidOutTableFragment fragment, in SceneFrame frame, ISceneVisitor visitor)
    {
        var table = new SceneFrame
        {
            Left = frame.Left + fragment.Layout.Decoration.LeftIndent,
            Delta = frame.Delta,
        };

        visitor.EnterFragment(fragment, table);
        foreach (var row in fragment.Rows)
        {
            visitor.EnterRow(row, table);
            foreach (var placed in row.Cells)
            {
                Cell(placed.Cell, table with { Delta = placed.Delta }, visitor);
            }

            visitor.LeaveRow(row, table);
        }

        visitor.LeaveFragment(fragment, table);
    }

    private static void Table(in LaidOutTablePlacement table, in SceneFrame frame, ISceneVisitor visitor)
    {
        var cells = new SceneFrame
        {
            Left = frame.Left + table.X + table.Layout.Decoration.LeftIndent,
            Delta = frame.Delta + table.Y,
        };

        visitor.EnterTable(table, cells);

        var layout = table.Layout;
        var heights = layout.RowHeights;
        var y = 0.0;
        for (var index = 0; index < heights.Length; index++)
        {
            var row = new LaidOutRow
            {
                SourceRow = index,
                IsHeader = false,
                Y = y,
                Height = heights[index],
                Background = layout.Decoration.RowBackground(index),
                Cells = RowCells(layout, index, cells.Delta),
            };

            visitor.EnterRow(row, cells);
            foreach (var placed in row.Cells)
            {
                Cell(placed.Cell, cells, visitor);
            }

            visitor.LeaveRow(row, cells);
            y += heights[index];
        }

        visitor.LeaveTable(table, cells);
    }

    private static ImmutableArray<LaidOutCellPlacement> RowCells(LaidOutTable layout, int row, double delta)
    {
        var placed = ImmutableArray.CreateBuilder<LaidOutCellPlacement>();
        foreach (var cell in layout.Cells)
        {
            if (cell.Row == row)
            {
                placed.Add(new LaidOutCellPlacement { Cell = cell, Delta = delta });
            }
        }

        return placed.ToImmutable();
    }

    private static void Cell(LaidOutCell cell, in SceneFrame frame, ISceneVisitor visitor)
    {
        var clip = Clip(
            cell,
            frame,
            cell.Bounds,
            cell.ContentBox.Width,
            cell.Bounds.X,
            cell.Bounds.X + cell.Bounds.Width);

        visitor.EnterCell(cell, frame, clip);
        Content(cell, frame, visitor);
        visitor.LeaveCell(cell, frame);
    }

    private static void Box(LaidOutBox box, in SceneFrame frame, bool contentInParentSpace, ISceneVisitor visitor)
    {
        var content = new SceneFrame
        {
            Left = contentInParentSpace ? frame.Left : frame.Left + box.Bounds.X,
            Delta = frame.Delta + box.Bounds.Y,
        };

        var clip = Clip(
            box.Content,
            frame,
            box.Bounds,
            Math.Max(0, box.Bounds.Width - box.Padding.Horizontal),
            contentInParentSpace ? box.Bounds.X : 0,
            contentInParentSpace ? box.Bounds.X + box.Bounds.Width : box.Bounds.Width);

        visitor.EnterBox(box, frame, clip);
        Content(box.Content, content, visitor);
        visitor.LeaveBox(box, frame);
    }

    private static SceneClip Clip<TContent>(
        TContent content,
        in SceneFrame frame,
        in Rect bounds,
        double contentWidth,
        double left,
        double right)
        where TContent : ILaidOutContent<LaidOutTablePlacement>
        => new()
        {
            Bounds = new Rect(
                frame.Left + bounds.X,
                frame.Delta + bounds.Y,
                bounds.Width,
                bounds.Height),
            ClipsLines = LinesOverflow(content, contentWidth),
            ClipsInline = InlineOverflows(content, left, right),
        };

    private static bool LinesOverflow<TContent>(TContent content, double width)
        where TContent : ILaidOutContent<LaidOutTablePlacement>
    {
        foreach (var line in content.Lines)
        {
            if (line.Line.Width > width + OverflowTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool InlineOverflows<TContent>(TContent content, double left, double right)
        where TContent : ILaidOutContent<LaidOutTablePlacement>
    {
        foreach (var image in content.Images)
        {
            if (Overflows(image.X, image.Width, left, right))
            {
                return true;
            }
        }

        foreach (var codeSymbol in content.CodeSymbols)
        {
            if (Overflows(codeSymbol.X, codeSymbol.Width, left, right))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Overflows(double x, double width, double left, double right)
        => x < left - OverflowTolerance || x + width > right + OverflowTolerance;

    private static void Content<TContent>(TContent content, in SceneFrame frame, ISceneVisitor visitor)
        where TContent : ILaidOutContent<LaidOutTablePlacement>
    {
        foreach (var line in content.Lines)
        {
            visitor.BeginItem(line.ZOrder);
            visitor.Line(line, frame);
            visitor.EndItem();
        }

        foreach (var image in content.Images)
        {
            visitor.BeginItem(image.ZOrder);
            visitor.Image(image, frame);
            visitor.EndItem();
        }

        foreach (var codeSymbol in content.CodeSymbols)
        {
            visitor.BeginItem(codeSymbol.ZOrder);
            visitor.CodeSymbol(codeSymbol, frame);
            visitor.EndItem();
        }

        var cursor = OrderedMerge.ByOrder(content.Tables, static t => t.ZOrder, content.Boxes, static b => b.ZOrder);
        while (cursor.MoveNext())
        {
            if (cursor.IsTable)
            {
                var table = content.Tables[cursor.TableIndex];
                visitor.BeginItem(table.ZOrder);
                Table(table, frame, visitor);
                visitor.EndItem();
            }
            else
            {
                var box = content.Boxes[cursor.BoxIndex];
                visitor.BeginItem(box.ZOrder);
                Box(box, frame, contentInParentSpace: false, visitor);
                visitor.EndItem();
            }
        }
    }
}
