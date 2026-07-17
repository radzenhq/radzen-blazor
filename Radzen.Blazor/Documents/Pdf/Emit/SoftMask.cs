using System;
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf.Objects;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf.Emit;

// Soft-mask subtype: /Luminosity derives the mask from the group's colour, /Alpha from its
// shape alpha (ISO 32000-1 11.6.5.2).
internal enum SoftMaskType
{
    Alpha,
    Luminosity,
}

// A soft mask installed through an ExtGState /SMask entry: the transparency group whose
// luminosity or alpha forms the mask, and an optional backdrop colour (/BC).
internal sealed class GeneratedSoftMask
{
    public required SoftMaskType Type { get; init; }

    public required GeneratedTransparencyGroup Group { get; init; }

    // Backdrop colour components in the group's colour space; null omits /BC.
    public double[]? Backdrop { get; init; }

    // Content identity for deduplication: soft masks with an equal, non-null key produce the
    // same SMask dictionary, so they can share one ExtGState and transparency group. Null opts
    // out (the mask is always registered fresh).
    public string? ContentKey { get; init; }
}

internal static class SoftMask
{
    // Builds the /SMask dictionary << /Type /Mask /S <type> /G <group ref> [/BC ...] >>. The
    // group form XObject is materialized into the writer so /G is an indirect reference.
    public static DictionaryObject BuildDictionary(DocumentWriter writer, GeneratedSoftMask mask)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("Mask"),
            ["S"] = new NameObject(mask.Type == SoftMaskType.Luminosity ? "Luminosity" : "Alpha"),
            ["G"] = writer.Add(TransparencyGroup.BuildForm(writer, mask.Group)),
        };

        if (mask.Backdrop is { } backdrop)
        {
            var bc = new ArrayObject();
            foreach (var component in backdrop)
            {
                bc.Add(new NumberObject(component));
            }

            dictionary["BC"] = bc;
        }

        return dictionary;
    }

    // Plans a box's drop shadow: rasterizes and blurs the rounded-rectangle coverage, wraps it
    // in a DeviceGray luminosity-mask group, registers the SMask ExtGState, and adds the
    // shadow-colour fill (in page space, offset and under the box). No-ops for an empty box.
    public static void EmitBoxShadow(PagePlan plan, PdfRect bounds, double cornerRadius, BoxShadow shadow)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var spread = shadow.Spread.Point;
        var shapeWidth = bounds.Width + (2 * spread);
        var shapeHeight = bounds.Height + (2 * spread);
        if (shapeWidth <= 0 || shapeHeight <= 0)
        {
            return;
        }

        var shapeRadius = Math.Max(0, cornerRadius + spread);
        var blur = Math.Max(0, shadow.BlurRadius.Point);
        var mask = plan.RenderShadowMask(shapeWidth, shapeHeight, shapeRadius, blur);

        var margin = mask.MarginPoints;
        var rectWidth = shapeWidth + (2 * margin);
        var rectHeight = shapeHeight + (2 * margin);

        // Shape is centred on the box; the image adds `margin` of blur padding on every edge.
        var left = bounds.Left - spread - margin + shadow.OffsetX.Point;
        var bottom = bounds.Bottom - spread - margin - shadow.OffsetY.Point;

        var image = TransparencyGroup.GrayImage(mask.Pixels, mask.Width, mask.Height);

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

    // FNV-1a over the blurred raster pins pixel identity; placement and alpha complete it.
    private static string ShadowKey(ShadowMask mask, double left, double bottom, double rectWidth, double rectHeight, double alpha)
    {
        var hash = 1469598103934665603UL;
        foreach (var pixel in mask.Pixels)
        {
            hash = (hash ^ pixel) * 1099511628211UL;
        }

        var culture = CultureInfo.InvariantCulture;
        return string.Create(culture, $"{mask.Width}x{mask.Height}:{hash:x}:{left}:{bottom}:{rectWidth}:{rectHeight}:{alpha}");
    }
}
