using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Content;


// Shared content-stream tokenizer for the page-content grammar (operators, operands,
// arrays/dicts and BI/ID/EI inline images captured whole as a single token). Both
// ContentInterpreter and TextExtractor consume this stream; each consumer filters the
// tokens it cares about. This is the content-stream grammar, distinct from the PDF
// object/file grammar in Objects/Lexer.cs.
internal static class ContentTokenizer
{
    internal enum TokenKind
    {
        Number,
        Name,
        String,
        ArrayStart,
        ArrayEnd,
        DictStart,
        DictEnd,
        Operator,
        InlineImage,
    }

    internal readonly record struct Token(TokenKind Kind, double Number, string? Text, byte[]? Bytes, int Start, int End);

    public static List<Token> Tokenize(byte[] data)
    {
        var tokens = new List<Token>();
        var position = 0;

        while (position < data.Length)
        {
            var b = data[position];

            if (IsWhitespace(b))
            {
                position++;
                continue;
            }

            switch (b)
            {
                case (byte)'%':
                    while (position < data.Length && data[position] != '\n' && data[position] != '\r')
                    {
                        position++;
                    }

                    continue;

                case (byte)'[':
                    tokens.Add(new Token(TokenKind.ArrayStart, 0, null, null, position, position + 1));
                    position++;
                    continue;

                case (byte)']':
                    tokens.Add(new Token(TokenKind.ArrayEnd, 0, null, null, position, position + 1));
                    position++;
                    continue;

                case (byte)'(':
                    var literalStart = position;
                    tokens.Add(new Token(TokenKind.String, 0, null, ReadLiteralString(data, ref position), literalStart, position));
                    continue;

                case (byte)'/':
                    var nameStart = position;
                    tokens.Add(new Token(TokenKind.Name, 0, ReadName(data, ref position), null, nameStart, position));
                    continue;

                case (byte)'<':
                    if (position + 1 < data.Length && data[position + 1] == '<')
                    {
                        tokens.Add(new Token(TokenKind.DictStart, 0, null, null, position, position + 2));
                        position += 2;
                        continue;
                    }

                    var hexStart = position;
                    tokens.Add(new Token(TokenKind.String, 0, null, ReadHexString(data, ref position), hexStart, position));
                    continue;

                case (byte)'>':
                    if (position + 1 < data.Length && data[position + 1] == '>')
                    {
                        tokens.Add(new Token(TokenKind.DictEnd, 0, null, null, position, position + 2));
                        position += 2;
                        continue;
                    }

                    position++;
                    continue;

                case (byte)'{':
                case (byte)'}':
                    position++;
                    continue;
            }

            if (IsNumberStart(b))
            {
                var start = position;
                while (position < data.Length && IsNumberChar(data[position]))
                {
                    position++;
                }

                // Numbers are ASCII, so the UTF-8 overload is the same parser over the raw
                // bytes and saves a transient string per numeric operand.
                if (double.TryParse(data.AsSpan(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    tokens.Add(new Token(TokenKind.Number, value, null, null, start, position));
                    continue;
                }
            }

            var keywordStart = position;
            while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
            {
                position++;
            }

            if (position == keywordStart)
            {
                position++;
                continue;
            }

            var keyword = Latin1(data, keywordStart, position - keywordStart);
            if (keyword == "BI")
            {
                SkipInlineImage(data, ref position);
                tokens.Add(new Token(TokenKind.InlineImage, 0, null, data[keywordStart..position], keywordStart, position));
                continue;
            }

            tokens.Add(new Token(TokenKind.Operator, 0, keyword, null, keywordStart, position));
        }

        return tokens;
    }

    // After a BI operator, skip the inline-image dict and its binary payload, resuming
    // past the EI that ends the image. The exact payload length is computed from the
    // dictionary (an explicit /L, or /W /H /BPC and the color-space component count for
    // an unfiltered image) so a payload that happens to contain the bytes "EI" is not
    // mistaken for the terminator. Only a filtered image with no /L - whose decoded
    // length the tokenizer cannot know - falls back to the whitespace-bounded EI scan.
    private static void SkipInlineImage(byte[] data, ref int position)
    {
        var image = ParseInlineImageDictionary(data, ref position);
        if (position >= data.Length)
        {
            return;
        }

        // Exactly one whitespace byte separates ID from the binary payload.
        if (IsWhitespace(data[position]))
        {
            position++;
        }

        // When the payload length is known, jump straight to it - but only commit if an EI
        // actually follows. A declared geometry that disagrees with the real payload (a
        // malformed image) falls through to the whitespace-bounded EI scan instead.
        var length = InlineImagePayloadLength(image);
        if (length >= 0 && position + length <= data.Length)
        {
            var probe = position + (int)length;
            while (probe < data.Length && IsWhitespace(data[probe]))
            {
                probe++;
            }

            if (probe + 1 < data.Length && data[probe] == (byte)'E' && data[probe + 1] == (byte)'I'
                && (probe + 2 >= data.Length || IsWhitespace(data[probe + 2]) || IsDelimiter(data[probe + 2])))
            {
                position = probe + 2;
                return;
            }
        }

        // EI ends the payload when bounded by whitespace/delimiter/EOF on the trailing side.
        // A preceding whitespace is the common case but not required: streams that pack the
        // image data flush against EI ("dataEI") must still terminate here rather than
        // swallowing the remainder of the content stream.
        while (position < data.Length)
        {
            if (data[position] == (byte)'E' && position + 1 < data.Length && data[position + 1] == (byte)'I'
                && (position + 2 >= data.Length || IsWhitespace(data[position + 2]) || IsDelimiter(data[position + 2])))
            {
                position += 2;
                return;
            }

            position++;
        }
    }

    // Reads the key/value pairs between BI and ID into the fields needed to size the
    // payload; leaves position just past the ID keyword.
    private static InlineImage ParseInlineImageDictionary(byte[] data, ref int position)
    {
        var image = new InlineImage();
        while (position < data.Length)
        {
            SkipTokenWhitespace(data, ref position);
            if (position >= data.Length)
            {
                break;
            }

            if (data[position] == (byte)'/')
            {
                var key = ReadName(data, ref position);
                SkipTokenWhitespace(data, ref position);
                var value = ReadInlineToken(data, ref position);
                image.Set(key, value);
                continue;
            }

            var keyword = ReadInlineToken(data, ref position);
            if (keyword == "ID")
            {
                return image;
            }
        }

        return image;
    }

    private static void SkipTokenWhitespace(byte[] data, ref int position)
    {
        while (position < data.Length)
        {
            if (IsWhitespace(data[position]))
            {
                position++;
            }
            else if (data[position] == (byte)'%')
            {
                while (position < data.Length && data[position] != (byte)'\n' && data[position] != (byte)'\r')
                {
                    position++;
                }
            }
            else
            {
                break;
            }
        }
    }

    // Reads a single dictionary value: a name (returned with a leading '/'), an
    // array/dictionary (skipped and returned as null), or a bare token (number, boolean
    // or keyword). Only the shapes the payload-length computation needs are distinguished.
    private static string? ReadInlineToken(byte[] data, ref int position)
    {
        if (position >= data.Length)
        {
            return null;
        }

        var b = data[position];
        if (b == (byte)'/')
        {
            return "/" + ReadName(data, ref position);
        }

        if (b == (byte)'[' || b == (byte)'<')
        {
            SkipInlineComposite(data, ref position);
            return null;
        }

        var start = position;
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            position++;
        }

        if (position > start)
        {
            return Latin1(data, start, position - start);
        }

        // A stray delimiter that is not a name/array/dict start: consume one byte so the
        // dictionary scan always makes progress and never spins to EOF.
        position++;
        return null;
    }

    private static void SkipInlineComposite(byte[] data, ref int position)
    {
        var depth = 0;
        while (position < data.Length)
        {
            var b = data[position];
            if (b == (byte)'[' || (b == (byte)'<' && position + 1 < data.Length && data[position + 1] == (byte)'<'))
            {
                depth++;
                position += b == (byte)'<' ? 2 : 1;
            }
            else if (b == (byte)']' || (b == (byte)'>' && position + 1 < data.Length && data[position + 1] == (byte)'>'))
            {
                depth--;
                position += b == (byte)'>' ? 2 : 1;
                if (depth <= 0)
                {
                    return;
                }
            }
            else
            {
                position++;
            }
        }
    }

    private static long InlineImagePayloadLength(InlineImage image)
    {
        if (image.Length >= 0)
        {
            return image.Length;
        }

        // A filtered image's on-disk length is its compressed size, which cannot be
        // derived from the geometry; defer to the EI scan.
        if (image.Filtered)
        {
            return -1;
        }

        if (image.Width <= 0 || image.Height <= 0)
        {
            return -1;
        }

        var components = image.Components();
        if (components <= 0)
        {
            return -1;
        }

        var bits = image.ImageMask ? 1 : image.BitsPerComponent;
        if (bits <= 0)
        {
            return -1;
        }

        var bitsPerRow = (long)image.Width * components * bits;
        var bytesPerRow = (bitsPerRow + 7) / 8;
        return bytesPerRow * image.Height;
    }

    private sealed class InlineImage
    {
        public int Width { get; private set; } = -1;

        public int Height { get; private set; } = -1;

        public int BitsPerComponent { get; private set; } = -1;

        public long Length { get; private set; } = -1;

        public bool ImageMask { get; private set; }

        public bool Filtered { get; private set; }

        private string? colorSpace;

        public void Set(string key, string? value)
        {
            switch (key)
            {
                case "W" or "Width":
                    Width = ParseInt(value, Width);
                    break;
                case "H" or "Height":
                    Height = ParseInt(value, Height);
                    break;
                case "BPC" or "BitsPerComponent":
                    BitsPerComponent = ParseInt(value, BitsPerComponent);
                    break;
                case "L" or "Length":
                    if (value is not null && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var len))
                    {
                        Length = len;
                    }

                    break;
                case "IM" or "ImageMask":
                    ImageMask = value == "true";
                    break;
                case "CS" or "ColorSpace":
                    colorSpace = value;
                    break;
                case "F" or "Filter":
                    // A null value means an array/dict filter list; a slash name is a
                    // single filter. Either way the payload is compressed.
                    Filtered = value is null || value.StartsWith('/');
                    break;
            }
        }

        public int Components() => colorSpace switch
        {
            "/G" or "/DeviceGray" or "/CalGray" or "/I" or "/Indexed" => 1,
            "/RGB" or "/DeviceRGB" or "/CalRGB" or "/Lab" => 3,
            "/CMYK" or "/DeviceCMYK" => 4,
            null when ImageMask => 1,
            _ => -1,
        };

        private static int ParseInt(string? value, int fallback)
            => value is not null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
    }

