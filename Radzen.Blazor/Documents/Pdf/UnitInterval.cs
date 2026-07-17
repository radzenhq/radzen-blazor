namespace Radzen.Documents.Pdf;

/// <summary>
/// Clipping for PDF values defined on the closed 0..1 interval (colour components,
/// alpha, gray levels).
/// </summary>
internal static class UnitInterval
{
    // Comparison-based rather than Math.Clamp so NaN propagates instead of being ordered;
    // callers that must map NaN to a channel go through ColorComponent.ToChannel.
    internal static double Clamp(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
