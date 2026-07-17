using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf.Content;

internal readonly struct WatermarkTextPlan
{
    public required byte[] Bytes { get; init; }

    public required double X { get; init; }

    public required double Baseline { get; init; }

    public static WatermarkTextPlan Base14(string text, Font font)
    {
        var bytes = WinAnsiText.Encode(text, OnUnencodable.Throw, WatermarkGeometry.EncodingContext);
        var metrics = Base14Metrics.Resolve(font) ?? Base14Metrics.Resolve(new Font())!;
        return new WatermarkTextPlan
        {
            Bytes = bytes,
            X = WatermarkGeometry.Centered(metrics.MeasureString(text, font.Size)),
            Baseline = WatermarkGeometry.Baseline(font.Size),
        };
    }
}
