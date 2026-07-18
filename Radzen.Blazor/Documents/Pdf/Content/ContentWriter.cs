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
    private byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
    private int length;
    private bool returned;
    private readonly ResourceKeyRegistry<string, KeyValuePair<string, string>> fonts;
    private readonly ResourceKeyRegistry<ImageXObject, KeyValuePair<string, ImageXObject>> images;
    private readonly ResourceKeyRegistry<GradientBrush, KeyValuePair<string, DictionaryObject>> patterns;

    private readonly ResourceKeyRegistry<double, KeyValuePair<string, double>> extGStates;

    private readonly FontScope scope;

    internal ContentWriter(FontScope scope = default, string fontKeyPrefix = "F", string imageKeyPrefix = "Im", string extGStateKeyPrefix = "GS", string patternKeyPrefix = "P")
    {
        this.scope = scope;
        fonts = new ResourceKeyRegistry<string, KeyValuePair<string, string>>(fontKeyPrefix, StringComparer.Ordinal);
        images = new ResourceKeyRegistry<ImageXObject, KeyValuePair<string, ImageXObject>>(imageKeyPrefix);
        patterns = new ResourceKeyRegistry<GradientBrush, KeyValuePair<string, DictionaryObject>>(patternKeyPrefix, ReferenceKeyComparer<GradientBrush>.Instance);
        extGStates = new ResourceKeyRegistry<double, KeyValuePair<string, double>>(extGStateKeyPrefix, AlphaComparer.Instance);
    }

    internal IEnumerable<KeyValuePair<string, string>> Fonts => fonts.Values;

    internal IReadOnlyList<KeyValuePair<string, ImageXObject>> Images => images.Values;

    internal IReadOnlyList<KeyValuePair<string, DictionaryObject>> Patterns => patterns.Values;

    internal string RegisterOpacity(double opacity)
    {
        var value = Math.Clamp(opacity, 0, 1);
        return extGStates.GetOrAdd(value, key => new KeyValuePair<string, double>(key, value));
    }

    internal string RegisterImage(ImageXObject image)
        => images.Add(key => new KeyValuePair<string, ImageXObject>(key, image));

    /// <summary>
    /// Registers a shading pattern for <paramref name="gradient"/> and returns its <c>/Pattern</c>
    /// resource name. One brush reused across elements emits a single pattern dictionary.
    /// </summary>
    /// <param name="gradient">The gradient brush to register.</param>
    /// <returns>The resource name the pattern is selected by.</returns>
    public string RegisterPattern(GradientBrush gradient)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        return patterns.GetOrAdd(
            gradient,
            key => new KeyValuePair<string, DictionaryObject>(key, ShadingBuilder.BuildPattern(gradient)));
    }

    internal byte[] ToArray() => buffer.AsSpan(0, length).ToArray();

    internal void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(Reserve(bytes.Length));
        length += bytes.Length;
    }

    internal void EnsureSeparated()
    {
        if (length == 0)
        {
            return;
        }

        var last = buffer[length - 1];
        if (!Lexer.IsWhitespace(last) && !Lexer.IsDelimiter(last))
        {
            WriteRaw("\n");
        }
    }

    internal ContentEmissionResult DetachResult() => new(
        ToArray(),
        new ContentResourceManifest([.. fonts.Values], [.. images.Values], [.. patterns.Values], [.. extGStates.Values]),
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
    /// <exception cref="NotSupportedException">
    /// <paramref name="font"/> names a family this stream cannot reach: one with no base-14 face,
    /// or one registered as an embeddable font file, which this stream cannot embed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The document's conformance level forbids referencing a base-14 face by name.
    /// </exception>
    public string RegisterFont(Font font)
    {
        ArgumentNullException.ThrowIfNull(font);
        var baseFont = FontResolution.ResolveBase14Name(font, scope);
        return fonts.GetOrAdd(baseFont, key => new KeyValuePair<string, string>(baseFont, key));
    }

    /// <summary>Appends <paramref name="text"/> to the content stream verbatim, one byte per character.</summary>
    /// <param name="text">The raw content-stream text; every character must be within the Latin-1 range.</param>
    public void WriteRaw(string text) => WriteRaw(text.AsSpan());

    /// <summary>Appends <paramref name="text"/> to the content stream verbatim, one byte per character.</summary>
    /// <param name="text">The raw content-stream text; every character must be within the Latin-1 range.</param>
    public void WriteRaw(ReadOnlySpan<char> text)
    {
        var destination = Reserve(text.Length);
        Latin1ByteEncoder.Encode(text, destination);
        length += text.Length;
    }

    /// <summary>Writes <paramref name="name"/> as a PDF name object, escaping characters that require it.</summary>
    /// <param name="name">The name to write, without the leading solidus.</param>
    public void WriteName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (NameObject.NeedsEscaping(name))
        {
            WriteRaw(NameObject.Escape(name));
            return;
        }

        Append((byte)'/');
        WriteRaw(name);
    }

    /// <summary>Writes <paramref name="value"/> as a content-stream number at 1/1000-unit precision.</summary>
    /// <param name="value">The number to write.</param>
    /// <exception cref="InvalidOperationException">The value is NaN or infinite; PDF
    /// has no valid token for non-finite numbers (ISO 32000-1 section 7.3.3).</exception>
    public void WriteNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException("A PDF number cannot be NaN or infinite.");
        }

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
    public void WriteString(ReadOnlySpan<byte> bytes)
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
