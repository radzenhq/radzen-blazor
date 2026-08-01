using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Radzen.Documents.Core;

namespace Radzen.Documents;

#nullable enable

internal readonly record struct ColorValue(double Red, double Green, double Blue, double Alpha)
{
    private static readonly Regex RgbPattern = new(@"rgb\((?<r>\d{1,3}),\s*(?<g>\d{1,3}),\s*(?<b>\d{1,3})\)");

    private static readonly Regex RgbaPattern = new(@"rgba\((?<r>\d{1,3}),\s*(?<g>\d{1,3}),\s*(?<b>\d{1,3}),\s*(?<a>\d?\.?\d+)\)");

    private static readonly Regex HexPattern = new("(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})");

    public static ColorValue? Parse(string color)
    {
        if (string.IsNullOrEmpty(color))
        {
            return null;
        }

        var match = RgbPattern.Match(color);

        if (match.Success)
        {
            var red = int.Parse(match.Groups["r"].Value, CultureInfo.InvariantCulture);
            var green = int.Parse(match.Groups["g"].Value, CultureInfo.InvariantCulture);
            var blue = int.Parse(match.Groups["b"].Value, CultureInfo.InvariantCulture);

            return new ColorValue(red, green, blue, 1);
        }

        match = RgbaPattern.Match(color);

        if (match.Success)
        {
            var red = int.Parse(match.Groups["r"].Value, CultureInfo.InvariantCulture);
            var green = int.Parse(match.Groups["g"].Value, CultureInfo.InvariantCulture);
            var blue = int.Parse(match.Groups["b"].Value, CultureInfo.InvariantCulture);
            var alpha = double.Parse(match.Groups["a"].Value, CultureInfo.InvariantCulture);

            return new ColorValue(red, green, blue, alpha);
        }

        match = HexPattern.Match(color);

        if (match.Success)
        {
            var red = int.Parse(match.Groups["r"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var green = int.Parse(match.Groups["g"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var blue = int.Parse(match.Groups["b"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return new ColorValue(red, green, blue, 1);
        }

        if (NamedColors.TryGet(color, out var rgb))
        {
            return new ColorValue((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, 1);
        }

        return null;
    }

    public string ToHex()
    {
        var red = Convert.ToInt32(Red);
        var green = Convert.ToInt32(Green);
        var blue = Convert.ToInt32(Blue);
        return $"{red.ToString("X2", CultureInfo.InvariantCulture)}{green.ToString("X2", CultureInfo.InvariantCulture)}{blue.ToString("X2", CultureInfo.InvariantCulture)}";
    }
}
