using System.Collections.Immutable;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class CodeSymbolRecorder(StructureTreeBuilder structureTree)
{
    private static readonly Color ModuleBlack = Color.FromRgb(0, 0, 0);

    public void EmitCodeSymbol(PageRenderContext context, LaidOutCodeSymbol positioned, double left, double top)
        => EmitCodeSymbolModules(
            context, positioned.Source, positioned.Modules,
            left + positioned.X, BottomUpSpace.FromTop(top, positioned.Y), positioned.Caption);

    public void EmitCodeSymbolModules(
        PageRenderContext context,
        SourceId source,
        ImmutableArray<CodeSymbolModule> modules,
        double x,
        double topY,
        ImmutableArray<LaidOutCaptionLine>? caption,
        SemanticArtifactKind? artifact = null)
    {
        var plan = context.Plan;
        var element = artifact is null ? structureTree.ElementOf(source) : null;
        artifact ??= element is null ? structureTree.ArtifactOf(source) : null;
        foreach (var module in modules)
        {
            plan.Fills.Add(new FillDraw
            {
                Sequence = plan.NextSequence(),
                Element = element,
                Artifact = artifact,
                X = x + module.X,
                Y = BottomUpSpace.Bottom(topY, module.Y, module.Height),
                Width = module.Width,
                Height = module.Height,
                Color = ModuleBlack,
            });
        }

        if (caption is not { } lines)
        {
            return;
        }

        foreach (var line in lines)
        {
            context.Text.EmitLine(context, line.Line, x, topY - line.Y, element, artifact: artifact);
        }
    }
}
