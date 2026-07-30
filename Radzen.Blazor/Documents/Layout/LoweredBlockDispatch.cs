using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal interface ILoweredBlockHandler<in TContext, out TResult>
{
    TResult Paragraph(Paragraph paragraph, TContext context);

    TResult Table(Table table, TContext context);

    TResult Image(Image image, TContext context);

    TResult Container(Container container, TContext context);

    TResult CodeSymbol(Block block, TContext context);

    TResult PageBreak(PageBreak pageBreak, TContext context);
}

internal static class LoweredBlockDispatch
{
    internal static void Place(Block block, FlowPlacementEngine engine, int index)
        => Dispatch(block, engine, index);

    internal static TResult Dispatch<TContext, TResult>(
        Block block,
        ILoweredBlockHandler<TContext, TResult> handler,
        TContext context)
        => block switch
        {
            Paragraph paragraph => handler.Paragraph(paragraph, context),
            Table table => handler.Table(table, context),
            Image image => handler.Image(image, context),
            Container container => handler.Container(container, context),
            Barcode or QrCode => handler.CodeSymbol(block, context),
            PageBreak pageBreak => handler.PageBreak(pageBreak, context),
            _ => throw Unsupported(block),
        };

    internal static NotSupportedException Unsupported(Block block)
        => new(
            $"Block type '{block.GetType().FullName}' reached layout before lowering. "
            + "Lowered layout accepts only Paragraph, Table, Image, Container, Barcode, QrCode and PageBreak.");

    internal static LaidOutTable LayoutTable(
        Table table,
        double contentWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext lowering,
        LayoutCaptureContext capture)
    {
        var indent = lowering.BlockIndent(table);
        return TableLayout.Layout(
            table,
            Math.Max(0, contentWidth - indent - table.LeftIndent.Point),
            fonts,
            measureImage,
            lowering,
            capture,
            indent);
    }
}
