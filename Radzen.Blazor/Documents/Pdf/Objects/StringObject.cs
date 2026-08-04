using System;
using System.IO;
using System.Text;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.4.2: a literal string is parenthesised. A value containing characters
// above U+00FF is written as a UTF-16BE text string with a BOM (ISO 32000-2 7.9.2.2).
internal sealed class StringObject(string value) : DocumentObject
{
    public string Value { get; } = value;

    internal override void Write(Stream stream, WriteContext context)
    {
        var bytes = EncodeBytes(Value);
        var encryptor = context.Encryptor;
        if (encryptor is not null)
        {
            bytes = encryptor.EncryptString(bytes, context.ObjectNumber, context.Generation);
        }

        var builder = new StringBuilder(bytes.Length + 2);
        PdfLiteralString.AppendEscaped(builder, bytes, binary: false);
        PdfBytes.WriteAscii(stream, builder.ToString());
    }

    internal static StringObject FromText(string value)
    {
        foreach (var ch in value)
        {
            if (ch > 0xFF || PdfDocEncoding.IsRemapped(ch))
            {
                return new StringObject(Encoding.Latin1.GetString(Utf16WithBom(value)));
            }
        }

        return new StringObject(value);
    }

    private static byte[] EncodeBytes(string value)
    {
        foreach (var ch in value)
        {
            if (ch > 0xFF)
            {
                return Utf16WithBom(value);
            }
        }

        var raw = new byte[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            raw[i] = (byte)value[i];
        }

        return raw;
    }

    private static byte[] Utf16WithBom(string value)
    {
        var bytes = new byte[2 + Encoding.BigEndianUnicode.GetByteCount(value)];
        bytes[0] = 0xFE;
        bytes[1] = 0xFF;
        Encoding.BigEndianUnicode.GetBytes(value, 0, value.Length, bytes, 2);
        return bytes;
    }

}

internal static class PdfLiteralString
{
    private interface IEscapeSink
    {
        void Put(char value);

        void Put(string value);
    }

    private readonly struct BuilderSink(StringBuilder builder) : IEscapeSink
    {
        public void Put(char value) => builder.Append(value);

        public void Put(string value) => builder.Append(value);
    }

    private readonly struct AccumulatorSink(PooledByteAccumulator accumulator) : IEscapeSink
    {
        public void Put(char value) => accumulator.Append((byte)value);

        public void Put(string value)
        {
            foreach (var character in value)
            {
                accumulator.Append((byte)character);
            }
        }
    }

    public static void AppendEscaped(StringBuilder builder, ReadOnlySpan<byte> bytes, bool binary)
        => Escape(new BuilderSink(builder), bytes, binary);

    public static void WriteEscaped(PooledByteAccumulator accumulator, ReadOnlySpan<byte> bytes, bool binary)
        => Escape(new AccumulatorSink(accumulator), bytes, binary);

    // ISO 32000-1 7.3.4.2: a literal string is parenthesised; the backslash and unbalanced
    // parentheses are escaped, the named control escapes and three-digit octal escapes are
    // permitted, and any other byte may be written raw.
    private static void Escape<TSink>(TSink sink, ReadOnlySpan<byte> bytes, bool binary)
        where TSink : struct, IEscapeSink
    {
        sink.Put('(');
        foreach (var b in bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                    sink.Put("\\\\");
                    break;
                case (byte)'(':
                    sink.Put("\\(");
                    break;
                case (byte)')':
                    sink.Put("\\)");
                    break;
                case (byte)'\n' when !binary:
                    sink.Put("\\n");
                    break;
                case (byte)'\r' when !binary:
                    sink.Put("\\r");
                    break;
                case (byte)'\t' when !binary:
                    sink.Put("\\t");
                    break;
                case (byte)'\b' when !binary:
                    sink.Put("\\b");
                    break;
                case (byte)'\f' when !binary:
                    sink.Put("\\f");
                    break;
                default:
                    if ((b >= 0x20 && b <= 0x7E) || (binary && b >= 0x80))
                    {
                        sink.Put((char)b);
                    }
                    else
                    {
                        sink.Put('\\');
                        sink.Put((char)('0' + ((b >> 6) & 0x7)));
                        sink.Put((char)('0' + ((b >> 3) & 0x7)));
                        sink.Put((char)('0' + (b & 0x7)));
                    }

                    break;
            }
        }

        sink.Put(')');
    }
}
