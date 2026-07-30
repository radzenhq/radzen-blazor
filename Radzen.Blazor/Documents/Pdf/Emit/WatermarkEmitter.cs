using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class WatermarkEmitter(
    GeneratorFontResolver fontResolver,
    ImageStore imageStore,
    bool allowUnsupportedCharacters)
{
    private readonly GlyphSpanEmitter spans = new(fontResolver, allowUnsupportedCharacters);

    public void Plan(PagePlan plan, LaidOutWatermark watermark)
    {
        var draw = new WatermarkDraw
        {
            CenterX = watermark.CenterX,
            CenterY = PageSpace.FromTop(plan.Size.Height.Point, watermark.CenterY),
            Rotation = watermark.Rotation,
            ExtGState = watermark.Opacity < 1
                ? plan.RegisterExtGState(watermark.Opacity, watermark.Opacity)
                : null,
        };

        if (watermark.Image is { } image)
        {
            var generated = imageStore.DecodeWatermark(image.Source, image.Paint);

            draw.Image = new ImageDraw
            {
                X = image.X,
                Y = image.Y,
                Width = image.Width,
                Height = image.Height,
                Image = generated,
                ExtGState = image.Alpha < 1
                    ? plan.RegisterExtGState(watermark.Opacity * image.Alpha, watermark.Opacity * image.Alpha)
                    : null,
            };
            plan.UsedImages.Add(generated);
        }

        if (watermark.Text is { } text)
        {
            PlanText(plan, draw, text);
        }

        plan.Watermark = draw;
    }

    private void PlanText(PagePlan plan, WatermarkDraw draw, in LaidOutWatermarkText text)
    {
        var size = text.Size;
        var baseline = -text.Baseline;
        var extGState = text.AlphaOverride is { } alpha
            ? plan.RegisterExtGState(alpha, alpha)
            : null;
        foreach (var span in text.GlyphRun.Spans)
        {
            var x = text.X + span.XOffset;
            var emitted = spans.Emit(span, text.Font.Size);
            plan.UsedFonts.Add(emitted.Font);
            draw.Texts.Add(new TextDraw
            {
                X = x,
                Baseline = baseline,
                Size = size,
                Color = text.Color,
                Font = emitted.Font,
                Bytes = emitted.Bytes,
                Kerns = emitted.Kerns,
                ExtGState = extGState,
            });
        }
    }
}
