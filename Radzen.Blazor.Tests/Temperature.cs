using System;
using System.Globalization;

namespace Radzen.Blazor.Tests;

public record struct Temperature(decimal DegreesCelsius)
    : IFormattable
{
    public decimal Celsius => DegreesCelsius;
    public decimal Fahrenheit => DegreesCelsius * 9 / 5 + 32;
    public decimal Kelvin => DegreesCelsius + 273.15m;

    public override string ToString() => ToString("G");
    public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

    public string ToString(string format, IFormatProvider provider)
    {
        provider ??= CultureInfo.CurrentCulture;
        if (string.IsNullOrEmpty(format))
            format = "G";

        switch (format.ToUpperInvariant())
        {
            case "G":
            case "C":
                return $"{Celsius.ToString("F2", provider)} °C";
            case "F":
                return $"{Fahrenheit.ToString("F2", provider)} °F";
            case "K":
                return $"{Kelvin.ToString("F2", provider)} K";
            default:
                throw new FormatException($"The {format} format string is not supported.");
        }
    }
}