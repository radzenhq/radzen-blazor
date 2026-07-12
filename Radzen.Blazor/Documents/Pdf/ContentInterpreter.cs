using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using Token = Radzen.Documents.Pdf.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf;


// Parses a page content stream back into ContentElement objects, folding the
// graphics-state (q/Q, cm) and text (Tm/Td) stacks into an absolute Transform per
// painted element. The inverse of the C3 emitter.
internal static class ContentInterpreter
{
    public static void Materialize(byte[] content, ContentCollection target, IReadOnlyDictionary<string, ReverseFont>? fonts = null)
    {
        var tokens = ContentTokenizer.Tokenize(content);

        var state = new GraphicsState();
        var stack = new Stack<GraphicsState>();

        var textMatrix = Matrix.Identity;
        var lineMatrix = Matrix.Identity;
        var fontSize = 0.0;
        var leading = 0.0;
        string? fontName = null;
        ReverseFont? font = null;

        var currentX = 0.0;
        var currentY = 0.0;
        var startX = 0.0;
        var startY = 0.0;

        var pathOps = new List<PathOp>();
        var operands = new List<Token>();
        var stringBuffer = new List<byte>();
        var arrayNumbers = new List<double>();
        var clipMode = PathClipMode.None;
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
                    arrayNumbers.Clear();
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind == TokenKind.String)
                        {
                            stringBuffer.AddRange(tokens[i].Bytes!);
                        }
                        else if (tokens[i].Kind == TokenKind.Number)
                        {
                            arrayNumbers.Add(tokens[i].Number);
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
                    state.FillPaint = null;
                    break;

                case "RG":
                    state.Stroke = Rgb(operands);
                    state.StrokePaint = null;
                    break;

                case "g":
                    state.Fill = Gray(operands);
                    state.FillPaint = null;
                    break;

                case "G":
                    state.Stroke = Gray(operands);
                    state.StrokePaint = null;
                    break;

                case "k":
                    state.FillPaint = new DeviceColor(DeviceColorKind.Cmyk, null, Numbers(operands, 4));
                    break;

                case "K":
                    state.StrokePaint = new DeviceColor(DeviceColorKind.Cmyk, null, Numbers(operands, 4));
                    break;

                case "cs":
                    state.FillColorSpace = LastName(operands);
                    break;

                case "CS":
                    state.StrokeColorSpace = LastName(operands);
                    break;

                case "scn":
                case "sc":
                    state.FillPaint = new DeviceColor(DeviceColorKind.Named, state.FillColorSpace, AllNumbers(operands));
                    break;

                case "SCN":
                case "SC":
                    state.StrokePaint = new DeviceColor(DeviceColorKind.Named, state.StrokeColorSpace, AllNumbers(operands));
                    break;

                case "d":
                    state.DashArray = [.. arrayNumbers];
                    state.DashPhase = LastNumber(operands);
                    break;

                case "W":
                    clipMode = PathClipMode.NonZero;
                    break;

                case "W*":
                    clipMode = PathClipMode.EvenOdd;
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
                    font = fontName is not null && fonts is not null && fonts.TryGetValue(fontName, out var resolved)
                        ? resolved
                        : null;
                    break;

                case "TL":
                    leading = LastNumber(operands);
                    break;

                case "TD":
                    leading = -Number(operands, 1);
                    goto case "Td";

                case "Td":
                    lineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * lineMatrix;
                    textMatrix = lineMatrix;
                    break;

                case "Tm":
                    lineMatrix = Components(operands);
                    textMatrix = lineMatrix;
                    break;

                case "T*":
                    lineMatrix = Matrix.Translate(0, -leading) * lineMatrix;
                    textMatrix = lineMatrix;
                    break;

                case "Tj":
                case "TJ":
                    EmitText(target, operands, textMatrix, state, fontName, fontSize, artifactDepth, font);
                    break;

                // ' and " advance to the next line by the leading before showing.
                case "'":
                case "\"":
                    lineMatrix = Matrix.Translate(0, -leading) * lineMatrix;
                    textMatrix = lineMatrix;
                    EmitText(target, operands, textMatrix, state, fontName, fontSize, artifactDepth, font);
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
                    EmitPath(target, pathOps, state, stroke: true, fill: false, close: false, evenOdd: false, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "s":
                    EmitPath(target, pathOps, state, stroke: true, fill: false, close: true, evenOdd: false, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "f":
                case "F":
                    EmitPath(target, pathOps, state, stroke: false, fill: true, close: false, evenOdd: false, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "f*":
                    EmitPath(target, pathOps, state, stroke: false, fill: true, close: false, evenOdd: true, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "B":
                    EmitPath(target, pathOps, state, stroke: true, fill: true, close: false, evenOdd: false, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "B*":
                    EmitPath(target, pathOps, state, stroke: true, fill: true, close: false, evenOdd: true, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "b":
                    EmitPath(target, pathOps, state, stroke: true, fill: true, close: true, evenOdd: false, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "b*":
                    EmitPath(target, pathOps, state, stroke: true, fill: true, close: true, evenOdd: true, clipMode, artifactDepth);
                    clipMode = PathClipMode.None;
                    break;

                case "n":
                    if (clipMode != PathClipMode.None)
                    {
                        EmitPath(target, pathOps, state, stroke: false, fill: false, close: false, evenOdd: false, clipMode, artifactDepth);
                    }

                    pathOps.Clear();
                    clipMode = PathClipMode.None;
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

                default:
                    // Any operator the element model does not represent (gs, sh, J, j, M,
                    // ri, i, ...) is carried through verbatim so a re-encode stays lossless.
                    // Text-state operators are excluded: a materialized run re-emits its own
                    // isolated BT/ET, so a stray Tr/Tc/Tz here would leak into unrelated text.
                    if (token.Text is not null && !IsTextState(token.Text))
                    {
                        target.Add(new RawContent(token.Text, [.. operands]) { IsArtifact = artifactDepth > 0 });
                    }

                    break;
            }

            operands.Clear();
        }
    }

    private static void EmitText(ContentCollection target, List<Token> operands, Matrix textMatrix, GraphicsState state, string? fontName, double fontSize, int artifactDepth, ReverseFont? font)
    {
        var bytes = LastString(operands);
        if (bytes is null)
        {
            return;
        }

        // A loaded run in an embedded/Type0 font carries multi-byte codes; decode Text via
        // the font's reverse map (as text extraction does) instead of per-byte WinAnsi,
        // which drops the 0x00 high bytes. SourceBytes still re-emits the run verbatim.
        var decoded = font is not null ? font.Decode(bytes) : Decode(bytes);
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

    private static void EmitPath(ContentCollection target, List<PathOp> pathOps, GraphicsState state, bool stroke, bool fill, bool close, bool evenOdd, PathClipMode clip, int artifactDepth)
    {
        var path = new PathContent
        {
            Stroke = stroke,
            Fill = fill,
            EvenOdd = evenOdd,
            Clip = clip,
            Thickness = state.LineWidth,
            StrokeColor = state.Stroke,
            FillColor = state.Fill,
            StrokePaint = state.StrokePaint,
            FillPaint = state.FillPaint,
            DashArray = state.DashArray,
            DashPhase = state.DashPhase,
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

    private static double[] AllNumbers(List<Token> operands)
    {
        var numbers = new List<double>();
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Number)
            {
                numbers.Add(token.Number);
            }
        }

        return [.. numbers];
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

    // Text-state operators (char/word spacing, horizontal scale, leading, rise, render
    // mode) set state that persists across BT/ET. The model re-emits each run in its own
    // isolated BT/ET, so passing these through as standalone elements would misapply them.
    private static bool IsTextState(string op) => op is "Tc" or "Tw" or "Tz" or "TL" or "Ts" or "Tr";

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

    private readonly record struct PathOp(string Operator, double[] Operands);

    private sealed class GraphicsState
    {
        public Matrix Ctm { get; set; } = Matrix.Identity;

        public Color Fill { get; set; } = Color.Black;

        public Color Stroke { get; set; } = Color.Black;

        public double LineWidth { get; set; } = 1;

        public DeviceColor? FillPaint { get; set; }

        public DeviceColor? StrokePaint { get; set; }

        public string? FillColorSpace { get; set; }

        public string? StrokeColorSpace { get; set; }

        public double[]? DashArray { get; set; }

        public double DashPhase { get; set; }

        public GraphicsState Clone() => new()
        {
            Ctm = Ctm,
            Fill = Fill,
            Stroke = Stroke,
            LineWidth = LineWidth,
            FillPaint = FillPaint,
            StrokePaint = StrokePaint,
            FillColorSpace = FillColorSpace,
            StrokeColorSpace = StrokeColorSpace,
            DashArray = DashArray,
            DashPhase = DashPhase,
        };
    }
}

// An operator with no element-model representation, captured verbatim from the decoded
// token stream and re-emitted unchanged so a full re-encode does not silently drop it.
internal sealed class RawContent(string op, IReadOnlyList<Token> operands) : ContentElement
{
    internal override void EmitBody(ContentWriter writer)
    {
        foreach (var operand in operands)
        {
            switch (operand.Kind)
            {
                case TokenKind.Number:
                    writer.WriteNumber(operand.Number);
                    writer.WriteRaw(" ");
                    break;
                case TokenKind.Name:
                    writer.WriteName(operand.Text!);
                    writer.WriteRaw(" ");
                    break;
                case TokenKind.String when operand.Bytes is not null:
                    writer.WriteString(operand.Bytes);
                    writer.WriteRaw(" ");
                    break;
            }
        }

        writer.WriteRaw(op);
        writer.WriteRaw("\n");
    }
}
