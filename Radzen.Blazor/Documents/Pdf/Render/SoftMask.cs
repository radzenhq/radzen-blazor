using System;
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

// ISO 32000-1 11.6.5.2: soft-mask subtype (/Luminosity from group color, /Alpha from shape alpha).
internal enum SoftMaskType
{
    Alpha,
    Luminosity,
}

internal sealed class GeneratedSoftMask
{
    public required SoftMaskType Type { get; init; }

    public required GeneratedTransparencyGroup Group { get; init; }

    public double[]? Backdrop { get; init; }

    public string? ContentKey { get; init; }
}

internal static class SoftMask
{
    public static void EmitBoxShadow(PagePlan plan, PdfRect bounds, double cornerRadius, in BoxShadowPaint shadow)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var spread = shadow.Spread;
        var shapeWidth = bounds.Width + (2 * spread);
        var shapeHeight = bounds.Height + (2 * spread);
        if (shapeWidth <= 0 || shapeHeight <= 0)
        {
            return;
        }

        var shapeRadius = Math.Max(0, cornerRadius + spread);
        var blur = Math.Max(0, shadow.BlurRadius);
        var mask = plan.RenderShadowMask(shapeWidth, shapeHeight, shapeRadius, blur);

        var margin = mask.MarginPoints;
        var rectWidth = shapeWidth + (2 * margin);
        var rectHeight = shapeHeight + (2 * margin);

        var left = bounds.Left - spread - margin + shadow.OffsetX;
        var bottom = bounds.Bottom - spread - margin - shadow.OffsetY;

        var image = ImageXObjectShell.FlateImage(
            mask.Pixels,
            mask.Width,
            mask.Height,
            8,
            new NameObject("DeviceGray"));

        using var content = new ContentWriter();
        ContentEmitter.WriteImagePlacement(content, "Sm", left, bottom, rectWidth, rectHeight);

        var group = new GeneratedTransparencyGroup
        {
            Content = content.ToArray(),
            BBox = [left, bottom, left + rectWidth, bottom + rectHeight],
            ColorSpace = "DeviceGray",
            XObjects = [new KeyValuePair<string, StreamObject>("Sm", image)],
        };

        var alpha = shadow.Color.A / 255.0;
        var softMask = new GeneratedSoftMask
        {
            Type = SoftMaskType.Luminosity,
            Group = group,
            ContentKey = ShadowKey(mask, left, bottom, rectWidth, rectHeight, alpha),
        };
        var extGState = plan.RegisterSoftMaskExtGState(alpha, alpha, softMask);

        plan.Fills.Add(new FillDraw
        {
            X = left,
            Y = bottom,
            Width = rectWidth,
            Height = rectHeight,
            Color = shadow.Color,
            ExtGState = extGState,
        });
    }

    public static void EmitBoxShadow(PagePlan plan, PdfRect bounds, double cornerRadius, BoxShadow shadow)
        => EmitBoxShadow(
            plan,
            bounds,
            cornerRadius,
            new BoxShadowPaint(
                shadow.Color,
                shadow.BlurRadius.Point,
                shadow.OffsetX.Point,
                shadow.OffsetY.Point,
                shadow.Spread.Point));

    private static string ShadowKey(ShadowMask mask, double left, double bottom, double rectWidth, double rectHeight, double alpha)
    {
        var hash = Fnv1a64.Hash(mask.Pixels);
        var culture = CultureInfo.InvariantCulture;
        return string.Create(culture, $"{mask.Width}x{mask.Height}:{hash:x}:{left}:{bottom}:{rectWidth}:{rectHeight}:{alpha}");
    }
}
