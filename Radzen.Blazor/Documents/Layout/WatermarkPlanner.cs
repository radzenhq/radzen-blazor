using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal static class WatermarkPlanner
{
    public static LaidOutWatermark? Plan(
        Watermark? watermark,
        PageSize size,
        FontCollection fonts,
        LayoutCaptureContext capture)
        => Plan(watermark, size, fonts, knownFonts: null, canEmbed: true, capture);

    public static LaidOutWatermark? PlanBase14(
        Watermark? watermark,
        PageSize size,
        FontCollectionSnapshot? knownFonts,
        LayoutCaptureContext capture)
        => Plan(watermark, size, new FontCollection(), knownFonts, canEmbed: false, capture);

    private static LaidOutWatermark? Plan(
        Watermark? watermark,
        PageSize size,
        FontCollection fonts,
        FontCollectionSnapshot? knownFonts,
        bool canEmbed,
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
            Artifact = SemanticArtifactKind.Watermark,
            Image = watermark.Image is { } image ? PlanImage(image, width, capture) : null,
            Text = string.IsNullOrEmpty(watermark.Text)
                ? null
                : PlanText(watermark, watermark.Text, fonts, knownFonts, canEmbed),
        };
    }

    private static LaidOutWatermarkImage PlanImage(
        Image image,
        double availableWidth,
        LayoutCaptureContext capture)
    {
        var (width, height) = capture.Probes.Measure(image, availableWidth);
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

    private static LaidOutWatermarkText PlanText(
        Watermark watermark,
        string text,
        FontCollection fonts,
        FontCollectionSnapshot? knownFonts,
        bool canEmbed)
    {
        var font = watermark.Font;
        var family = font.EffectiveFamily;
        if (!canEmbed && !string.IsNullOrEmpty(family) && knownFonts?.HasFamily(family) == true)
        {
            throw new NotSupportedException(
                $"Font family '{family}' is registered as an embeddable font file, but text added to a loaded or already-built page cannot embed one; use a base-14 family (Helvetica, Courier, Times, Symbol, ZapfDingbats) here.");
        }

        if (!fonts.TryResolvePrimary(font, out _)
            && !string.IsNullOrEmpty(family)
            && BuiltInFontMetrics.Resolve(font) is null)
        {
            throw new NotSupportedException(
                canEmbed
                    ? $"No font is registered for family '{family}'; register it with Document.Fonts or use a family supplied by the built-in metrics."
                    : $"Font family '{family}' is not one of the base-14 families (Helvetica, Courier, Times, Symbol, ZapfDingbats), and text added to a loaded or already-built page cannot embed a font file; use a base-14 family here.");
        }

        var glyphRun = fonts.CaptureGlyphRun(text, font);

        return new LaidOutWatermarkText
        {
            Text = text,
            Font = GeometryCapture.Font(font),
            X = WatermarkGeometry.Centered(glyphRun.Advance),
            Baseline = WatermarkGeometry.BaselineBelowCenter(font.EffectiveSize.Point),
            GlyphRun = glyphRun,
            AlphaOverride = WatermarkGeometry.AlphaOverride(watermark.Opacity, font.EffectiveColor.A),
        };
    }
}
