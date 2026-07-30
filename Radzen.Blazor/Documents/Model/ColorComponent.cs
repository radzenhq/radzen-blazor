using System;

namespace Radzen.Documents;

internal static class ColorComponent
{
    internal static byte ToChannel(double value)
    {
        var clamped = Math.Clamp(value, 0, 1);

        return double.IsNaN(clamped) ? (byte)0 : (byte)Math.Round(clamped * 255, MidpointRounding.AwayFromZero);
    }
}
