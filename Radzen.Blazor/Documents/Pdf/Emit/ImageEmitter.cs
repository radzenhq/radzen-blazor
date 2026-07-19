using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class ImageEmitter(ImageStore imageStore, StructureTreeBuilder structureTree)
{
    private readonly AppliedImageCache<GeneratedImage> prepared = new();

    private int preparedCount;

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
            StencilColor = positioned.Source.Stencil ? positioned.Source.StencilColor : null,
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

        return prepared.Get(source, () =>
        {
            var applied = ImageDecoder.ApplyOptions(generated.Image, source);
            return ReferenceEquals(applied, generated.Image)
                ? generated
                : new GeneratedImage
                {
                    Key = "Imo" + preparedCount++.ToString(CultureInfo.InvariantCulture),
                    Image = applied,
                };
        });
    }
}
