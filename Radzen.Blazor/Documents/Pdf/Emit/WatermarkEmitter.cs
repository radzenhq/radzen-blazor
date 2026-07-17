using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf.Emit;

// Plans a section watermark onto a page: shapes the text during page planning (so
// glyph usage lands in the document-wide subsets before compaction), decodes the
// image, and records a WatermarkDraw the generator serializes after all content.
internal sealed class WatermarkEmitter(FontCollection fonts, GeneratorFontResolver fontResolver, ImageStore imageStore)
{
    private readonly SfntRunBuilder runBuilder = new(fonts, fontResolver);

    public void Plan(PagePlan plan, Watermark watermark)
    {
        var draw = new WatermarkDraw
        {
            CenterX = plan.Size.Width.Point / 2,
            CenterY = plan.Size.Height.Point / 2,
            Rotation = watermark.Rotation,
            ExtGState = watermark.Opacity < 1
                ? plan.RegisterExtGState(watermark.Opacity, watermark.Opacity)
                : null,
        };

        if (watermark.Image is { } image)
        {
            var generated = imageStore.Decode(image);
            var (width, height) = ImageDecoder.Measure(image, generated.Image, plan.Size.Width.Point);
            draw.Image = new ImageDraw
            {
                X = WatermarkGeometry.Centered(width),
                Y = WatermarkGeometry.Centered(height),
                Width = width,
                Height = height,
                Image = generated,
            };
            plan.UsedImages.Add(generated);
        }

        if (!string.IsNullOrEmpty(watermark.Text))
        {
            PlanText(plan, draw, watermark.Text, watermark.Font);
        }

        plan.Watermark = draw;
    }

    // Segments are laid out in the rotated coordinate system, centered on its origin:
    // the run starts at -width/2 with the baseline dropped so the glyph body straddles
    // the page center.
    private void PlanText(PagePlan plan, WatermarkDraw draw, string text, Font font)
    {
        var size = font.Size;
        var baseline = WatermarkGeometry.Baseline(size);
        if (fonts.TryResolvePrimary(font, out _))
        {
            // The whole text is measured as one run, so its space-straddling pairs are kerned
            // in the width the mark is centred from and must be kerned in the drawn run too.
            var x = WatermarkGeometry.Centered(fonts.MeasureText(text, font));
            foreach (var glyphRun in runBuilder.Build(text, font, size, kernAcrossSpaces: true))
            {
                plan.UsedFonts.Add(glyphRun.Font);
                draw.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = baseline,
                    Size = size,
                    Color = font.Color,
                    Font = glyphRun.Font,
                    Bytes = glyphRun.Bytes,
                    Kerns = glyphRun.Kerns,
                });
                x += glyphRun.Advance;
            }
        }
        else
        {
            // Base-14 watermarks are WinAnsi-only; fail loud rather than silently dropping
            // unrepresentable codepoints (which would blank or mangle the watermark). Encoded
            // before the plan is touched so a rejected watermark leaves no font registered.
            var bytes = WinAnsiText.Encode(text, OnUnencodable.Throw, WatermarkGeometry.EncodingContext);
            var metrics = Fonts.Base14Metrics.Resolve(font) ?? Fonts.Base14Metrics.Resolve(new Font())!;
            var generated = fontResolver.ResolveBase14(font);
            plan.UsedFonts.Add(generated);
            draw.Texts.Add(new TextDraw
            {
                X = WatermarkGeometry.Centered(metrics.MeasureString(text, size)),
                Baseline = baseline,
                Size = size,
                Color = font.Color,
                Font = generated,
                Bytes = bytes,
            });
        }
    }
}
