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
            PlanText(plan, draw, watermark.Text, watermark);
        }

        plan.Watermark = draw;
    }

    private GeneratedImage ApplyOptions(Watermark watermark, Image image, GeneratedImage baseImage)
        => appliedImages.Get(image, () => new GeneratedImage
        {
            Key = baseImage.Key + "w",
            Image = watermark.DecodeImage(image),
        });

    private void PlanText(PagePlan plan, WatermarkDraw draw, string text, Watermark watermark)
    {
        var font = watermark.Font;
        var size = font.Size;
        var textPlan = WatermarkTextPlanning.Plan(text, watermark, fonts);
        var extGState = textPlan.AlphaOverride is { } alpha
            ? plan.RegisterExtGState(alpha, alpha)
            : null;
        if (textPlan.IsSfnt)
        {
            var x = textPlan.X;
            foreach (var glyphRun in runBuilder.Build(text, font, size))
            {
                plan.UsedFonts.Add(glyphRun.Font);
                draw.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = textPlan.Baseline,
                    Size = size,
                    Color = font.Color,
                    Font = glyphRun.Font,
                    Bytes = glyphRun.Bytes,
                    Kerns = glyphRun.Kerns,
                    ExtGState = extGState,
                });
                x += glyphRun.Advance;
            }
        }
        else
        {
            var generated = fontResolver.ResolveBase14(font);
            plan.UsedFonts.Add(generated);
            draw.Texts.Add(new TextDraw
            {
                X = textPlan.X,
                Baseline = textPlan.Baseline,
                Size = size,
                Color = font.Color,
                Font = generated,
                Bytes = textPlan.Base14Bytes!,
                ExtGState = extGState,
            });
        }
    }
}
