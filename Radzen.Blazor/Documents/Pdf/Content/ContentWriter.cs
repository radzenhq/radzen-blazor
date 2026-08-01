
using Radzen.Documents.Fonts;
using Radzen.Documents.Internal;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System;
using Radzen.Documents.Core;
namespace Radzen.Documents.Pdf.Content;


internal sealed record ContentResourcePrefixes(string Font, string Image, string ExtGState, string Pattern)
{
    public static ContentResourcePrefixes Page { get; } = new("F", "Im", "GS", "P");

    public static ContentResourcePrefixes Overlay { get; } = new("SF", "SIm", "SGS", "SP");

    public static ContentResourcePrefixes Appearance { get; } = new("AF", "AIm", "GS", "P");
}

/// <summary>
/// The write surface for a page content stream, passed to <see cref="ContentElement.EmitBody"/>.
/// Emits content-stream operators and registers the base-14 fonts, image XObjects and shading
/// patterns an element references, returning the resource name each is reached by.
/// </summary>
public sealed class ContentWriter : IDisposable
{
    private readonly PooledByteAccumulator accumulator = new(1024);
    private readonly ResourceNameAllocator<string, KeyValuePair<string, string>> fonts;
    private readonly ResourceNameAllocator<DecodedImage, KeyValuePair<string, DecodedImage>> images;
    private readonly ResourceNameAllocator<GradientBrush, KeyValuePair<string, DictionaryObject>> patterns;

    private readonly ResourceNameAllocator<string, KeyValuePair<string, double>> extGStates;

    private readonly FontScope scope;

    private readonly ImageDecoders decoders;

    internal ContentWriter(
        FontScope scope = default,
        ContentResourcePrefixes? prefixes = null,
        IEnumerable<string>? reserved = null,
        ImageDecoders? decoders = null)
    {
        this.scope = scope;
        this.decoders = decoders ?? ImageDecoders.Default;
        var names = prefixes ?? ContentResourcePrefixes.Page;
        fonts = new ResourceNameAllocator<string, KeyValuePair<string, string>>(names.Font, reserved, StringComparer.Ordinal);
        images = new ResourceNameAllocator<DecodedImage, KeyValuePair<string, DecodedImage>>(names.Image, reserved);
        patterns = new ResourceNameAllocator<GradientBrush, KeyValuePair<string, DictionaryObject>>(names.Pattern, reserved, ReferenceKeyComparer<GradientBrush>.Instance);
        extGStates = new ResourceNameAllocator<string, KeyValuePair<string, double>>(names.ExtGState, reserved, StringComparer.Ordinal);
    }

    internal IEnumerable<KeyValuePair<string, string>> Fonts => fonts.Values;

    internal IReadOnlyList<KeyValuePair<string, DecodedImage>> Images => images.Values;

    internal IReadOnlyList<KeyValuePair<string, DictionaryObject>> Patterns => patterns.Values;

    internal string RegisterOpacity(double opacity)
    {
        var value = Math.Clamp(opacity, 0, 1);
        return ExtGStateRegistration.RegisterAlpha(
            extGStates,
            value,
            value,
            blend: null,
            key => new KeyValuePair<string, double>(key, value));
    }

    internal string RegisterImage(DecodedImage image)
        => images.Add(key => new KeyValuePair<string, DecodedImage>(key, image));

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

    internal byte[] ToArray() => accumulator.ToArray();

    internal void WriteBytes(ReadOnlySpan<byte> bytes) => accumulator.Write(bytes);

    internal void EnsureSeparated()
    {
        var written = accumulator.WrittenSpan;
        if (written.Length == 0)
        {
            return;
        }

        var last = written[^1];
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
        ArgumentNullException.ThrowIfNull(encodedImage);
        return RegisterImage(decoders.Decode(encodedImage, ReaderLimits.Default));
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
        var destination = accumulator.Reserve(text.Length);
        Latin1ByteEncoder.Encode(text, destination);
        accumulator.Advance(text.Length);
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

    internal void WriteTjAdjustment(double pointsDelta, double denominator)
    {
        if (Math.Abs(pointsDelta) <= 0.000001)
        {
            return;
        }

        WriteRaw(" ");
        WriteNumber(pointsDelta / denominator * 1000.0);
        WriteRaw(" ");
    }

    internal static double RequireTjScale(double fontSize, double scale, string operation)
    {
        var denominator = fontSize * scale;
        if (!double.IsFinite(denominator) || Math.Abs(denominator) < 0.000001)
        {
            throw new NotSupportedException(
                $"{operation} text with a zero or non-finite font scale cannot preserve positioning safely.");
        }

        return denominator;
    }

    /// <summary>Writes <paramref name="color"/> as an RGB triple followed by <paramref name="operatorName"/> and a newline.</summary>
    /// <param name="color">The color to write, emitted as three 0..1 components.</param>
    /// <param name="operatorName">The color operator to apply (for example <c>rg</c> or <c>RG</c>).</param>
    public void WriteColor(Color color, string operatorName)
    {
        WriteNumber(PdfColor.Component(color.R));
        WriteRaw(" ");
        WriteNumber(PdfColor.Component(color.G));
        WriteRaw(" ");
        WriteNumber(PdfColor.Component(color.B));
        WriteRaw(" ");
        WriteRaw(operatorName);
        WriteRaw("\n");
    }

    /// <summary>Writes <paramref name="bytes"/> as a parenthesised PDF literal string, escaping as required.</summary>
    /// <param name="bytes">The raw string bytes to write.</param>
    public void WriteString(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder(bytes.Length + 2);
        PdfLiteralString.AppendEscaped(builder, bytes, binary: true);
        WriteRaw(builder.ToString());
    }

    private void Append(byte value) => accumulator.Append(value);

    /// <summary>Returns the pooled internal buffer. Call only after the emitted bytes have been read out.</summary>
    public void Dispose() => accumulator.Return();
}
