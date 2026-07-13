using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf;


// Accumulates a page content stream and the base-14 font and image XObject resources
// it references. The key prefixes keep overlay streams from colliding with generated
// resources.
internal sealed class ContentWriter(string fontKeyPrefix = "F", string imageKeyPrefix = "Im") : IDisposable
{
    private byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024);
    private int length;
    private bool returned;
    private readonly Dictionary<string, string> keysByBaseFont = new(StringComparer.Ordinal);
    private readonly List<KeyValuePair<string, ImageXObject>> images = [];
    private readonly List<KeyValuePair<string, DictionaryObject>> patterns = [];
    private readonly List<GradientBrush> patternBrushes = [];

    public IEnumerable<KeyValuePair<string, string>> Fonts => keysByBaseFont;

    public IReadOnlyList<KeyValuePair<string, ImageXObject>> Images => images;

    public IReadOnlyList<KeyValuePair<string, DictionaryObject>> Patterns => patterns;

    // Registers a shading pattern for a gradient brush and returns its /Pattern resource name.
    // Deduplicates by brush reference so one brush reused across elements emits a single pattern
    // dictionary, matching PagePlan.RegisterPattern in the page-generation path.
    public string RegisterPattern(GradientBrush gradient)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        for (var i = 0; i < patternBrushes.Count; i++)
        {
            if (ReferenceEquals(patternBrushes[i], gradient))
            {
                return patterns[i].Key;
            }
        }

        var key = "P" + patterns.Count.ToString(CultureInfo.InvariantCulture);
        patterns.Add(new KeyValuePair<string, DictionaryObject>(key, ShadingBuilder.BuildPattern(gradient)));
        patternBrushes.Add(gradient);
        return key;
    }

    public byte[] ToArray() => buffer.AsSpan(0, length).ToArray();

    // Decodes and registers an image XObject, returning its resource name. An undecodable
    // payload throws (ImageDecoder.Decode) instead of silently emitting nothing: dropping
    // content would violate the fail-loud invariant.
    public string RegisterImage(byte[] encodedImage)
    {
        var decoded = ImageDecoder.Decode(encodedImage);
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

    public void WriteRaw(string text) => WriteRaw(text.AsSpan());

    public void WriteRaw(ReadOnlySpan<char> text)
    {
        var destination = Reserve(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            destination[i] = (byte)text[i];
        }

        length += text.Length;
    }

    public void WriteName(string name)
    {
        foreach (var ch in name)
        {
            // Names are byte sequences; a code point above Latin-1 cannot be represented
            // without silently aliasing to a different resource (e.g. U+0141 -> 'A').
            if (ch > 0xFF)
            {
                throw new NotSupportedException($"Name '{name}' contains a code point (U+{(int)ch:X4}) outside the encodable range.");
            }

            var code = ch & 0xFF;
            if (code <= 0x20 || code >= 0x7F || code == '#' || IsDelimiter(code))
            {
                WriteRaw(NameObject.Escape(name));
                return;
            }
        }

        Append((byte)'/');
        WriteRaw(name);
    }

    // Content-stream operands are emitted at 1/1000-unit precision: sub-0.001pt
    // coordinate rounding is invisible, and 3-decimal color/matrix values quantize
    // to the same 8-bit channels and glyph positions while shrinking the stream.
    public void WriteNumber(double value)
    {
        Span<char> chars = stackalloc char[32];
        if (value.TryFormat(chars, out var written, "0.###", CultureInfo.InvariantCulture))
        {
            WriteRaw(chars[..written]);
        }
        else
        {
            WriteRaw(value.ToString("0.###", CultureInfo.InvariantCulture));
        }
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
        Append((byte)'(');
        foreach (var b in bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                case (byte)'(':
                case (byte)')':
                    Append((byte)'\\');
                    Append(b);
                    break;
                default:
                    if (b < 0x20 || b == 0x7F)
                    {
                        Append((byte)'\\');
                        Append((byte)('0' + ((b >> 6) & 0x7)));
                        Append((byte)('0' + ((b >> 3) & 0x7)));
                        Append((byte)('0' + (b & 0x7)));
                    }
                    else
                    {
                        Append(b);
                    }

                    break;
            }
        }

        Append((byte)')');
    }

    private static bool IsDelimiter(int code) => code switch
    {
        '(' or ')' or '<' or '>' or '[' or ']' or '{' or '}' or '/' or '%' => true,
        _ => false,
    };

    private void Append(byte value)
    {
        if (length == buffer.Length)
        {
            Grow(1);
        }

        buffer[length++] = value;
    }

    private Span<byte> Reserve(int size)
    {
        if (buffer.Length - length < size)
        {
            Grow(size);
        }

        return buffer.AsSpan(length, size);
    }

    private void Grow(int size)
    {
        var pool = System.Buffers.ArrayPool<byte>.Shared;
        var replacement = pool.Rent(Math.Max(buffer.Length * 2, length + size));
        buffer.AsSpan(0, length).CopyTo(replacement);
        pool.Return(buffer);
        buffer = replacement;
    }

    // Returns the live buffer to the pool. Callers dispose only after ToArray has copied
    // out the bytes they need; the Fonts/Images maps stay valid for later resource emit.
    public void Dispose()
    {
        if (returned)
        {
            return;
        }

        returned = true;
        System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
    }
}
