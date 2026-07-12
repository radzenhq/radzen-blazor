using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

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
    public static void EmitBoxShadow(PagePlan plan, Rect bounds, double cornerRadius, BoxShadow shadow)
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
        var mask = GaussianBlur.Render(shapeWidth, shapeHeight, shapeRadius, blur);

        var margin = mask.MarginPoints;
        var rectWidth = shapeWidth + (2 * margin);
        var rectHeight = shapeHeight + (2 * margin);

        // Shape is centred on the box; the image adds `margin` of blur padding on every edge.
        var left = bounds.X - spread - margin + shadow.OffsetX.Point;
        var bottom = bounds.Y - spread - margin - shadow.OffsetY.Point;

        var image = TransparencyGroup.GrayImage(mask.Pixels, mask.Width, mask.Height);

        using var content = new ContentWriter();
        content.WriteRaw("q\n");
        content.WriteNumber(rectWidth);
        content.WriteRaw(" 0 0 ");
        content.WriteNumber(rectHeight);
        content.WriteRaw(" ");
        content.WriteNumber(left);
        content.WriteRaw(" ");
        content.WriteNumber(bottom);
        content.WriteRaw(" cm\n/Sm Do\nQ\n");

        var group = new GeneratedTransparencyGroup
        {
            Content = content.ToArray(),
            BBox = [left, bottom, left + rectWidth, bottom + rectHeight],
            ColorSpace = "DeviceGray",
            XObjects = [new KeyValuePair<string, StreamObject>("Sm", image)],
        };

        var alpha = shadow.Color.A / 255.0;
        var extGState = plan.RegisterSoftMaskExtGState(alpha, alpha, new GeneratedSoftMask
        {
            Type = SoftMaskType.Luminosity,
            Group = group,
        });

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
}
