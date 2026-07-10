using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf;

#nullable enable

// Accumulates a page content stream and the base-14 font resources it references.
// The font key prefix keeps overlay streams from colliding with generated resources.
internal sealed class ContentWriter(string fontKeyPrefix = "F")
{
    private readonly List<byte> buffer = [];
    private readonly Dictionary<string, string> keysByBaseFont = new(System.StringComparer.Ordinal);

    public IReadOnlyList<byte> Buffer => buffer;

    public IEnumerable<KeyValuePair<string, string>> Fonts => keysByBaseFont;

    public byte[] ToArray() => [.. buffer];

    public string RegisterFont(Font font)
    {
        var baseFont = Base14Metrics.Resolve(font)?.PostScriptName ?? "Helvetica";
        if (!keysByBaseFont.TryGetValue(baseFont, out var key))
        {
            key = fontKeyPrefix + keysByBaseFont.Count.ToString(CultureInfo.InvariantCulture);
            keysByBaseFont[baseFont] = key;
        }

        return key;
    }

    public void WriteRaw(string text)
    {
        foreach (var c in text)
        {
            buffer.Add((byte)c);
        }
    }

    public void WriteName(string name)
    {
        WriteRaw(NameObject.Escape(name));
    }

    public void WriteNumber(double value)
    {
        WriteRaw(value.ToString("0.######", CultureInfo.InvariantCulture));
    }

    public void WriteColor(Color color, string operatorName)
    {
        WriteNumber(color.R / 255.0);
        WriteRaw(" ");
        WriteNumber(color.G / 255.0);
        WriteRaw(" ");
        WriteNumber(color.B / 255.0);
        WriteRaw(" ");
        WriteRaw(operatorName);
        WriteRaw("\n");
    }

    public void WriteString(byte[] bytes)
    {
        buffer.Add((byte)'(');
        foreach (var b in bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                case (byte)'(':
                case (byte)')':
                    buffer.Add((byte)'\\');
                    buffer.Add(b);
                    break;
                default:
                    if (b < 0x20 || b == 0x7F)
                    {
                        buffer.Add((byte)'\\');
                        WriteRaw(System.Convert.ToString(b, 8).PadLeft(3, '0'));
                    }
                    else
                    {
                        buffer.Add(b);
                    }

                    break;
            }
        }

        buffer.Add((byte)')');
    }
}
