using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emission;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal static class SoftMask
{
    public static void EmitBoxShadow(
        PagePlan plan,
        PdfRect bounds,
        double cornerRadius,
        in BoxShadowPaint shadow,
        SemanticArtifactKind artifact)
    {
        if (BoxShadowGeometry.Shape(
            bounds.Width,
            bounds.Height,
            cornerRadius,
            shadow.Spread,
            shadow.BlurRadius) is not { } shape)
        {
            return;
        }

        var mask = plan.RenderShadowMask(shape.Width, shape.Height, shape.Radius, shape.Blur);

        var placement = BoxShadowGeometry.Placement(
            shape,
            bounds.Left,
            bounds.Bottom,
            shadow.Spread,
            mask.MarginPoints,
            shadow.OffsetX,
            shadow.OffsetY);

        var left = placement.Left;
        var bottom = placement.Bottom;
        var rectWidth = placement.Width;
        var rectHeight = placement.Height;

        var image = new DecodedImage(mask.Pixels, mask.Width, mask.Height, 8, ImageColorSpace.DeviceGray);

        using var content = new ContentWriter();
        ContentEmitter.WriteImagePlacement(content, "Sm", left, bottom, rectWidth, rectHeight);

        // ISO 32000-1 11.6.5.2: soft-mask subtype (/Luminosity from group color, /Alpha from shape alpha).
        var group = new EmissionTransparencyGroup(
            content.ToArray(),
            [left, bottom, left + rectWidth, bottom + rectHeight],
            "DeviceGray",
            isolated: null,
            knockout: null,
            [new KeyValuePair<string, EmissionImagePayload>("Sm", EmissionImagePayload.Capture(image))]);

        var alpha = shadow.Color.A / 255.0;
        var softMask = new EmissionSoftMask(EmissionSoftMaskType.Luminosity, group, backdrop: null);
        var extGState = plan.RegisterSoftMaskExtGState(
            alpha,
            alpha,
            softMask,
            ShadowKey(mask, left, bottom, rectWidth, rectHeight, alpha));

        plan.Fills.Add(new FillDraw
        {
            X = left,
            Y = bottom,
            Width = rectWidth,
            Height = rectHeight,
            Color = shadow.Color,
            ExtGState = extGState,
            Artifact = artifact,
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
                shadow.Spread.Point),
            SemanticArtifactKind.LayoutDecoration);

    private static string ShadowKey(ShadowMask mask, double left, double bottom, double rectWidth, double rectHeight, double alpha)
    {
        var hash = Fnv1a64.Hash(mask.Pixels);
        var culture = CultureInfo.InvariantCulture;
        return string.Create(culture, $"{mask.Width}x{mask.Height}:{hash:x}:{left}:{bottom}:{rectWidth}:{rectHeight}:{alpha}");
    }
}
