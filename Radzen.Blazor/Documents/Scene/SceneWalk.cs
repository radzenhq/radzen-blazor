using System;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Scene;

internal readonly record struct SceneFrame
{
    public required double Left { get; init; }

    public required double Delta { get; init; }
}

internal readonly record struct SceneContentBounds
{
    public required double Width { get; init; }

    public required double Left { get; init; }

    public required double Right { get; init; }
}

internal enum SceneLayerKind
{
    Body,
    Header,
    Footer,
}

internal interface ISceneVisitor
{
    void EnterLayer(SceneLayerKind kind)
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

    void AfterLines()
    {
    }

    void AfterInline()
    {
    }

    void EnterBox(LaidOutBox box, in SceneFrame frame, in SceneContentBounds bounds)
    {
    }

    void LeaveBox(LaidOutBox box, in SceneFrame frame)
    {
    }

    void EnterFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
    }

    void Row(in LaidOutRow row, in SceneFrame frame)
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

    void EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneContentBounds bounds)
    {
    }

    void LeaveCell(LaidOutCell cell, in SceneFrame frame)
    {
    }
}

internal static class SceneWalk
{
    public static void Page(LaidOutPage page, ISceneVisitor visitor)
    {
        Layer(SceneLayerKind.Body, page.Body, page.ContentBox.X, visitor);
        Layer(SceneLayerKind.Header, page.HeaderLayer, page.ContentBox.X, visitor);
        Layer(SceneLayerKind.Footer, page.FooterLayer, page.ContentBox.X, visitor);
    }

    public static double LayerTop(LaidOutPage page, SceneLayerKind kind)
        => kind switch
        {
            SceneLayerKind.Body => page.ContentBox.Y,
            SceneLayerKind.Header => page.HeaderTop,
            _ => page.FooterTop,
        };

    private static void Layer(SceneLayerKind kind, LaidOutLayer layer, double left, ISceneVisitor visitor)
    {
        var frame = new SceneFrame { Left = left, Delta = 0 };
        visitor.EnterLayer(kind);

        foreach (var line in layer.Lines)
        {
            visitor.BeginItem(line.ZOrder);
            visitor.Line(line, frame);
            visitor.EndItem();
        }

        if (kind == SceneLayerKind.Body)
        {
            Fragments(layer, frame, visitor);
            Inline(layer, frame, visitor);
        }
        else
        {
            Inline(layer, frame, visitor);
            Fragments(layer, frame, visitor);
        }
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
            visitor.Row(row, table);
            foreach (var placed in row.Cells)
            {
                Cell(placed.Cell, table with { Delta = placed.Delta }, visitor);
            }
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
        foreach (var cell in table.Layout.Cells)
        {
            Cell(cell, cells, visitor);
        }

        visitor.LeaveTable(table, cells);
    }

    private static void Cell(LaidOutCell cell, in SceneFrame frame, ISceneVisitor visitor)
    {
        var bounds = new SceneContentBounds
        {
            Width = cell.ContentBox.Width,
            Left = cell.Bounds.X,
            Right = cell.Bounds.X + cell.Bounds.Width,
        };

        visitor.EnterCell(cell, frame, bounds);
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

        var bounds = new SceneContentBounds
        {
            Width = Math.Max(0, box.Bounds.Width - box.Padding.Horizontal),
            Left = contentInParentSpace ? box.Bounds.X : 0,
            Right = contentInParentSpace ? box.Bounds.X + box.Bounds.Width : box.Bounds.Width,
        };

        visitor.EnterBox(box, frame, bounds);
        Content(box.Content, content, visitor);
        visitor.LeaveBox(box, frame);
    }

    private static void Content<TContent>(TContent content, in SceneFrame frame, ISceneVisitor visitor)
        where TContent : ILaidOutContent<LaidOutTablePlacement>
    {
        foreach (var line in content.Lines)
        {
            visitor.BeginItem(line.ZOrder);
            visitor.Line(line, frame);
            visitor.EndItem();
        }

        visitor.AfterLines();

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

        visitor.AfterInline();

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
