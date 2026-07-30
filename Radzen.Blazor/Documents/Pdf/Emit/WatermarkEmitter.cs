using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class WatermarkEmitter(
    GeneratorFontResolver fontResolver,
    ImageStore imageStore,
    bool allowUnsupportedCharacters)
{
    private readonly SfntRunBuilder runBuilder = new(fontResolver);
    private readonly Base14GlyphEncoder base14Encoder = new(allowUnsupportedCharacters);

    public void Plan(PagePlan plan, PositionedWatermark watermark)
    {
        var draw = new WatermarkDraw
        {
            CenterX = watermark.CenterX,
            CenterY = plan.Size.Height.Point - watermark.CenterY,
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

    private void PlanText(PagePlan plan, WatermarkDraw draw, in PositionedWatermarkText text)
    {
        var font = PdfModelAdapter.Materialize(text.Font);
        var size = text.Size;
        var baseline = -text.Baseline;
        var extGState = text.AlphaOverride is { } alpha
            ? plan.RegisterExtGState(alpha, alpha)
            : null;
        foreach (var span in text.GlyphRun.Spans)
        {
            var x = text.X + span.XOffset;
            if (span.IsSfnt)
            {
                var glyphRun = runBuilder.Build(span);
                plan.UsedFonts.Add(glyphRun.Font);
                draw.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = baseline,
                    Size = size,
                    Color = text.Color,
                    Font = glyphRun.Font,
                    Bytes = glyphRun.Bytes,
                    Kerns = glyphRun.Kerns,
                    ExtGState = extGState,
                });
            }
            else
            {
                var glyphs = span.BuiltInGlyphs;
                var bytes = base14Encoder.Encode(glyphs, font);
                var kerns = glyphs.Length > 1 ? new double[glyphs.Length - 1] : [];
                for (var i = 0; i < glyphs.Length; i++)
                {
                    if (i < kerns.Length)
                    {
                        kerns[i] = glyphs[i].TextAdjustment;
                    }
                }

                var generated = fontResolver.ResolveBase14(font);
                plan.UsedFonts.Add(generated);
                draw.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = baseline,
                    Size = size,
                    Color = text.Color,
                    Font = generated,
                    Bytes = bytes,
                    Kerns = SfntRunBuilder.HasNonZero(kerns) ? kerns : null,
                    ExtGState = extGState,
                });
            }
        }
    }
}
