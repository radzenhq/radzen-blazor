using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal static class BoxContentRecorder
{
    public static void EmitBoxContent(
        PageRenderContext context,
        in LaidOutBoxContent content,
        double contentWidth,
        double boundsLeft,
        double boundsRight,
        in PdfRect clip,
        double radius,
        double opacity,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? artifact)
    {
        var plan = context.Plan;
        var contentClip = clip;

        var contentMark = radius > 0 ? plan.Mark() : default;

        var firstText = plan.Texts.Count;
        var textOverflows = false;
        foreach (var line in content.Lines)
        {
            textOverflows |= EmitLine(
                context, line, contentWidth, opacity, element, left, contentTop, delta, artifact);
        }

        if (textOverflows)
        {
            for (var t = firstText; t < plan.Texts.Count; t++)
            {
                plan.Texts[t] = plan.Texts[t] with { Clip = contentClip };
            }
        }

        var firstImage = plan.Images.Count;
        var firstFill = plan.Fills.Count;
        var firstCodeSymbolText = plan.Texts.Count;
        var contentOverflows = false;
        foreach (var image in content.Images)
        {
            contentOverflows |= EmitImage(
                context, image, boundsLeft, boundsRight, opacity, element,
                left, contentTop, delta, artifact);
        }

        foreach (var codeSymbol in content.CodeSymbols)
        {
            contentOverflows |= EmitCodeSymbol(
                context, codeSymbol, boundsLeft, boundsRight,
                left, contentTop, delta, artifact);
        }

        if (contentOverflows)
        {
            for (var i = firstImage; i < plan.Images.Count; i++)
            {
                plan.Images[i] = plan.Images[i] with { Clip = contentClip };
            }

            for (var i = firstFill; i < plan.Fills.Count; i++)
            {
                plan.Fills[i] = plan.Fills[i] with { Clip = contentClip };
            }

            for (var i = firstCodeSymbolText; i < plan.Texts.Count; i++)
            {
                plan.Texts[i] = plan.Texts[i] with { Clip = contentClip };
            }
        }

        OrderedMerge.VisitByOrder(
            content.Tables,
            static table => table.ZOrder,
            content.Boxes,
            static box => box.ZOrder,
            table => EmitTable(context, table, element, left, contentTop, delta, artifact),
            box => EmitBox(context, box, element, left, contentTop, delta, artifact));

        if (radius > 0)
        {
            PageDrawTransformer.ApplyRoundedClip(plan, contentClip, radius, contentMark);
        }
    }

    private static bool EmitLine(
        PageRenderContext context,
        in LaidOutLine line,
        double contentWidth,
        double opacity,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? artifact)
    {
        var mark = context.BeginStack(line.ZOrder);
        var overflows = context.Text.EmitLines(
            context, [line],
            left, contentTop, delta,
            opacity, element, resolveStructure: artifact is null,
            overflowThreshold: contentWidth,
            artifact: artifact);
        context.EndStack(mark);
        return overflows;
    }

    private static bool EmitImage(
        PageRenderContext context,
        in LaidOutImage image,
        double boundsLeft,
        double boundsRight,
        double opacity,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? artifact)
    {
        var mark = context.BeginStack(image.ZOrder);
        context.Images.EmitImage(context, image, left, contentTop, delta, opacity, element, artifact);
        context.EndStack(mark);
        return image.X < boundsLeft - 0.01 || image.X + image.Width > boundsRight + 0.01;
    }

    private static bool EmitCodeSymbol(
        PageRenderContext context,
        in LaidOutCodeSymbol codeSymbol,
        double boundsLeft,
        double boundsRight,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? artifact)
    {
        var mark = context.BeginStack(codeSymbol.ZOrder);
        context.CodeSymbols.EmitCodeSymbolModules(
            context, codeSymbol.Source, codeSymbol.Modules, codeSymbol.Foreground,
            left + codeSymbol.X,
            BottomUpSpace.FromTop(contentTop, codeSymbol.Y + delta),
            codeSymbol.Caption,
            artifact);
        context.EndStack(mark);
        return codeSymbol.X < boundsLeft - 0.01 || codeSymbol.X + codeSymbol.Width > boundsRight + 0.01;
    }

    private static void EmitTable(
        PageRenderContext context,
        in LaidOutTablePlacement table,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? artifact)
    {
        var mark = context.BeginStack(table.ZOrder);
        context.Tables.EmitNestedTable(context, table, element, left, contentTop, delta, artifact);
        context.EndStack(mark);
    }

    private static void EmitBox(
        PageRenderContext context,
        LaidOutBox box,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? artifact)
    {
        var mark = context.BeginStack(box.ZOrder);
        context.Boxes.EmitBox(
            context, box, BoxContentOrigin.Box, element, left, contentTop, delta, artifact);
        context.EndStack(mark);
    }
}
