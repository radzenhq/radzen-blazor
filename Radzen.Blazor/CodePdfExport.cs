using System;
using System.Globalization;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor
{
    internal static class CodePdfExport
    {
        public static double PointsOrDefault(string? css, double fallback)
        {
            if (!string.IsNullOrEmpty(css) && css.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
                double.TryParse(css[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels) && pixels > 0)
            {
                return pixels * 72 / 96;
            }

            return fallback;
        }

        public static Color ParseColor(string? value, Color fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            if (ColorValue.Parse(value) is { Alpha: > 0 } parsed)
            {
                return Color.FromRgb((byte)parsed.Red, (byte)parsed.Green, (byte)parsed.Blue);
            }

            try
            {
                return Color.FromHex(value);
            }
            catch (FormatException)
            {
                return fallback;
            }
        }
    }
}
