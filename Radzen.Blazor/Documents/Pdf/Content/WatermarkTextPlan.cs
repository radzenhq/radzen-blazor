using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf.Content;

// The font handle the bytes are shown with (embedded subset vs base-14 name) deliberately
// stays out: that is the CanEmbed capability of the emitting site, not a property of the mark.
internal readonly struct WatermarkTextPlan
{
    public required byte[] Bytes { get; init; }

    public required double X { get; init; }

    public required double Baseline { get; init; }

    // Base-14 watermarks are WinAnsi-only; fail loud rather than silently dropping
    // unrepresentable codepoints, which would blank or mangle the watermark. Encoding throws
    // before this returns, so a caller registering its font from the returned plan cannot
    // register one for a rejected mark.
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
