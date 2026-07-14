using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf.Content;


/// <summary>
/// The write surface for a page content stream, passed to <see cref="ContentElement.EmitBody"/>.
/// Emits content-stream operators and registers the base-14 fonts, image XObjects and shading
/// patterns an element references, returning the resource name each is reached by.
/// </summary>
public sealed class ContentWriter : IDisposable
{
    private readonly string fontKeyPrefix;
    private readonly string imageKeyPrefix;
    private readonly string extGStateKeyPrefix;
    private byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
    private int length;
    private bool returned;
    private readonly Dictionary<string, string> keysByBaseFont = new(StringComparer.Ordinal);
    private readonly List<KeyValuePair<string, ImageXObject>> images = [];
    private readonly List<KeyValuePair<string, DictionaryObject>> patterns = [];
    private readonly List<GradientBrush> patternBrushes = [];
    private readonly List<KeyValuePair<string, double>> extGStates = [];

    // The key prefixes keep overlay streams from colliding with generated resources.
    internal ContentWriter(string fontKeyPrefix = "F", string imageKeyPrefix = "Im", string extGStateKeyPrefix = "GS")
    {
        this.fontKeyPrefix = fontKeyPrefix;
        this.imageKeyPrefix = imageKeyPrefix;
        this.extGStateKeyPrefix = extGStateKeyPrefix;
    }

    internal IEnumerable<KeyValuePair<string, string>> Fonts => keysByBaseFont;

    internal IReadOnlyList<KeyValuePair<string, ImageXObject>> Images => images;

    internal IReadOnlyList<KeyValuePair<string, DictionaryObject>> Patterns => patterns;

    internal string RegisterOpacity(double opacity)
    {
        var value = Math.Clamp(opacity, 0, 1);
        foreach (var state in extGStates)
        {
            if (state.Value == value)
            {
                return state.Key;
            }
        }

        var key = extGStateKeyPrefix + extGStates.Count.ToString(CultureInfo.InvariantCulture);
        extGStates.Add(new KeyValuePair<string, double>(key, value));
        return key;
    }

    internal string RegisterImage(ImageXObject image)
    {
        var key = imageKeyPrefix + images.Count.ToString(CultureInfo.InvariantCulture);
        images.Add(new KeyValuePair<string, ImageXObject>(key, image));
        return key;
    }

    /// <summary>
    /// Registers a shading pattern for <paramref name="gradient"/> and returns its <c>/Pattern</c>
    /// resource name. One brush reused across elements emits a single pattern dictionary.
    /// </summary>
    /// <param name="gradient">The gradient brush to register.</param>
    /// <returns>The resource name the pattern is selected by.</returns>
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

    internal byte[] ToArray() => buffer.AsSpan(0, length).ToArray();

    internal ContentEmissionResult DetachResult() => new(
        ToArray(),
        new ContentResourceManifest([.. keysByBaseFont], [.. images], [.. patterns], [.. extGStates]),
        isEmitted: true);

    /// <summary>
    /// Decodes and registers an image XObject for <paramref name="encodedImage"/> and returns its
    /// resource name. An undecodable payload throws rather than silently emitting nothing.
    /// </summary>
    /// <param name="encodedImage">The encoded image bytes (PNG, JPEG or JPEG2000).</param>
    /// <returns>The resource name the image is painted by.</returns>
    public string RegisterImage(byte[] encodedImage)
    {
        return RegisterImage(ImageDecoder.Decode(encodedImage));
    }

    /// <summary>Registers <paramref name="font"/> and returns the resource name its base-14 face is reached by.</summary>
    /// <param name="font">The font whose base-14 face to register.</param>
    /// <returns>The resource name the font is selected by.</returns>
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

    /// <summary>Appends <paramref name="text"/> to the content stream verbatim, one byte per character.</summary>
    /// <param name="text">The raw content-stream text; every character must be within the Latin-1 range.</param>
    public void WriteRaw(string text) => WriteRaw(text.AsSpan());

    /// <summary>Appends <paramref name="text"/> to the content stream verbatim, one byte per character.</summary>
    /// <param name="text">The raw content-stream text; every character must be within the Latin-1 range.</param>
    public void WriteRaw(ReadOnlySpan<char> text)
    {
        var destination = Reserve(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            destination[i] = (byte)text[i];
        }

        length += text.Length;
    }

    /// <summary>Writes <paramref name="name"/> as a PDF name object, escaping characters that require it.</summary>
    /// <param name="name">The name to write, without the leading solidus.</param>
    public void WriteName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
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

    /// <summary>Writes <paramref name="value"/> as a content-stream number at 1/1000-unit precision.</summary>
    /// <param name="value">The number to write.</param>
    // Sub-0.001pt coordinate rounding is invisible, and 3-decimal color/matrix values quantize
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

    /// <summary>Writes <paramref name="color"/> as an RGB triple followed by <paramref name="operatorName"/> and a newline.</summary>
    /// <param name="color">The colour to write, emitted as three 0..1 components.</param>
    /// <param name="operatorName">The colour operator to apply (for example <c>rg</c> or <c>RG</c>).</param>
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

    /// <summary>Writes <paramref name="bytes"/> as a parenthesised PDF literal string, escaping as required.</summary>
    /// <param name="bytes">The raw string bytes to write.</param>
    public void WriteString(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
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
        var pool = ArrayPool<byte>.Shared;
        var replacement = pool.Rent(Math.Max(buffer.Length * 2, length + size));
        buffer.AsSpan(0, length).CopyTo(replacement);
        pool.Return(buffer);
        buffer = replacement;
    }

    /// <summary>Returns the pooled internal buffer. Call only after the emitted bytes have been read out.</summary>
    public void Dispose()
    {
        if (returned)
        {
            return;
        }

        returned = true;
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
