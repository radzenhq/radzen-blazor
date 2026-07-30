using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Radzen.Documents.Codes;

internal static class SvgAttributes
{
    private static readonly Regex ColorSyntax = new(
        @"^(?:#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})|rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*(?:,\s*\d*\.?\d+\s*)?\)|[A-Za-z]+)$",
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

    internal static string Color(string value, string parameterName)
        => IsColor(value)
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
            || IsKeyword(value)
            || ColorValue.Parse(value) is not null;
    }

    internal static bool IsKeyword(string value)
        => string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "transparent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "currentColor", StringComparison.OrdinalIgnoreCase);
}