    private static byte[] ReadLiteralString(byte[] data, ref int position)
    {
        var bytes = new List<byte>();
        var depth = 0;
        position++;

        while (position < data.Length)
        {
            var b = data[position++];
            if (b == '\\')
            {
                if (position >= data.Length)
                {
                    break;
                }

                var e = data[position++];
                switch (e)
                {
                    case (byte)'n': bytes.Add((byte)'\n'); break;
                    case (byte)'r': bytes.Add((byte)'\r'); break;
                    case (byte)'t': bytes.Add((byte)'\t'); break;
                    case (byte)'b': bytes.Add((byte)'\b'); break;
                    case (byte)'f': bytes.Add((byte)'\f'); break;
                    case (byte)'(': bytes.Add((byte)'('); break;
                    case (byte)')': bytes.Add((byte)')'); break;
                    case (byte)'\\': bytes.Add((byte)'\\'); break;
                    case (byte)'\r':
                        if (position < data.Length && data[position] == '\n')
                        {
                            position++;
                        }

                        break;
                    case (byte)'\n':
                        break;
                    default:
                        if (e is >= (byte)'0' and <= (byte)'7')
                        {
                            var value = e - '0';
                            for (var k = 0; k < 2 && position < data.Length && data[position] is >= (byte)'0' and <= (byte)'7'; k++)
                            {
                                value = (value * 8) + (data[position++] - '0');
                            }

                            bytes.Add((byte)value);
                        }
                        else
                        {
                            bytes.Add(e);
                        }

                        break;
                }

                continue;
            }

            if (b == '(')
            {
                depth++;
                bytes.Add(b);
                continue;
            }

            if (b == ')')
            {
                if (depth == 0)
                {
                    break;
                }

                depth--;
                bytes.Add(b);
                continue;
            }

            // ISO 32000-1 7.3.4.2: an unescaped CR, LF or CRLF is an end-of-line marker and
            // decodes to a single LF, matching Objects/Lexer.ReadLiteralString.
            if (b == 13)
            {
                bytes.Add(10);
                if (position < data.Length && data[position] == 10)
                {
                    position++;
                }

                continue;
            }

            bytes.Add(b);
        }

        return [.. bytes];
    }

