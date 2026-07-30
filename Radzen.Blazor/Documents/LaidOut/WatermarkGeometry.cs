using System;

namespace Radzen.Documents.LaidOut;

internal static class WatermarkGeometry
{
    private const double BaselineFactor = 0.35;

    public static double Baseline(double size) => size * BaselineFactor;

    public static double Centered(double extent) => -extent / 2;

    public static double? AlphaOverride(double opacity, byte alpha)
        => alpha == 255 ? null : Math.Clamp(opacity * alpha / 255.0, 0, 1);

    public static Matrix Rotation(double rotation, double centerX, double centerY)
        => Matrix.Rotate(rotation) * Matrix.Translate(centerX, centerY);
}
