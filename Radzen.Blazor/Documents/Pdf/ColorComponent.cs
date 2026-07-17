using System;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Converts a PDF colour component in the 0 to 1 range to an 8-bit channel.
/// </summary>
internal static class ColorComponent
{
    /// <summary>
    /// Quantizes <paramref name="value"/> to a channel byte, clipping it to the 0 to 1 range.
    /// </summary>
    // PDF clips out-of-range colour components rather than rejecting the document, and a
    // half-way component rounds away from zero: Math.Round's ToEven default would read a
    // plain "0.3 0.3 0.3 rg" as 76 rather than 77.
    internal static byte ToChannel(double value)
    {
        var clamped = Math.Clamp(value, 0, 1);

        return double.IsNaN(clamped) ? (byte)0 : (byte)Math.Round(clamped * 255, MidpointRounding.AwayFromZero);
    }
}
