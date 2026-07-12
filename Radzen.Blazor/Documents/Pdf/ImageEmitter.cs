namespace Radzen.Documents.Pdf;

// Measures and paints block-level images: measurement feeds the paginator, emission
// places the decoded XObject on the page (top-left origin flipped to PDF's bottom-left)
// and tags it with its Figure structure element.
internal sealed class ImageEmitter(ImageStore imageStore, StructureTreeBuilder structureTree)
{
    public (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, imageStore.Decode(image).Image, availableWidth);

    public void EmitImage(EmitContext context, PositionedImage positioned, double left, double top)
    {
        var plan = context.Plan;
        var xobject = imageStore.Decode(positioned.Source);
        plan.Images.Add(new ImageDraw
        {
            X = left + positioned.XOffset,
            Y = top - positioned.Y - positioned.Height,
            Width = positioned.Width,
            Height = positioned.Height,
            Image = xobject,
            Element = structureTree.ElementOf(positioned.Source),
            ExtGState = positioned.Source.Opacity < 1
                ? plan.RegisterExtGState(positioned.Source.Opacity, positioned.Source.Opacity)
                : null,
        });
        plan.UsedImages.Add(xobject);
    }
}
