using System;

namespace Radzen.Documents.Pdf.Content;

internal static class WatermarkGeometry
{
    public const string EncodingContext = "Watermark text";

    private const double BaselineFactor = 0.35;

    public static double Baseline(double size) => -size * BaselineFactor;

    public static double Centered(double extent) => -extent / 2;

    public static void WriteRotation(ContentWriter writer, double rotation, double centerX, double centerY)
    {
        var radians = rotation * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(sin);
        writer.WriteRaw(" ");
        writer.WriteNumber(sin == 0 ? 0 : -sin);
        writer.WriteRaw(" ");
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(centerX);
        writer.WriteRaw(" ");
        writer.WriteNumber(centerY);
        writer.WriteRaw(" cm\n");
    }
}
