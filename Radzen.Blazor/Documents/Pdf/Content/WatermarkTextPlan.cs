using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf.Content;

internal readonly struct WatermarkTextPlan
{
    public required byte[] Bytes { get; init; }

    public required double X { get; init; }

    public required double Baseline { get; init; }

    public static WatermarkTextPlan Base14(string text, Font font)
    {
        var bytes = WinAnsiText.Encode(text, OnUnencodable.Throw, "Watermark text");
        var metrics = BuiltInFontMetrics.Resolve(font) ?? BuiltInFontMetrics.Resolve(new Font())!;
        return new WatermarkTextPlan
        {
            Bytes = bytes,
            X = WatermarkGeometry.Centered(metrics.MeasureString(text, font.EffectiveSize.Point)),
            Baseline = -WatermarkGeometry.Baseline(font.EffectiveSize.Point),
        };
    }
}
