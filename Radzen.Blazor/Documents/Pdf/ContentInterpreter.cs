using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf;

#nullable enable

// Parses a page content stream back into ContentElement objects, folding the
// graphics-state (q/Q, cm) and text (Tm/Td) stacks into an absolute Transform per
// painted element. The inverse of the C3 emitter.
internal static class ContentInterpreter
{
    public static void Materialize(byte[] content, ContentCollection target)
    {
        var tokens = Tokenize(content);

        var state = new GraphicsState();
        var stack = new Stack<GraphicsState>();

        var textMatrix = Matrix.Identity;
        var lineMatrix = Matrix.Identity;
        var fontSize = 0.0;
        string? fontName = null;

        var currentX = 0.0;
        var currentY = 0.0;
        var startX = 0.0;
        var startY = 0.0;

        var pathOps = new List<PathOp>();
        var operands = new List<Token>();
        var stringBuffer = new List<byte>();
        var artifactDepth = 0;
        var markedContent = new Stack<bool>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token.Kind)
            {
                case TokenKind.Number:
                case TokenKind.Name:
                case TokenKind.String:
                    operands.Add(token);
                    continue;

                case TokenKind.ArrayStart:
                    stringBuffer.Clear();
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind == TokenKind.String)
                        {
                            stringBuffer.AddRange(tokens[i].Bytes!);
                        }
                    }

                    operands.Add(new Token(TokenKind.String, 0, null, [.. stringBuffer]));
                    continue;

                case TokenKind.DictStart:
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.DictEnd; i++)
                    {
                    }

                    continue;

                case TokenKind.ArrayEnd:
                case TokenKind.DictEnd:
                    continue;
            }

            switch (token.Text)
            {
                case "q":
                    stack.Push(state.Clone());
                    break;

                case "Q":
                    if (stack.Count > 0)
                    {
                        state = stack.Pop();
                    }

                    break;

                case "cm":
                    state.Ctm = Components(operands) * state.Ctm;
                    break;

                case "w":
                    state.LineWidth = LastNumber(operands);
                    break;

                case "rg":
                    state.Fill = Rgb(operands);
                    break;

                case "RG":
                    state.Stroke = Rgb(operands);
                    break;

                case "g":
                    state.Fill = Gray(operands);
                    break;

                case "G":
                    state.Stroke = Gray(operands);
                    break;

                case "BT":
                    textMatrix = Matrix.Identity;
                    lineMatrix = Matrix.Identity;
                    break;

                case "ET":
                    break;

                case "Tf":
                    fontName = LastName(operands);
                    fontSize = LastNumber(operands);
                    break;

                case "Td":
                case "TD":
                    lineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * lineMatrix;
                    textMatrix = lineMatrix;
                    break;

                case "Tm":
                    lineMatrix = Components(operands);
                    textMatrix = lineMatrix;
                    break;

                case "Tj":
                case "TJ":
                case "'":
                case "\"":
                    EmitText(target, operands, textMatrix, state, fontName, fontSize, artifactDepth);
                    break;

                case "m":
                {
                    var x = Number(operands, 0);
                    var y = Number(operands, 1);
                    pathOps.Add(new PathOp("m", [x, y]));
                    startX = currentX = x;
                    startY = currentY = y;
                    break;
                }

                case "l":
                    currentX = Number(operands, 0);
                    currentY = Number(operands, 1);
                    pathOps.Add(new PathOp("l", [currentX, currentY]));
                    break;

                case "c":
                {
                    var n = Numbers(operands, 6);
                    pathOps.Add(new PathOp("c", n));
                    currentX = n[4];
                    currentY = n[5];
                    break;
                }

                case "v":
                {
                    var n = Numbers(operands, 4);
                    pathOps.Add(new PathOp("c", [currentX, currentY, n[0], n[1], n[2], n[3]]));
                    currentX = n[2];
                    currentY = n[3];
                    break;
                }

                case "y":
                {
                    var n = Numbers(operands, 4);
                    pathOps.Add(new PathOp("c", [n[0], n[1], n[2], n[3], n[2], n[3]]));
                    currentX = n[2];
                    currentY = n[3];
                    break;
                }

                case "re":
                {
                    var n = Numbers(operands, 4);
                    pathOps.Add(new PathOp("m", [n[0], n[1]]));
                    pathOps.Add(new PathOp("l", [n[0] + n[2], n[1]]));
                    pathOps.Add(new PathOp("l", [n[0] + n[2], n[1] + n[3]]));
                    pathOps.Add(new PathOp("l", [n[0], n[1] + n[3]]));
                    pathOps.Add(new PathOp("h", []));
                    startX = currentX = n[0];
                    startY = currentY = n[1];
                    break;
                }

                case "h":
                    pathOps.Add(new PathOp("h", []));
                    currentX = startX;
                    currentY = startY;
                    break;

                case "Do":
                    if (LastName(operands) is { } xobject)
                    {
                        target.Add(new XObjectContent(xobject)
                        {
                            Transform = state.Ctm,
                            IsArtifact = artifactDepth > 0,
                        });
                    }

                    break;

                case "S":
                    EmitPath(target, pathOps, state, stroke: true, fill: false, close: false, artifactDepth);
                    break;

                case "s":
                    EmitPath(target, pathOps, state, stroke: true, fill: false, close: true, artifactDepth);
                    break;

                case "f":
                case "F":
                case "f*":
                    EmitPath(target, pathOps, state, stroke: false, fill: true, close: false, artifactDepth);
                    break;

                case "B":
                case "B*":
                    EmitPath(target, pathOps, state, stroke: true, fill: true, close: false, artifactDepth);
                    break;

                case "b":
                case "b*":
                    EmitPath(target, pathOps, state, stroke: true, fill: true, close: true, artifactDepth);
                    break;

                case "n":
                    pathOps.Clear();
                    break;

                case "BDC":
                case "BMC":
                {
                    var isArtifact = FirstName(operands) == "Artifact";
                    markedContent.Push(isArtifact);
                    if (isArtifact)
                    {
                        artifactDepth++;
                    }

                    break;
                }

                case "EMC":
                    if (markedContent.Count > 0 && markedContent.Pop())
                    {
                        artifactDepth--;
                    }

                    break;
            }

            operands.Clear();
        }
    }

    private static void EmitText(ContentCollection target, List<Token> operands, Matrix textMatrix, GraphicsState state, string? fontName, double fontSize, int artifactDepth)
    {
        var bytes = LastString(operands);
        if (bytes is null)
        {
            return;
        }

        var decoded = Decode(bytes);
        target.Add(new TextContent(decoded, 0, 0)
        {
            Font = new Font { Size = fontSize },
            FontResourceName = fontName,
            SourceBytes = bytes,
            SourceText = decoded,
            Color = state.Fill,
            Transform = textMatrix * state.Ctm,
            IsArtifact = artifactDepth > 0,
        });
    }

    private static void EmitPath(ContentCollection target, List<PathOp> pathOps, GraphicsState state, bool stroke, bool fill, bool close, int artifactDepth)
    {
        var path = new PathContent
        {
            Stroke = stroke,
            Fill = fill,
            Thickness = state.LineWidth,
            StrokeColor = state.Stroke,
            FillColor = state.Fill,
            Transform = state.Ctm,
            IsArtifact = artifactDepth > 0,
        };

        foreach (var op in pathOps)
        {
            switch (op.Operator)
            {
                case "m":
                    path.MoveTo(op.Operands[0], op.Operands[1]);
                    break;
                case "l":
                    path.LineTo(op.Operands[0], op.Operands[1]);
                    break;
                case "c":
                    path.CurveTo(op.Operands[0], op.Operands[1], op.Operands[2], op.Operands[3], op.Operands[4], op.Operands[5]);
                    break;
                case "h":
                    path.Close();
                    break;
            }
        }

        if (close)
        {
            path.Close();
        }

        target.Add(path);
        pathOps.Clear();
    }

    private static Matrix Components(List<Token> operands)
    {
        var n = Numbers(operands, 6);
        return Matrix.FromComponents(n[0], n[1], n[2], n[3], n[4], n[5]);
    }

    private static Color Rgb(List<Token> operands)
    {
        var n = Numbers(operands, 3);
        return Color.FromRgb(Channel(n[0]), Channel(n[1]), Channel(n[2]));
    }

    private static Color Gray(List<Token> operands)
    {
        var v = Channel(Number(operands, 0));
        return Color.FromRgb(v, v, v);
    }

    private static byte Channel(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255.0);

    private static double[] Numbers(List<Token> operands, int count)
    {
        var result = new double[count];
        var numbers = new List<double>(count);
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Number)
            {
                numbers.Add(token.Number);
            }
        }

        var offset = numbers.Count - count;
        for (var i = 0; i < count; i++)
        {
            var index = offset + i;
            result[i] = index >= 0 && index < numbers.Count ? numbers[index] : 0.0;
        }

        return result;
    }

    private static double Number(List<Token> operands, int index)
    {
        var numbers = new List<double>();
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Number)
            {
                numbers.Add(token.Number);
            }
        }

        return index < numbers.Count ? numbers[index] : 0.0;
    }

    private static double LastNumber(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Number)
            {
                return operands[i].Number;
            }
        }

        return 0.0;
    }

    private static string? LastName(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Name)
            {
                return operands[i].Text;
            }
        }

        return null;
    }

    // The BDC/BMC tag is the first name operand; the optional property list may itself
    // be a name, so LastName would misread it.
    private static string? FirstName(List<Token> operands)
    {
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Name)
            {
                return token.Text;
            }
        }

        return null;
    }

    private static byte[]? LastString(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.String)
            {
                return operands[i].Bytes;
            }
        }

        return null;
    }

    private static string Decode(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            if (WinAnsiEncoding.TryGetChar(b, out var c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static List<Token> Tokenize(byte[] data)
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
                    tokens.Add(new Token(TokenKind.ArrayStart, 0, null, null));
                    position++;
                    continue;

                case (byte)']':
                    tokens.Add(new Token(TokenKind.ArrayEnd, 0, null, null));
                    position++;
                    continue;

                case (byte)'(':
                    tokens.Add(new Token(TokenKind.String, 0, null, ReadLiteralString(data, ref position)));
                    continue;

                case (byte)'/':
                    tokens.Add(new Token(TokenKind.Name, 0, ReadName(data, ref position), null));
                    continue;

                case (byte)'<':
                    if (position + 1 < data.Length && data[position + 1] == '<')
                    {
                        tokens.Add(new Token(TokenKind.DictStart, 0, null, null));
                        position += 2;
                        continue;
                    }

                    tokens.Add(new Token(TokenKind.String, 0, null, ReadHexString(data, ref position)));
                    continue;

                case (byte)'>':
                    if (position + 1 < data.Length && data[position + 1] == '>')
                    {
                        tokens.Add(new Token(TokenKind.DictEnd, 0, null, null));
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

                var text = Latin1(data, start, position - start);
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    tokens.Add(new Token(TokenKind.Number, value, null, null));
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
                continue;
            }

            tokens.Add(new Token(TokenKind.Operator, 0, keyword, null));
        }

        return tokens;
    }

    // After a BI operator, skip the inline-image dict and its binary payload, resuming
    // past the whitespace-delimited EI so the binary is not lexed as operators. EI may
    // occur inside the binary, so it only terminates when bounded by whitespace/EOF.
    private static void SkipInlineImage(byte[] data, ref int position)
    {
        while (position < data.Length)
        {
            if (data[position] == (byte)'I' && position + 1 < data.Length && data[position + 1] == (byte)'D'
                && (position == 0 || IsWhitespace(data[position - 1]) || IsDelimiter(data[position - 1]))
                && (position + 2 >= data.Length || IsWhitespace(data[position + 2]) || IsDelimiter(data[position + 2])))
            {
                position += 2;
                break;
            }

            position++;
        }

        if (position < data.Length && IsWhitespace(data[position]))
        {
            position++;
        }

        while (position < data.Length)
        {
            if (IsWhitespace(data[position]) && position + 2 < data.Length
                && data[position + 1] == (byte)'E' && data[position + 2] == (byte)'I'
                && (position + 3 >= data.Length || IsWhitespace(data[position + 3]) || IsDelimiter(data[position + 3])))
            {
                position += 3;
                return;
            }

            position++;
        }
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

    private static string Latin1(byte[] data, int start, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)data[start + i];
        }

        return new string(chars);
    }

    private static bool IsWhitespace(byte b) => b is 0 or 9 or 10 or 12 or 13 or 32;

    private static bool IsDelimiter(byte b) => b is (byte)'(' or (byte)')' or (byte)'<' or (byte)'>'
        or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or (byte)'/' or (byte)'%';

    private static bool IsNumberStart(byte b) => b is (byte)'+' or (byte)'-' or (byte)'.' or (>= (byte)'0' and <= (byte)'9');

    private static bool IsNumberChar(byte b) => b is (byte)'+' or (byte)'-' or (byte)'.' or (byte)'e' or (byte)'E' or (>= (byte)'0' and <= (byte)'9');

    private enum TokenKind
    {
        Number,
        Name,
        String,
        ArrayStart,
        ArrayEnd,
        DictStart,
        DictEnd,
        Operator,
    }

    private readonly record struct Token(TokenKind Kind, double Number, string? Text, byte[]? Bytes);

    private readonly record struct PathOp(string Operator, double[] Operands);

    private sealed class GraphicsState
    {
        public Matrix Ctm { get; set; } = Matrix.Identity;

        public Color Fill { get; set; } = Color.Black;

        public Color Stroke { get; set; } = Color.Black;

        public double LineWidth { get; set; } = 1;

        public GraphicsState Clone() => new()
        {
            Ctm = Ctm,
            Fill = Fill,
            Stroke = Stroke,
            LineWidth = LineWidth,
        };
    }
}