    private static byte[] ReadHexString(byte[] data, ref int position)
    {
        var bytes = new List<byte>();
        position++;
        var high = -1;

        while (position < data.Length && data[position] != '>')
        {
            var b = data[position++];
            if (IsWhitespace(b))
            {
                continue;
            }

            var digit = HexDigit(b);
            if (digit < 0)
            {
                continue;
            }

            if (high < 0)
            {
                high = digit;
            }
            else
            {
                bytes.Add((byte)((high << 4) | digit));
                high = -1;
            }
        }

        if (high >= 0)
        {
            bytes.Add((byte)(high << 4));
        }

        if (position < data.Length)
        {
            position++;
        }

        return [.. bytes];
    }

    private static string ReadName(byte[] data, ref int position)
    {
        position++;
        var builder = new StringBuilder();
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            var b = data[position++];
            if (b == '#' && position + 1 < data.Length)
            {
                var hi = HexDigit(data[position]);
                var lo = HexDigit(data[position + 1]);
                if (hi >= 0 && lo >= 0)
                {
                    builder.Append((char)((hi << 4) | lo));
                    position += 2;
                    continue;
                }
            }

            builder.Append((char)b);
        }

        return builder.ToString();
    }

    private static int HexDigit(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - '0',
        >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
        >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
        _ => -1,
    };

    private static string Latin1(byte[] data, int start, int length) => Encoding.Latin1.GetString(data, start, length);

    private static bool IsWhitespace(byte b) => Lexer.IsWhitespace(b);

    private static bool IsDelimiter(byte b) => Lexer.IsDelimiter(b);

    private static bool IsNumberStart(byte b) => b is (byte)'+' or (byte)'-' or (byte)'.' or (>= (byte)'0' and <= (byte)'9');

    private static bool IsNumberChar(byte b) => b is (byte)'+' or (byte)'-' or (byte)'.' or (byte)'e' or (byte)'E' or (>= (byte)'0' and <= (byte)'9');
}
