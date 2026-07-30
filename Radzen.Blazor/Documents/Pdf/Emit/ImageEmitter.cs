using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class ImageEmitter(ImageStore imageStore, StructureTreeBuilder structureTree)
{
    public void EmitImage(EmitContext context, PositionedImage positioned, double left, double top)
    {
        var plan = context.Plan;
        var paint = positioned.Paint;
        var xobject = imageStore.DecodeApplied(positioned.Source, paint);
        plan.Images.Add(new ImageDraw
        {
            Sequence = plan.NextSequence(),
            X = left + positioned.XOffset,
            Y = top - positioned.Y - positioned.Height,
            Width = positioned.Width,
            Height = positioned.Height,
            Image = xobject,
            Element = structureTree.ElementOf(positioned.Source),
            ExtGState = paint.Opacity < 1
                ? plan.RegisterExtGState(paint.Opacity, paint.Opacity)
                : null,
        });
        plan.UsedImages.Add(xobject);
    }
}
