using System;

namespace Radzen.Documents.Geometry;

internal readonly record struct BoxShadowShape(double Width, double Height, double Radius, double Blur);

internal static class BoxShadowGeometry
{
    public static BoxShadowShape? Shape(
        double width,
        double height,
        double cornerRadius,
        double spread,
        double blurRadius)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var shapeWidth = width + (2 * spread);
        var shapeHeight = height + (2 * spread);
        if (shapeWidth <= 0 || shapeHeight <= 0)
        {
            return null;
        }

        return new BoxShadowShape(
            shapeWidth,
            shapeHeight,
            Math.Max(0, cornerRadius + spread),
            Math.Max(0, blurRadius));
    }

    public static PageBox Placement(
        in BoxShadowShape shape,
        double left,
        double bottom,
        double spread,
        double margin,
        double offsetX,
        double offsetY)
        => new(
            left - spread - margin + offsetX,
            bottom - spread - margin - offsetY,
            shape.Width + (2 * margin),
            shape.Height + (2 * margin));
}
