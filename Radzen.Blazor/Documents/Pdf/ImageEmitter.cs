using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// Measures and paints block-level images: measurement feeds the paginator, emission
// places the decoded XObject on the page (top-left origin flipped to PDF's bottom-left)
// and tags it with its Figure structure element.
internal sealed class ImageEmitter(ImageStore imageStore, StructureTreeBuilder structureTree)
{
    // Per-Image XObject after opt-in options are applied, cached so a picture reused across
    // pages shares one XObject even when an option (e.g. a stencil mask) yields a fresh one.
    private readonly Dictionary<Image, GeneratedImage> prepared = [];

    public (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, imageStore.Decode(image).Image, availableWidth);

    public void EmitImage(EmitContext context, PositionedImage positioned, double left, double top)
    {
        var plan = context.Plan;
        var xobject = Prepare(positioned.Source);
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

    private GeneratedImage Prepare(Image source)
    {
        var generated = imageStore.Decode(source);
        if (!source.HasXObjectOptions)
        {
            return generated;
        }

        if (prepared.TryGetValue(source, out var cached))
        {
            return cached;
        }

        var applied = ImageDecoder.ApplyOptions(generated.Image, source);
        var result = ReferenceEquals(applied, generated.Image)
            ? generated
            : new GeneratedImage { Key = generated.Key, Image = applied };
        prepared[source] = result;
        return result;
    }
}
