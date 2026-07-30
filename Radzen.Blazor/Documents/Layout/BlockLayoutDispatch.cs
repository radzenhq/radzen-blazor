using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal interface IBlockLayoutHandler<in TContext, out TResult>
{
    TResult Paragraph(Paragraph paragraph, TContext context);

    TResult Table(Table table, TContext context);

    TResult Image(Image image, TContext context);

    TResult Container(Container container, TContext context);

    TResult CodeSymbol(Block block, TContext context);

    TResult PageBreak(PageBreak pageBreak, TContext context);

    TResult Unsupported(Block block, TContext context);
}

internal static class BlockLayoutDispatch
{
    internal static TResult Dispatch<TContext, TResult>(
        Block block,
        IBlockLayoutHandler<TContext, TResult> handler,
        TContext context)
        => block switch
        {
            Paragraph paragraph => handler.Paragraph(paragraph, context),
            Table table => handler.Table(table, context),
            Image image => handler.Image(image, context),
            Container container => handler.Container(container, context),
            Barcode or QrCode => handler.CodeSymbol(block, context),
            PageBreak pageBreak => handler.PageBreak(pageBreak, context),
            _ => handler.Unsupported(block, context),
        };

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
            indent,
            capture);
    }
}
