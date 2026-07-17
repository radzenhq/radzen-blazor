using System;

namespace Radzen.Documents.Pdf.Content;

// The watermark placement geometry, shared by the generated and the loaded pipeline. The two
// differ in how they resolve fonts (embedded sfnt with per-glyph fallback vs base-14 WinAnsi);
// where the mark is *placed* is this one implementation.
internal static class WatermarkGeometry
{
    public const string EncodingContext = "Watermark text";

    // The baseline drops by this fraction of the font size so the glyph body, not its
    // baseline, straddles the rotation centre.
    private const double BaselineFactor = 0.35;

    public static double Baseline(double size) => -size * BaselineFactor;

    // Places a run/image of the given extent so its midpoint sits on the rotation centre.
    public static double Centered(double extent) => -extent / 2;

    // "cos sin -sin cos cx cy cm": rotation about the page centre, which the caller has
    // already wrapped in q .. Q.
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
