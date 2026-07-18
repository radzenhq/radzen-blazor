using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class WatermarkEmitter(FontCollection fonts, GeneratorFontResolver fontResolver, ImageStore imageStore)
{
    private readonly SfntRunBuilder runBuilder = new(fonts, fontResolver);
    private readonly Dictionary<Image, GeneratedImage> appliedImages = [];

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

            var (width, height) = ImageDecoder.Measure(image, generated.Image, plan.Size.Width.Point);
            var imageAlpha = image.Opacity;
            if (image.Stencil && image.StencilColor.A != 255)
            {
                imageAlpha *= image.StencilColor.A / 255.0;
            }

            draw.Image = new ImageDraw
            {
                X = WatermarkGeometry.Centered(width),
                Y = WatermarkGeometry.Centered(height),
                Width = width,
                Height = height,
                Image = generated,
                ExtGState = imageAlpha < 1
                    ? plan.RegisterExtGState(watermark.Opacity * imageAlpha, watermark.Opacity * imageAlpha)
                    : null,
                StencilColor = image.Stencil ? image.StencilColor : null,
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
    {
        if (!appliedImages.TryGetValue(image, out var applied))
        {
            applied = new GeneratedImage { Key = baseImage.Key + "w", Image = watermark.DecodeImage(image) };
            appliedImages[image] = applied;
        }

        return applied;
    }

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
