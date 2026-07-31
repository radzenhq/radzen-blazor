using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal static class BoxContentPlanner
{
    public static void EmitBoxContent(
        EmitContext context,
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
        var images = content.Images;
        var codeSymbols = content.CodeSymbols;
        var tables = content.Tables;
        var boxes = content.Boxes;
        var plan = context.Plan;

        var contentMark = radius > 0 ? plan.Mark() : default;

        var firstText = plan.Texts.Count;
        var overflows = context.Text.EmitLines(
            context, content.Lines,
            left, contentTop, delta,
            opacity, element, resolveStructure: artifact is null,
            overflowThreshold: contentWidth,
            artifact: artifact);

        var cellClip = clip;
        if (overflows)
        {
            for (var t = firstText; t < plan.Texts.Count; t++)
            {
                plan.Texts[t] = plan.Texts[t] with { Clip = cellClip };
            }
        }

        var contentOverflows = false;
        var firstImage = plan.Images.Count;
        var firstFill = plan.Fills.Count;
        var firstCodeSymbolText = plan.Texts.Count;

        foreach (var image in images)
        {
            contentOverflows |= image.X < boundsLeft - 0.01 || image.X + image.Width > boundsRight + 0.01;
            context.Images.EmitImage(context, image, left, contentTop, delta, opacity, element, artifact);
        }

        foreach (var codeSymbol in codeSymbols)
        {
            contentOverflows |= codeSymbol.X < boundsLeft - 0.01 || codeSymbol.X + codeSymbol.Width > boundsRight + 0.01;
            context.CodeSymbols.EmitCodeSymbolModules(
                context, codeSymbol.Source, codeSymbol.Modules,
                left + codeSymbol.X,
                BottomUpSpace.FromTop(contentTop, codeSymbol.Y + delta),
                codeSymbol.Caption,
                artifact);
        }

        if (contentOverflows)
        {
            for (var im = firstImage; im < plan.Images.Count; im++)
            {
                plan.Images[im] = plan.Images[im] with { Clip = cellClip };
            }

            for (var f = firstFill; f < plan.Fills.Count; f++)
            {
                plan.Fills[f] = plan.Fills[f] with { Clip = cellClip };
            }

            for (var t = firstCodeSymbolText; t < plan.Texts.Count; t++)
            {
                plan.Texts[t] = plan.Texts[t] with { Clip = cellClip };
            }
        }

        OrderedMerge.VisitByOrder(
            tables,
            static table => table.ZOrder,
            boxes,
            static box => box.ZOrder,
            table => context.Tables.EmitNestedTable(context, table, element, left, contentTop, delta, artifact),
            box => context.Boxes.EmitBox(
                context, box, BoxContentOrigin.Box, element, left, contentTop, delta, artifact));

        if (radius > 0)
        {
            plan.ApplyRoundedClip(cellClip, radius, contentMark);
        }
    }
}
