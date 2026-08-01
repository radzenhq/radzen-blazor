using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class ImageRecorder(ImageRegistry imageRegistry, StructureTreeBuilder structureTree)
{
    public void EmitImage(
        PageRenderContext context,
        in LaidOutImage positioned,
        double left,
        double top,
        double delta,
        double opacity,
        StructureElement? inherited,
        SemanticArtifactKind? inheritedArtifact)
    {
        var plan = context.Plan;
        var paint = positioned.Paint;
        var xobject = imageRegistry.DecodeApplied(positioned.Source, paint);
        var element = inheritedArtifact is null
            ? structureTree.ElementOf(positioned.Source) ?? inherited
            : null;
        var alpha = paint.Opacity * opacity;
        plan.Images.Add(new ImageDraw
        {
            Sequence = plan.NextSequence(),
            X = left + positioned.X,
            Y = BottomUpSpace.Bottom(top, positioned.Y + delta, positioned.Height),
            Width = positioned.Width,
            Height = positioned.Height,
            Image = xobject,
            Element = element,
            Artifact = element is null
                ? inheritedArtifact ?? structureTree.ArtifactOf(positioned.Source)
                : null,
            ExtGState = alpha < 1 ? plan.Resources.RegisterExtGState(alpha, alpha) : null,
        });
        plan.Resources.UsedImages.Add(xobject);
    }
}
