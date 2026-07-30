using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Pdf.Render;

internal sealed class ImagePlanner(ImageStore imageStore, StructureTreeBuilder structureTree)
{
    public void EmitImage(EmitContext context, LaidOutImage positioned, double left, double top)
    {
        var plan = context.Plan;
        var paint = positioned.Paint;
        var xobject = imageStore.DecodeApplied(positioned.Source, paint);
        var element = structureTree.ElementOf(positioned.Source);
        plan.Images.Add(new ImageDraw
        {
            Sequence = plan.NextSequence(),
            X = left + positioned.X,
            Y = PageSpace.Bottom(top, positioned.Y, positioned.Height),
            Width = positioned.Width,
            Height = positioned.Height,
            Image = xobject,
            Element = element,
            Artifact = element is null ? structureTree.ArtifactOf(positioned.Source) : null,
            ExtGState = paint.Opacity < 1
                ? plan.RegisterExtGState(paint.Opacity, paint.Opacity)
                : null,
        });
        plan.UsedImages.Add(xobject);
    }
}
