using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;


// Parses a page content stream back into ContentElement objects, folding the
// graphics-state (q/Q, cm) and text (Tm/Td) stacks into an absolute Transform per
// painted element. The inverse of the C3 emitter.
internal static class ContentInterpreter
{
    public static void Materialize(byte[] content, ContentCollection target, IReadOnlyDictionary<string, ReverseFont>? fonts = null)
    {
        var tokens = ContentTokenizer.Tokenize(content);
        var interpreter = new InterpreterState();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token.Kind)
            {
                case TokenKind.Number:
                case TokenKind.Name:
                case TokenKind.String:
                    interpreter.Operands.Add(token);
                    continue;
                case TokenKind.ArrayStart:
                    interpreter.StringBuffer.Clear();
                    interpreter.ArrayNumbers.Clear();
                    interpreter.TjSegments = [];
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind == TokenKind.String)
                        {
                            interpreter.StringBuffer.AddRange(tokens[i].Bytes!);
                            interpreter.TjSegments.Add(new TextAdjustment(tokens[i].Bytes!, 0));
                        }
                        else if (tokens[i].Kind == TokenKind.Number)
                        {
                            interpreter.ArrayNumbers.Add(tokens[i].Number);
                            interpreter.TjSegments.Add(new TextAdjustment(null, tokens[i].Number));
                        }
                    }

                    // An array left unterminated by a truncated stream consumes every remaining
                    // token, so the span ends at the last one rather than at the missing ']'.
                    var end = i < tokens.Count ? tokens[i].End : tokens[^1].End;
                    interpreter.Operands.Add(new Token(TokenKind.String, 0, null, [.. interpreter.StringBuffer], token.Start, end));
                    continue;
                case TokenKind.DictStart:
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.DictEnd; i++)
                    {
                    }

                    continue;
                case TokenKind.ArrayEnd:
                case TokenKind.DictEnd:
                    continue;
                case TokenKind.InlineImage:
                    target.Add(new InlineImageContent(token.Bytes!)
                    {
                        Transform = interpreter.Graphics.Ctm,
                        IsArtifact = interpreter.ArtifactDepth > 0,
                    });
                    interpreter.PendingMerge = null;
                    interpreter.ResetOperandFrame();
                    continue;
            }

            var op = token.Text;
            if (!HandleGraphicsOperators(op, interpreter, target)
                && !HandleTextOperators(op, interpreter, target, fonts)
                && !HandlePathOperators(op, interpreter, target)
                && !HandleMarkedContentOperators(op, interpreter))
            {
                HandlePassthroughOperator(op, interpreter, target);
            }

            if (op is not ("Tj" or "TJ" or "'" or "\""))
            {
                interpreter.PendingMerge = null;
            }

            interpreter.ResetOperandFrame();
        }
    }

    private static bool HandleGraphicsOperators(string? op, InterpreterState interpreter, ContentCollection target)
    {
        var operands = interpreter.Operands;
        ref var state = ref interpreter.Graphics;
        switch (op)
        {
            case "q":
                interpreter.Stack.Push(state.Clone());
                break;

            case "Q":
                if (interpreter.Stack.Count > 0)
                {
                    state = interpreter.Stack.Pop();
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
                state.FillPaint = new DeviceColor(DeviceColorKind.Named, state.FillColorSpace, AllNumbers(operands), LastName(operands));
                break;

            case "SCN":
            case "SC":
                state.StrokePaint = new DeviceColor(DeviceColorKind.Named, state.StrokeColorSpace, AllNumbers(operands), LastName(operands));
                break;

            case "d":
                state.DashArray = [.. interpreter.ArrayNumbers];
                state.DashPhase = LastNumber(operands);
                break;

            case "Do":
                if (LastName(operands) is { } xobject)
                {
                    target.Add(new XObjectContent(xobject)
                    {
                        Transform = state.Ctm,
                        IsArtifact = interpreter.ArtifactDepth > 0,
                    });
                }

                break;

            default:
                return false;
        }

        return true;
    }

    private static bool HandleTextOperators(string? op, InterpreterState interpreter, ContentCollection target, IReadOnlyDictionary<string, ReverseFont>? reverseFonts)
    {
        var operands = interpreter.Operands;
        ref var state = ref interpreter.Graphics;
        switch (op)
        {
            case "BT":
                interpreter.TextMatrix = Matrix.Identity;
                interpreter.LineMatrix = Matrix.Identity;
                break;

            case "ET":
                break;

            case "Tf":
                interpreter.FontName = LastName(operands);
                interpreter.FontSize = LastNumber(operands);
                interpreter.Font = interpreter.FontName is not null && reverseFonts is not null && reverseFonts.TryGetValue(interpreter.FontName, out var resolved)
                    ? resolved
                    : null;
                break;

            case "TL":
                interpreter.Leading = LastNumber(operands);
                break;

            case "TD":
                interpreter.Leading = -Number(operands, 1);
                goto case "Td";

            case "Td":
                interpreter.LineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * interpreter.LineMatrix;
                interpreter.TextMatrix = interpreter.LineMatrix;
                break;

            case "Tm":
                interpreter.LineMatrix = Components(operands);
                interpreter.TextMatrix = interpreter.LineMatrix;
                break;

            case "T*":
                interpreter.LineMatrix = Matrix.Translate(0, -interpreter.Leading) * interpreter.LineMatrix;
                interpreter.TextMatrix = interpreter.LineMatrix;
                break;

            case "Tj":
            case "TJ":
                interpreter.PendingMerge = EmitText(target, operands, interpreter.TextMatrix, state, new TextState(interpreter.FontName, interpreter.FontSize, interpreter.Font, 0, 0), interpreter.ArtifactDepth, interpreter.TjSegments, interpreter.PendingMerge);
                break;

            // ' advances to the next line by the interpreter.Leading before showing.
            case "'":
                interpreter.LineMatrix = Matrix.Translate(0, -interpreter.Leading) * interpreter.LineMatrix;
                interpreter.TextMatrix = interpreter.LineMatrix;
                interpreter.PendingMerge = EmitText(target, operands, interpreter.TextMatrix, state, new TextState(interpreter.FontName, interpreter.FontSize, interpreter.Font, 0, 0), interpreter.ArtifactDepth, interpreter.TjSegments, interpreter.PendingMerge);
                break;

            // " advances the line then shows, and additionally sets word spacing (aw)
            // and character spacing (ac) from its first two operands.
            case "\"":
                interpreter.LineMatrix = Matrix.Translate(0, -interpreter.Leading) * interpreter.LineMatrix;
                interpreter.TextMatrix = interpreter.LineMatrix;
                interpreter.PendingMerge = EmitText(target, operands, interpreter.TextMatrix, state, new TextState(interpreter.FontName, interpreter.FontSize, interpreter.Font, Number(operands, 0), Number(operands, 1)), interpreter.ArtifactDepth, interpreter.TjSegments, interpreter.PendingMerge);
                break;

            default:
                return false;
        }

        return true;
    }

    private static bool HandlePathOperators(string? op, InterpreterState interpreter, ContentCollection target)
    {
        var operands = interpreter.Operands;
        ref var state = ref interpreter.Graphics;
        switch (op)
        {
            case "W":
                interpreter.ClipMode = PathClipMode.NonZero;
                break;

            case "W*":
                interpreter.ClipMode = PathClipMode.EvenOdd;
                break;

            case "m":
                {
                    var x = Number(operands, 0);
                    var y = Number(operands, 1);
                    interpreter.PathOps.Add(new PathOp("m", [x, y]));
                    interpreter.StartX = interpreter.CurrentX = x;
                    interpreter.StartY = interpreter.CurrentY = y;
                    break;
                }

            case "l":
                interpreter.CurrentX = Number(operands, 0);
                interpreter.CurrentY = Number(operands, 1);
                interpreter.PathOps.Add(new PathOp("l", [interpreter.CurrentX, interpreter.CurrentY]));
                break;

            case "c":
                {
                    var n = Numbers(operands, 6);
                    interpreter.PathOps.Add(new PathOp("c", n));
                    interpreter.CurrentX = n[4];
                    interpreter.CurrentY = n[5];
                    break;
                }

            case "v":
                {
                    var n = Numbers(operands, 4);
                    interpreter.PathOps.Add(new PathOp("c", [interpreter.CurrentX, interpreter.CurrentY, n[0], n[1], n[2], n[3]]));
                    interpreter.CurrentX = n[2];
                    interpreter.CurrentY = n[3];
                    break;
                }

            case "y":
                {
                    var n = Numbers(operands, 4);
                    interpreter.PathOps.Add(new PathOp("c", [n[0], n[1], n[2], n[3], n[2], n[3]]));
                    interpreter.CurrentX = n[2];
                    interpreter.CurrentY = n[3];
                    break;
                }

            case "re":
                {
                    var n = Numbers(operands, 4);
                    interpreter.PathOps.Add(new PathOp("m", [n[0], n[1]]));
                    interpreter.PathOps.Add(new PathOp("l", [n[0] + n[2], n[1]]));
                    interpreter.PathOps.Add(new PathOp("l", [n[0] + n[2], n[1] + n[3]]));
                    interpreter.PathOps.Add(new PathOp("l", [n[0], n[1] + n[3]]));
                    interpreter.PathOps.Add(new PathOp("h", []));
                    interpreter.StartX = interpreter.CurrentX = n[0];
                    interpreter.StartY = interpreter.CurrentY = n[1];
                    break;
                }

            case "h":
                interpreter.PathOps.Add(new PathOp("h", []));
                interpreter.CurrentX = interpreter.StartX;
                interpreter.CurrentY = interpreter.StartY;
                break;

            case "S":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: true, Fill: false, Close: false, EvenOdd: false), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "s":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: true, Fill: false, Close: true, EvenOdd: false), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "f":
            case "F":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: false, Fill: true, Close: false, EvenOdd: false), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "f*":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: false, Fill: true, Close: false, EvenOdd: true), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "B":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: true, Fill: true, Close: false, EvenOdd: false), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "B*":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: true, Fill: true, Close: false, EvenOdd: true), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "b":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: true, Fill: true, Close: true, EvenOdd: false), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "b*":
                EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: true, Fill: true, Close: true, EvenOdd: true), interpreter.ClipMode, interpreter.ArtifactDepth);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "n":
                if (interpreter.ClipMode != PathClipMode.None)
                {
                    EmitPath(target, interpreter.PathOps, state, new PathPaint(Stroke: false, Fill: false, Close: false, EvenOdd: false), interpreter.ClipMode, interpreter.ArtifactDepth);
                }

                interpreter.PathOps.Clear();
                interpreter.ClipMode = PathClipMode.None;
                break;

            default:
                return false;
        }

        return true;
    }

    private static bool HandleMarkedContentOperators(string? op, InterpreterState interpreter)
    {
        var operands = interpreter.Operands;
        switch (op)
        {
            case "BDC":
            case "BMC":
                {
                    var isArtifact = FirstName(operands) == "Artifact";
                    interpreter.MarkedContent.Push(isArtifact);
                    if (isArtifact)
                    {
                        interpreter.ArtifactDepth++;
                    }

                    break;
                }

            case "EMC":
                if (interpreter.MarkedContent.Count > 0 && interpreter.MarkedContent.Pop())
                {
                    interpreter.ArtifactDepth--;
                }

                break;

            default:
                return false;
        }

        return true;
    }

    private static void HandlePassthroughOperator(string? op, InterpreterState interpreter, ContentCollection target)
    {
        if (op is not null && !IsTextState(op))
        {
            target.Add(new RawContent(op, [.. interpreter.Operands])
            {
                IsArtifact = interpreter.ArtifactDepth > 0,
                ClipBounds = interpreter.Graphics.Clip,
            });
        }
    }

    private sealed class InterpreterState
    {
        public GraphicsState Graphics = new();
        public Stack<GraphicsState> Stack { get; } = new();
        public Matrix TextMatrix { get; set; } = Matrix.Identity;
        public Matrix LineMatrix { get; set; } = Matrix.Identity;
        public double FontSize { get; set; }
        public double Leading { get; set; }
        public string? FontName { get; set; }
        public ReverseFont? Font { get; set; }
        public double CurrentX { get; set; }
        public double CurrentY { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public List<PathOp> PathOps { get; } = [];
        public List<Token> Operands { get; } = [];
        public List<byte> StringBuffer { get; } = [];
        public List<double> ArrayNumbers { get; } = [];
        public List<TextAdjustment>? TjSegments { get; set; }
        public PathClipMode ClipMode { get; set; }
        public int ArtifactDepth { get; set; }
        public Stack<bool> MarkedContent { get; } = new();
        public TextContent? PendingMerge { get; set; }

        public void ResetOperandFrame()
        {
            Operands.Clear();
            ArrayNumbers.Clear();
            TjSegments = null;
        }
    }


    private static TextContent? EmitText(ContentCollection target, List<Token> operands, Matrix textMatrix, GraphicsState state, TextState text, int artifactDepth, List<TextAdjustment>? tjSegments, TextContent? pendingMerge)
    {
        var bytes = LastString(operands);
        if (bytes is null)
        {
            return null;
        }

        // A loaded run in an embedded/Type0 font carries multi-byte codes; decode Text via
        // the font's reverse map (as text extraction does) instead of per-byte WinAnsi,
        // which drops the 0x00 high bytes. SourceBytes still re-emits the run verbatim.
        var decoded = text.Font is not null ? text.Font.Decode(bytes) : Decode(bytes);
        var transform = textMatrix * state.Ctm;

        // The spec advances the text matrix by each shown glyph's width, but the element
        // model carries no per-run width. Rather than collapse a following show onto the
        // same origin, fold consecutive shows that share the text matrix and text state
        // into one run: a single combined show operator lets the renderer advance between
        // the chunks using the font's own widths.
        if (pendingMerge is { SourceBytes: not null, SourceText: not null }
            && pendingMerge.Transform == transform
            && pendingMerge.FontResourceName == text.FontName
            && pendingMerge.Font.Size == text.FontSize
            && pendingMerge.Color == state.Fill
            && pendingMerge.FillPaint == state.FillPaint
            && pendingMerge.WordSpacing == text.WordSpacing
            && pendingMerge.CharSpacing == text.CharSpacing
            && pendingMerge.IsArtifact == (artifactDepth > 0))
        {
            var segments = new List<TextAdjustment>(
                pendingMerge.SourceAdjustments ?? [new TextAdjustment(pendingMerge.SourceBytes, 0)]);
            segments.AddRange(tjSegments ?? [new TextAdjustment(bytes, 0)]);
            var combinedText = pendingMerge.SourceText + decoded;
            pendingMerge.SourceAdjustments = segments;
            pendingMerge.SourceBytes = Concat(pendingMerge.SourceBytes, bytes);
            pendingMerge.Text = combinedText;
            pendingMerge.SourceText = combinedText;
            return pendingMerge;
        }

        var run = new TextContent(decoded, 0, 0)
        {
            Font = new Font { Size = text.FontSize },
            FontResourceName = text.FontName,
            SourceBytes = bytes,
            SourceText = decoded,
            SourceFont = text.Font,
            // Only carry the TJ array when it holds a numeric adjustment; a plain string
            // (Tj or a single-element TJ) re-emits through the simpler Tj path unchanged.
            SourceAdjustments = HasAdjustment(tjSegments) ? tjSegments : null,
            Color = state.Fill,
            FillPaint = state.FillPaint,
            WordSpacing = text.WordSpacing,
            CharSpacing = text.CharSpacing,
            Transform = transform,
            IsArtifact = artifactDepth > 0,
        };
        target.Add(run);
        return run;
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }

    private static bool HasAdjustment(List<TextAdjustment>? segments)
    {
        if (segments is null)
        {
            return false;
        }

        foreach (var segment in segments)
        {
            if (segment.Text is null && segment.Adjustment != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void EmitPath(ContentCollection target, List<PathOp> pathOps, GraphicsState state, PathPaint paint, PathClipMode clip, int artifactDepth)
    {
        var path = new PathContent
        {
            Stroke = paint.Stroke,
            Fill = paint.Fill,
            EvenOdd = paint.EvenOdd,
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

        if (paint.Close)
        {
            path.Close();
        }

        if (clip != PathClipMode.None)
        {
            state.Clip = Intersect(state.Clip, path.GetBounds());
        }

        target.Add(path);
        pathOps.Clear();
    }

    // An empty clipping path bounds nothing, so it keeps whatever the enclosing clip was
    // rather than widening it back to unbounded.
    private static TextBounds? Intersect(TextBounds? current, TextBounds? added)
    {
        if (added is not { } bounds)
        {
            return current;
        }

        if (current is not { } existing)
        {
            return bounds;
        }

        return new TextBounds(
            Math.Max(existing.Left, bounds.Left),
            Math.Max(existing.Bottom, bounds.Bottom),
            Math.Min(existing.Right, bounds.Right),
            Math.Min(existing.Top, bounds.Top));
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

    // The text-show state a show operator carries: the selected font resource and size,
    // its resolved reverse map, and the word/character spacing an inline " sets.
    private readonly record struct TextState(string? FontName, double FontSize, ReverseFont? Font, double WordSpacing, double CharSpacing);

    // The paint disposition of a path operator: stroke/fill, whether it closes the last
    // subpath, and even-odd vs nonzero fill.
    private readonly record struct PathPaint(bool Stroke, bool Fill, bool Close, bool EvenOdd);

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

        // A bounding box the current clipping path is known to fit inside, or null while no
        // clip is active. Only ever a superset of the real region, so it can bound an
        // operator that paints the whole clip (sh) without under-reporting its extent.
        public TextBounds? Clip { get; set; }

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
            Clip = Clip,
        };
    }
}

// One element of a TJ show array: a string chunk (Text set) or a numeric position
// adjustment in thousandths of an em (Text null). Preserved so a re-emitted run keeps
// its intra-run displacements (kerning, inter-word gaps) instead of collapsing to Tj.
internal readonly record struct TextAdjustment(byte[]? Text, double Adjustment);

// An operator with no element-model representation, captured verbatim from the decoded
// token stream and re-emitted unchanged so a full re-encode does not silently drop it.
internal sealed class RawContent(string op, IReadOnlyList<Token> operands) : ContentElement
{
    public string Operator => op;

    // The clip in effect where the operator appeared, or null if unclipped. An operator
    // that paints without a shape of its own (sh) can only be bounded by this.
    public TextBounds? ClipBounds { get; init; }

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
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

// A BI/ID/EI inline image, captured verbatim from the source bytes. Its payload is opaque
// binary that the content grammar cannot rewrite, so it is only ever re-emitted unchanged
// or dropped whole; Transform carries the CTM that maps its unit square onto the page,
// which is what makes it bounds-testable like any other painted element.
internal sealed class InlineImageContent(byte[] source) : ContentElement
{
    public byte[] Source => source;

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
    {
        writer.WriteBytes(source);
        writer.WriteRaw("\n");
    }
}
