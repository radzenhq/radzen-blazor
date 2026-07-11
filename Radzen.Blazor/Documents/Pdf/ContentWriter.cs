using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf;

#nullable enable

// Accumulates a page content stream and the base-14 font and image XObject resources
// it references. The key prefixes keep overlay streams from colliding with generated
// resources.
internal sealed class ContentWriter(string fontKeyPrefix = "F", string imageKeyPrefix = "Im")
{
    private readonly List<byte> buffer = [];
    private readonly Dictionary<string, string> keysByBaseFont = new(System.StringComparer.Ordinal);
    private readonly List<KeyValuePair<string, ImageXObject>> images = [];

    public IReadOnlyList<byte> Buffer => buffer;

    public IEnumerable<KeyValuePair<string, string>> Fonts => keysByBaseFont;

    public IReadOnlyList<KeyValuePair<string, ImageXObject>> Images => images;

    public byte[] ToArray() => [.. buffer];

    // Returns null when the payload is not a decodable PNG/JPEG so the element can
    // degrade to emitting nothing instead of failing the whole save.
    public string? RegisterImage(byte[] encodedImage)
    {
        ImageXObject decoded;
        try
        {
            decoded = ImageDecoder.Decode(encodedImage);
        }
        catch (System.NotSupportedException)
        {
            return null;
        }

        var key = imageKeyPrefix + images.Count.ToString(CultureInfo.InvariantCulture);
        images.Add(new KeyValuePair<string, ImageXObject>(key, decoded));
        return key;
    }

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
