using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class WatermarkPlanner
{
    public static LaidOutWatermark? Plan(
        Watermark? watermark,
        PageSize size,
        FontCollection fonts,
        LayoutCaptureContext capture)
    {
        if (watermark is null)
        {
            return null;
        }


        var width = size.Width.Point;
        return new LaidOutWatermark
        {
            Id = capture.Node(),
            CenterX = width / 2,
            CenterY = size.Height.Point / 2,
            Rotation = watermark.Rotation,
            Opacity = watermark.Opacity,
            Image = watermark.Image is { } image ? PlanImage(image, width, capture) : null,
            Text = string.IsNullOrEmpty(watermark.Text) ? null : PlanText(watermark, watermark.Text, fonts),
        };
    }

    private static LaidOutWatermarkImage PlanImage(
        Image image,
        double availableWidth,
        LayoutCaptureContext capture)
    {
        var (width, height) = ImageProbe.Measure(image, availableWidth);
        return new LaidOutWatermarkImage
        {
            Source = capture.Source(image),
            Paint = GeometryCapture.Image(image, capture),
            X = WatermarkGeometry.Centered(width),
            Y = WatermarkGeometry.Centered(height),
            Width = width,
            Height = height,
            Alpha = image.Opacity,
        };
    }

    private static LaidOutWatermarkText PlanText(Watermark watermark, string text, FontCollection fonts)
    {
        var font = watermark.Font;
        var isSfnt = fonts.TryResolvePrimary(font, out _);
        if (!isSfnt
            && !string.IsNullOrEmpty(font.EffectiveFamily)
            && BuiltInFontMetrics.Resolve(font) is null)
        {
            throw new NotSupportedException(
                $"No font is registered for family '{font.EffectiveFamily}'; register it with Document.Fonts "
                + "or use a family supplied by the built-in metrics.");
        }

        var glyphRun = fonts.CaptureGlyphRun(text, font, enableBuiltInKerning: isSfnt);

        return new LaidOutWatermarkText
        {
            Text = text,
            Font = GeometryCapture.Font(font),
            Size = font.EffectiveSize.Point,
            Color = font.EffectiveColor,
            X = WatermarkGeometry.Centered(glyphRun.Advance),
            Baseline = WatermarkGeometry.Baseline(font.EffectiveSize.Point),
            GlyphRun = glyphRun,
            AlphaOverride = WatermarkGeometry.AlphaOverride(watermark.Opacity, font.EffectiveColor.A),
        };
    }
}
