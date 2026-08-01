using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class PageRenderContext(
    PagePlan plan,
    TextLineRecorder text,
    CodeSymbolRecorder codeSymbols,
    ImageRecorder images,
    TableRecorder tables,
    BoxRecorder boxes)
{
    public PagePlan Plan { get; } = plan;
    public int Layer { get; set; }
    private PaintStackOrder? stackParent;
    private Matrix? stackTransform;
    private readonly Stack<Matrix?> stackTransforms = new();

    public StackMark BeginStack(int zOrder, in PdfRect bounds, Matrix? transform = null)
    {
        stackTransforms.Push(stackTransform);
        if (transform is { } topDown)
        {
            stackTransform = BottomUpSpace.FlipVertical(topDown, Plan.Size.Height.Point);
        }

        var order = new PaintStackOrder(stackParent, zOrder);
        stackParent = order;
        return Plan.BeginStack(
            Layer,
            order,
            stackTransform is { } active ? TransformBounds(bounds, active) : bounds);
    }

    public void EndStack(in StackMark mark)
    {
        Plan.EndStack(mark);
        stackParent = mark.Stack.Order.Parent;
        stackTransform = stackTransforms.Pop();
    }

    private static PdfRect TransformBounds(in PdfRect bounds, in Matrix transform)
    {
        var top = bounds.Top;
        var right = bounds.Right;
        var corners = new[]
        {
            transform.Transform(bounds.Left, bounds.Bottom),
            transform.Transform(right, bounds.Bottom),
            transform.Transform(bounds.Left, top),
            transform.Transform(right, top),
        };
        var minX = corners[0].X;
        var maxX = corners[0].X;
        var minY = corners[0].Y;
        var maxY = corners[0].Y;
        for (var i = 1; i < corners.Length; i++)
        {
            minX = Math.Min(minX, corners[i].X);
            maxX = Math.Max(maxX, corners[i].X);
            minY = Math.Min(minY, corners[i].Y);
            maxY = Math.Max(maxY, corners[i].Y);
        }

        return new PdfRect(minX, minY, maxX, maxY);
    }
    public TextLineRecorder Text { get; } = text;
    public CodeSymbolRecorder CodeSymbols { get; } = codeSymbols;
    public ImageRecorder Images { get; } = images;
    public TableRecorder Tables { get; } = tables;
    public BoxRecorder Boxes { get; } = boxes;
}
