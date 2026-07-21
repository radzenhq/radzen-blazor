using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Content;


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

    internal sealed class Cache
    {
        private byte[]? source;
        private List<Token>? tokens;

        public List<Token> Get(byte[] data)
        {
            if (!ReferenceEquals(source, data))
            {
                source = data;
                tokens = Tokenize(data);
            }

            return tokens!;
        }
    }

    public static List<Token> Tokenize(byte[] data, Cache? cache) => cache is null ? Tokenize(data) : cache.Get(data);

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
                    tokens.Add(new Token(TokenKind.String, 0, null, Lexer.ReadLiteralString(data, ref position, Lexer.Recovery.Lenient), literalStart, position));
                    continue;

                case (byte)'/':
                    var nameStart = position;
                    tokens.Add(new Token(TokenKind.Name, 0, Lexer.ReadName(data, ref position), null, nameStart, position));
                    continue;

                case (byte)'<':
                    if (position + 1 < data.Length && data[position + 1] == '<')
                    {
                        tokens.Add(new Token(TokenKind.DictStart, 0, null, null, position, position + 2));
                        position += 2;
                        continue;
                    }

                    var hexStart = position;
                    tokens.Add(new Token(TokenKind.String, 0, null, Lexer.ReadHexString(data, ref position, Lexer.Recovery.Lenient), hexStart, position));
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
                var number = Lexer.ReadNumber(data, ref position, Lexer.Recovery.Lenient);
                if (number is { } value)
                {
                    tokens.Add(new Token(TokenKind.Number, value.RealValue, null, null, start, position));
                }

                continue;
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

    private static void SkipInlineImage(byte[] data, ref int position)
    {
        var image = ParseInlineImageDictionary(data, ref position);
        if (position >= data.Length)
        {
            return;
        }

        if (IsWhitespace(data[position]))
        {
            position++;
        }

        var end = InlineImagePayloadEnd(data, position, image);
        if (end >= 0)
        {
            var probe = end;
            while (probe < data.Length && IsWhitespace(data[probe]))
            {
                probe++;
            }

            if (IsInlineImageTerminator(data, probe))
            {
                position = probe + 2;
                return;
            }
        }

        ScanForInlineImageTerminator(data, ref position);
    }

    private static void ScanForInlineImageTerminator(byte[] data, ref int position)
    {
        var first = -1;
        var probe = position;
        while (probe < data.Length)
        {
            if (IsInlineImageTerminator(data, probe))
            {
                if (first < 0)
                {
                    first = probe + 2;
                }

                if (LooksLikeOperatorStream(data, probe + 2))
                {
                    position = probe + 2;
                    return;
                }
            }

            probe++;
        }

        position = first < 0 ? data.Length : first;
    }

    private static bool IsInlineImageTerminator(byte[] data, int position)
        => position + 1 < data.Length && data[position] == (byte)'E' && data[position + 1] == (byte)'I'
            && (position + 2 >= data.Length || IsWhitespace(data[position + 2]) || IsDelimiter(data[position + 2]));

    private const int InlineImageValidationBudget = 8;

    private static bool LooksLikeOperatorStream(byte[] data, int position)
    {
        for (var seen = 0; seen < InlineImageValidationBudget;)
        {
            SkipTokenWhitespace(data, ref position);
            if (position >= data.Length)
            {
                return true;
            }

            var b = data[position];
            switch (b)
            {
                case (byte)'[':
                case (byte)']':
                    position++;
                    seen++;
                    continue;

                case (byte)'(':
                    Lexer.ReadLiteralString(data, ref position, Lexer.Recovery.Lenient);
                    seen++;
                    continue;

                case (byte)'/':
                    Lexer.ReadName(data, ref position);
                    seen++;
                    continue;

                case (byte)'<':
                    if (position + 1 < data.Length && data[position + 1] == (byte)'<')
                    {
                        position += 2;
                    }
                    else
                    {
                        Lexer.ReadHexString(data, ref position, Lexer.Recovery.Lenient);
                    }

                    seen++;
                    continue;

                case (byte)'>':
                    if (position + 1 >= data.Length || data[position + 1] != (byte)'>')
                    {
                        return false;
                    }

                    position += 2;
                    seen++;
                    continue;

                case (byte)')':
                case (byte)'{':
                case (byte)'}':
                    return false;
            }

            if (IsNumberStart(b))
            {
                if (Lexer.ReadNumber(data, ref position, Lexer.Recovery.Lenient) is null)
                {
                    return false;
                }

                seen++;
                continue;
            }

            var keywordStart = position;
            while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
            {
                position++;
            }

            if (position == keywordStart || !ContentOperatorClass.IsContentOperator(Latin1(data, keywordStart, position - keywordStart)))
            {
                return false;
            }

            seen++;
        }

        return true;
    }

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
                var key = Lexer.ReadName(data, ref position);
                SkipTokenWhitespace(data, ref position);
                var value = ReadInlineToken(data, ref position, out var raw);
                image.Set(key, value, raw);
                continue;
            }

            var keyword = ReadInlineToken(data, ref position, out _);
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

    private static string? ReadInlineToken(byte[] data, ref int position, out string? raw)
    {
        raw = null;
        if (position >= data.Length)
        {
            return null;
        }

        var b = data[position];
        if (b == (byte)'/')
        {
            return raw = "/" + Lexer.ReadName(data, ref position);
        }

        if (b == (byte)'[' || b == (byte)'<')
        {
            var compositeStart = position;
            SkipInlineComposite(data, ref position);
            raw = Latin1(data, compositeStart, position - compositeStart);
            return null;
        }

        var start = position;
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            position++;
        }

        if (position > start)
        {
            return raw = Latin1(data, start, position - start);
        }

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

    private static int InlineImagePayloadEnd(byte[] data, int start, InlineImage image)
    {
        var length = InlineImagePayloadLength(image);
        if (length >= 0)
        {
            return start + length <= data.Length ? start + (int)length : -1;
        }

        return image.FirstFilter switch
        {
            "/AHx" or "/ASCIIHexDecode" => IndexAfter(data, start, (byte)'>'),
            "/A85" or "/ASCII85Decode" => IndexAfterAscii85(data, start),
            "/RL" or "/RunLengthDecode" => IndexAfterRunLength(data, start),
            _ => -1,
        };
    }

    private static int IndexAfter(byte[] data, int start, byte marker)
    {
        var index = System.Array.IndexOf(data, marker, start);
        return index < 0 ? -1 : index + 1;
    }

    private static int IndexAfterAscii85(byte[] data, int start)
    {
        for (var index = start; index + 1 < data.Length; index++)
        {
            if (data[index] == (byte)'~' && data[index + 1] == (byte)'>')
            {
                return index + 2;
            }
        }

        return -1;
    }

    private static int IndexAfterRunLength(byte[] data, int start)
    {
        var index = start;
        while (index < data.Length)
        {
            var run = data[index];
            if (run == RunLengthFilter.Eod)
            {
                return index + 1;
            }

            index += RunLengthFilter.PacketSpan(run);
        }

        return -1;
    }

    private static long InlineImagePayloadLength(InlineImage image)
    {
        if (image.Length >= 0)
        {
            return image.Length;
        }

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

        public string? FirstFilter { get; private set; }

        private string? colorSpace;

        private static string? FirstNameIn(string? raw)
        {
            var slash = raw is null || !raw.StartsWith('[') ? -1 : raw.IndexOf('/', System.StringComparison.Ordinal);
            if (slash < 0)
            {
                return null;
            }

            var end = slash + 1;
            while (end < raw!.Length && !IsWhitespace((byte)raw[end]) && !IsDelimiter((byte)raw[end]))
            {
                end++;
            }

            return raw[slash..end];
        }

        public void Set(string key, string? value, string? raw)
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
                    Filtered = value is null || value.StartsWith('/');
                    FirstFilter = value is not null && value.StartsWith('/') ? value : FirstNameIn(raw);
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

    private static string Latin1(byte[] data, int start, int length) => Encoding.Latin1.GetString(data, start, length);

    private static bool IsWhitespace(byte b) => Lexer.IsWhitespace(b);

    private static bool IsDelimiter(byte b) => Lexer.IsDelimiter(b);

    private static bool IsNumberStart(byte b) => Lexer.IsNumberStart(b);
}
