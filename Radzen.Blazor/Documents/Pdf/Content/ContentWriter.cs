
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

internal sealed class ContentWriter : IDisposable
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
        this.decoders = decoders ?? ImageDecoders.BuiltIn;
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

    internal string RegisterPattern(GradientBrush gradient)
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

    internal string RegisterImage(byte[] encodedImage)
    {
        ArgumentNullException.ThrowIfNull(encodedImage);
        return RegisterImage(decoders.Decode(encodedImage));
    }

    internal string RegisterFont(Font font)
    {
        ArgumentNullException.ThrowIfNull(font);
        var baseFont = FontResolution.ResolveBase14Name(font, scope);
        return fonts.GetOrAdd(baseFont, key => new KeyValuePair<string, string>(baseFont, key));
    }

    internal void WriteRaw(string text) => WriteRaw(text.AsSpan());

    internal void WriteRaw(ReadOnlySpan<char> text)
    {
        var destination = accumulator.Reserve(text.Length);
        Latin1ByteEncoder.Encode(text, destination);
        accumulator.Advance(text.Length);
    }

    internal void WriteName(string name)
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

    // ISO 32000-1 section 7.3.3: a PDF number has no token for NaN or infinity.
    internal void WriteNumber(double value)
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

    internal void WriteColor(Color color, string operatorName)
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

    internal void WriteString(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder(bytes.Length + 2);
        PdfLiteralString.AppendEscaped(builder, bytes, binary: true);
        WriteRaw(builder.ToString());
    }

    private void Append(byte value) => accumulator.Append(value);

    public void Dispose() => accumulator.Return();
}
