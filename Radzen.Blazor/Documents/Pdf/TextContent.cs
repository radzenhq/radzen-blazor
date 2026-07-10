using Radzen.Documents.Pdf.Fonts;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A run of text drawn at a fixed baseline position using a base-14 font and
/// WinAnsi encoding.
/// </summary>
/// <param name="text">The text to draw.</param>
/// <param name="x">The baseline X position.</param>
/// <param name="y">The baseline Y position.</param>
public sealed class TextContent(string text, Unit x, Unit y) : ContentElement
{
    /// <summary>Gets or sets the text to draw.</summary>
    public string Text { get; set; } = text;

    /// <summary>Gets or sets the font.</summary>
    public Font Font { get; set; } = new();

    /// <summary>Gets or sets the fill color of the text. Defaults to black.</summary>
    public Color Color { get; set; } = Color.Black;

    // Resource name captured when materializing a loaded page; when set, re-emission
    // keeps the original /Font reference instead of registering a base-14 face.
    internal string? FontResourceName { get; set; }

    internal override void EmitBody(ContentWriter writer)
    {
        var key = FontResourceName ?? writer.RegisterFont(Font);

        writer.WriteRaw("BT\n");
        writer.WriteColor(Color, "rg");
        writer.WriteName(key);
        writer.WriteRaw(" ");
        writer.WriteNumber(Font.Size);
        writer.WriteRaw(" Tf\n");
        writer.WriteNumber(x.Point);
        writer.WriteRaw(" ");
        writer.WriteNumber(y.Point);
        writer.WriteRaw(" Td\n");
        writer.WriteString(Encode(Text));
        writer.WriteRaw(" Tj\n");
        writer.WriteRaw("ET\n");
    }

    private static byte[] Encode(string text)
    {
        var bytes = new List<byte>(text.Length);
        foreach (var c in text)
        {
            if (WinAnsiEncoding.TryGetCode(c, out var code))
            {
                bytes.Add(code);
            }
        }

        return [.. bytes];
    }
}
