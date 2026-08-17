using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Core;

namespace Radzen.Documents.Codes;

internal static class SvgAttributes
{
    private static readonly Regex ColorSyntax = new(
        @"^(?:#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})|(?:rgba?|hsla?)\([^<>&""']+\)|var\(--[A-Za-z0-9_-]+(?:,\s*[^<>&""']+)?\)|[A-Za-z]+)$",
        RegexOptions.CultureInvariant);

    // https://www.w3.org/TR/xml/#NT-AttValue - an attribute value may not contain '<', '&' or its delimiter.
    internal static string Escape(string value)
    {
        var index = value.AsSpan().IndexOfAny("<>&\"'");
        if (index < 0)
        {
            return value;
        }

        var escaped = new StringBuilder(value.Length + 16);
        foreach (var character in value)
        {
            switch (character)
            {
                case '<': escaped.Append("&lt;"); break;
                case '>': escaped.Append("&gt;"); break;
                case '&': escaped.Append("&amp;"); break;
                case '"': escaped.Append("&quot;"); break;
                case '\'': escaped.Append("&apos;"); break;
                default: escaped.Append(character); break;
            }
        }

        return escaped.ToString();
    }

    internal static string Color(string? value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? "none"
            : IsColor(value)
            ? Escape(value)
            : throw new ArgumentException($"'{value}' is not a valid CSS color.", parameterName);

    internal static bool IsColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !ColorSyntax.IsMatch(value))
        {
            return false;
        }

        return value[0] == '#'
            || value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("hsl", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("var(", StringComparison.OrdinalIgnoreCase)
            || IsKeyword(value)
            || ColorValue.Parse(value) is not null;
    }

    internal static string Href(string value, string parameterName)
        => IsAllowedHref(value)
            ? Escape(value)
            : throw new ArgumentException(
                $"'{value}' is not an allowed image reference; use an http, https or data:image URL.",
                parameterName);

    private static bool IsAllowedHref(string value)
        => value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

    // https://www.w3.org/TR/SVG11/types.html#DataTypeNumber - an SVG number is decimal or
    // exponent notation with an 'e', never the 'E' that .NET round-tripping emits.
    internal static string Number(double value)
        => value.ToString("0.################", CultureInfo.InvariantCulture);

    internal static (string Color, double Opacity) TranslucentFill(string value, string parameterName)
    {
        var escaped = Color(value, parameterName);
        if (ColorValue.Parse(value) is { } rgb && rgb.Alpha < 1)
        {
            return (
                $"rgb({Number(rgb.Red)}, {Number(rgb.Green)}, {Number(rgb.Blue)})",
                Math.Clamp(rgb.Alpha, 0, 1));
        }

        return (escaped, 1);
    }

    internal static (string Color, double Opacity) Fill(string? color, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return ("none", 1);
        }

        if (string.Equals(color, "transparent", StringComparison.OrdinalIgnoreCase))
        {
            return ("rgb(0, 0, 0)", 0);
        }

        if (!IsColor(color))
        {
            throw new ArgumentException($"'{color}' is not a valid CSS color.", parameterName);
        }

        if (ColorValue.Parse(color) is not { } rgb)
        {
            return (Escape(color), 1);
        }

        return (
            $"rgb({Number(rgb.Red)}, {Number(rgb.Green)}, {Number(rgb.Blue)})",
            Math.Clamp(rgb.Alpha, 0, 1));
    }

    internal static bool IsKeyword(string value)
        => string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "transparent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "currentColor", StringComparison.OrdinalIgnoreCase);
}
