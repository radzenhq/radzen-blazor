namespace Radzen.Documents.Pdf.Content;

internal static class WatermarkGeometry
{
    public const string EncodingContext = "Watermark text";

    private const double BaselineFactor = 0.35;

    public static double Baseline(double size) => -size * BaselineFactor;

    public static double Centered(double extent) => -extent / 2;

    public static void WriteRotation(ContentWriter writer, double rotation, double centerX, double centerY)
        => ContentEmitter.WriteTransform(
            writer, Matrix.Rotate(rotation) * Matrix.Translate(centerX, centerY));
}
