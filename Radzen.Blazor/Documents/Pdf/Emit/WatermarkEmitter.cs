using Radzen.Documents.Pdf.Content;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class WatermarkEmitter(FontCollection fonts, GeneratorFontResolver fontResolver, ImageStore imageStore)
{
    private readonly SfntRunBuilder runBuilder = new(fonts, fontResolver);
    private readonly AppliedImageCache<GeneratedImage> appliedImages = new();

    public void Plan(PagePlan plan, Watermark watermark)
    {
        watermark.Validate();
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
            if (image.HasXObjectOptions)
            {
                generated = ApplyOptions(watermark, image, generated);
            }

            var imagePlan = WatermarkImagePlan.Create(image, generated.Image, plan.Size.Width.Point);

            draw.Image = new ImageDraw
            {
                X = imagePlan.X,
                Y = imagePlan.Y,
                Width = imagePlan.Width,
                Height = imagePlan.Height,
                Image = generated,
                ExtGState = imagePlan.Alpha < 1
                    ? plan.RegisterExtGState(watermark.Opacity * imagePlan.Alpha, watermark.Opacity * imagePlan.Alpha)
                    : null,
                StencilColor = imagePlan.StencilColor,
            };
            plan.UsedImages.Add(generated);
        }

        if (!string.IsNullOrEmpty(watermark.Text))
        {
            PlanText(plan, draw, watermark.Text, watermark.Font);
        }

        plan.Watermark = draw;
    }

    private GeneratedImage ApplyOptions(Watermark watermark, Image image, GeneratedImage baseImage)
        => appliedImages.Get(image, () => new GeneratedImage
        {
            Key = baseImage.Key + "w",
            Image = watermark.DecodeImage(image),
        });

    private void PlanText(PagePlan plan, WatermarkDraw draw, string text, Font font)
    {
        var size = font.Size;
        var baseline = WatermarkGeometry.Baseline(size);
        if (fonts.TryResolvePrimary(font, out _))
        {
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
            var text14 = WatermarkTextPlan.Base14(text, font);
            var generated = fontResolver.ResolveBase14(font);
            plan.UsedFonts.Add(generated);
            draw.Texts.Add(new TextDraw
            {
                X = text14.X,
                Baseline = text14.Baseline,
                Size = size,
                Color = font.Color,
                Font = generated,
                Bytes = text14.Bytes,
            });
        }
    }
}
