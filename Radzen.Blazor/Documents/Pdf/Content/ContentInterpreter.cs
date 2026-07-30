using Radzen.Documents.Fonts;
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;


internal static class ContentInterpreter
{
    public static IReadOnlyList<ContentEditor.SourceElement> Materialize(byte[] content, ContentCollection target, IReadOnlyDictionary<string, ReverseFont>? fonts = null, ContentTokenizer.Cache? cache = null)
    {
        var tokens = ContentTokenizer.Tokenize(content, cache);
        var interpreter = new InterpreterState(fonts);

        foreach (var frame in ContentOperandScan.Scan(tokens))
        {
            if (frame.IsInlineImage)
            {
                if (interpreter.PathStart < 0)
                {
                    var inline = new InlineImageContent(frame.InlineImage.Bytes!)
                    {
                        Transform = interpreter.Graphics.Ctm,
                        IsArtifact = interpreter.ArtifactDepth > 0,
                    };
                    target.Add(inline);
                    interpreter.AddSpan(inline, frame.InlineImage.Start, frame.InlineImage.End, interpreter.Graphics.Ctm, false);
                }

                FinalizeMerge(interpreter);
                interpreter.ResetOperandFrame();
                continue;
            }

            LoadOperands(interpreter, frame);

            var op = frame.Operator.Text;
            interpreter.FrameStart = frame.FrameStart < 0 ? frame.Operator.Start : frame.FrameStart;
            interpreter.OperatorEnd = frame.Operator.End;

            if (!interpreter.Machine.Apply(op, interpreter.Operands)
                && !HandleGraphicsOperators(op, interpreter, target)
                && !HandleTextOperators(op, interpreter, target)
                && !HandlePathOperators(op, interpreter, target)
                && !HandleMarkedContentOperators(op, interpreter)
                && ContentOperatorClass.IsUnknown(op))
            {
                HandlePassthroughOperator(op, interpreter, target);
            }

            if (!ContentShows.IsShow(op))
            {
                FinalizeMerge(interpreter);
            }

            interpreter.ResetOperandFrame();
        }

        FinalizeMerge(interpreter);
        return interpreter.Spans;
    }

    private static void LoadOperands(InterpreterState interpreter, ContentOperandFrame frame)
    {
        interpreter.Operands.Clear();
        interpreter.ArrayNumbers.Clear();
        interpreter.TjSegments = null;
        interpreter.Operands.AddRange(frame.Operands);

        if (!frame.HasArray)
        {
            return;
        }

        interpreter.StringBuffer.Clear();
        interpreter.TjSegments = [];
        foreach (var item in frame.Array)
        {
            if (item.Kind == TokenKind.String)
            {
                interpreter.StringBuffer.AddRange(item.Bytes!);
                interpreter.TjSegments.Add(new TextAdjustment(item.Bytes!, 0));
            }
            else if (item.Kind == TokenKind.Number)
            {
                interpreter.ArrayNumbers.Add(item.Number);
                interpreter.TjSegments.Add(new TextAdjustment(null, item.Number));
            }
        }

        interpreter.Operands.Insert(frame.ArrayOperandIndex,
            new Token(TokenKind.String, 0, null, [.. interpreter.StringBuffer], frame.ArrayStart, frame.ArrayEnd));
    }

    private static void FinalizeMerge(InterpreterState interpreter)
    {
        if (interpreter.PendingMerge is { } run && interpreter.MergeBytes.Count > 0)
        {
            var text = interpreter.MergeText.ToString();
            run.SourceBytes = interpreter.MergeBytes.ToArray();
            run.Text = text;
            run.SourceText = text;
            run.SourceAdjustments = [.. interpreter.MergeAdjustments!];
        }

        interpreter.PendingMerge = null;
        interpreter.PendingSpanIndex = -1;
        interpreter.MergeBytes.Clear();
        interpreter.MergeText.Clear();
        interpreter.MergeAdjustments = null;
    }

    private static bool HandleGraphicsOperators(string? op, InterpreterState interpreter, ContentCollection target)
    {
        var operands = interpreter.Operands;
        var state = interpreter.Graphics;
        switch (op)
        {
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
                state.DashArray = interpreter.ArrayNumbers.ToArray();
                state.DashPhase = LastNumber(operands);
                break;

            case "Do":
                if (interpreter.PathStart < 0 && LastName(operands) is { } xobject)
                {
                    var xObjectContent = new XObjectContent(xobject)
                    {
                        Transform = state.Ctm,
                        IsArtifact = interpreter.ArtifactDepth > 0,
                    };
                    target.Add(xObjectContent);
                    interpreter.AddSpan(xObjectContent, interpreter.FrameStart, interpreter.OperatorEnd, state.Ctm, false);
                }

                break;

            default:
                return false;
        }

        return true;
    }

    private static bool HandleTextOperators(string? op, InterpreterState interpreter, ContentCollection target)
    {
        if (!ContentShows.IsShow(op))
        {
            return false;
        }

        EmitText(target, interpreter, interpreter.Graphics);
        return true;
    }

    private static bool HandlePathOperators(string? op, InterpreterState interpreter, ContentCollection target)
    {
        var operands = interpreter.Operands;
        var state = interpreter.Graphics;
        if (ContentOperatorClass.IsPathConstruction(op) && interpreter.PathStart < 0)
        {
            interpreter.PathStart = interpreter.FrameStart;
            interpreter.PathAmbient = state.Ctm;
        }

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
                EmitPath(interpreter, target, state,new PathPaint(Stroke: true, Fill: false, Close: false, EvenOdd: false), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "s":
                EmitPath(interpreter, target, state,new PathPaint(Stroke: true, Fill: false, Close: true, EvenOdd: false), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "f":
            case "F":
                EmitPath(interpreter, target, state,new PathPaint(Stroke: false, Fill: true, Close: false, EvenOdd: false), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "f*":
                EmitPath(interpreter, target, state,new PathPaint(Stroke: false, Fill: true, Close: false, EvenOdd: true), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "B":
                EmitPath(interpreter, target, state,new PathPaint(Stroke: true, Fill: true, Close: false, EvenOdd: false), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "B*":
                EmitPath(interpreter, target, state,new PathPaint(Stroke: true, Fill: true, Close: false, EvenOdd: true), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "b":
                EmitPath(interpreter, target, state,new PathPaint(Stroke: true, Fill: true, Close: true, EvenOdd: false), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "b*":
                EmitPath(interpreter, target, state,new PathPaint(Stroke: true, Fill: true, Close: true, EvenOdd: true), interpreter.ClipMode);
                interpreter.ClipMode = PathClipMode.None;
                break;

            case "n":
                if (interpreter.ClipMode != PathClipMode.None)
                {
                    EmitPath(interpreter, target, state,new PathPaint(Stroke: false, Fill: false, Close: false, EvenOdd: false), interpreter.ClipMode);
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
        if (op is not null && interpreter.PathStart < 0)
        {
            var raw = new RawContent(op, [.. interpreter.Operands])
            {
                IsArtifact = interpreter.ArtifactDepth > 0,
                ClipBounds = interpreter.Graphics.Clip,
            };
            target.Add(raw);
            interpreter.AddSpan(raw, interpreter.FrameStart, interpreter.OperatorEnd, interpreter.Graphics.Ctm, false);
        }
    }

    private sealed class InterpreterState(IReadOnlyDictionary<string, ReverseFont>? fonts)
    {
        public ContentStateMachine Machine { get; } = new(fonts, fallbackFont: null, new GraphicsState());

        public GraphicsState Graphics => (GraphicsState)Machine.State;

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

        public int FrameStart { get; set; }
        public int OperatorEnd { get; set; }
        public int PathStart { get; set; } = -1;
        public Matrix PathAmbient { get; set; }
        public List<ContentEditor.SourceElement> Spans { get; } = [];
        public int PendingSpanIndex { get; set; } = -1;

        public void AddSpan(ContentElement element, int start, int end, Matrix ambient, bool insideTextObject)
            => Spans.Add(new ContentEditor.SourceElement(element, start, end, ambient, insideTextObject));

        public List<byte> MergeBytes { get; } = [];

        public StringBuilder MergeText { get; } = new();

        public List<TextAdjustment>? MergeAdjustments { get; set; }

        public void ResetOperandFrame()
        {
            Operands.Clear();
            ArrayNumbers.Clear();
            TjSegments = null;
        }
    }


    private static void EmitText(ContentCollection target, InterpreterState interpreter, GraphicsState state)
    {
        ref var text = ref interpreter.Machine.Text;
        var textMatrix = interpreter.Machine.TextMatrix;
        var artifactDepth = interpreter.ArtifactDepth;
        var tjSegments = interpreter.TjSegments;
        var pendingMerge = interpreter.PendingMerge;

        var bytes = LastString(interpreter.Operands);
        if (bytes is null)
        {
            FinalizeMerge(interpreter);
            return;
        }

        var decoded = text.Font is not null ? text.Font.Decode(bytes) : ReverseFont.WinAnsi.Decode(bytes);
        var transform = textMatrix * state.Ctm;

        if (pendingMerge is { SourceBytes: { } pendingBytes, SourceText: not null }
            && pendingMerge.Transform == transform
            && pendingMerge.FontResourceName == text.FontName
            && pendingMerge.Font.Size == text.FontSize
            && pendingMerge.Color == state.Fill
            && pendingMerge.FillPaint == state.FillPaint
            && pendingMerge.WordSpacing == text.Spacing.WordSpacing
            && pendingMerge.CharSpacing == text.Spacing.CharSpacing
            && pendingMerge.IsArtifact == (artifactDepth > 0))
        {
            if (interpreter.MergeBytes.Count == 0)
            {
                interpreter.MergeBytes.AddRange(pendingBytes.ToArray());
                interpreter.MergeText.Append(pendingMerge.SourceText);
                interpreter.MergeAdjustments = pendingMerge.SourceAdjustments is { } existing
                    ? [.. existing]
                    : [new TextAdjustment(pendingBytes.ToArray(), 0)];
            }

            interpreter.MergeAdjustments!.AddRange(tjSegments ?? [new TextAdjustment(bytes, 0)]);
            interpreter.MergeBytes.AddRange(bytes);
            interpreter.MergeText.Append(decoded);
            if (interpreter.PendingSpanIndex >= 0)
            {
                interpreter.Spans[interpreter.PendingSpanIndex] =
                    interpreter.Spans[interpreter.PendingSpanIndex] with { End = interpreter.OperatorEnd };
            }

            return;
        }

        FinalizeMerge(interpreter);

        var run = new TextContent(decoded, 0, 0)
        {
            Font = new Font { Size = text.FontSize },
            FontResourceName = text.FontName,
            SourceBytes = bytes,
            SourceText = decoded,
            SourceFont = text.Font,
            SourceAdjustments = HasAdjustment(tjSegments) ? [.. tjSegments!] : null,
            Color = state.Fill,
            FillPaint = state.FillPaint,
            WordSpacing = text.Spacing.WordSpacing,
            CharSpacing = text.Spacing.CharSpacing,
            Transform = transform,
            IsArtifact = artifactDepth > 0,
        };
        target.Add(run);
        var insideText = interpreter.Machine.TextObjectDepth > 0;
        var ambient = insideText ? interpreter.Machine.TextMatrix * state.Ctm : state.Ctm;
        interpreter.AddSpan(run, interpreter.FrameStart, interpreter.OperatorEnd, ambient, insideText);
        interpreter.PendingSpanIndex = interpreter.Spans.Count - 1;
        interpreter.PendingMerge = run;
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

    private static void EmitPath(InterpreterState interpreter, ContentCollection target, GraphicsState state, PathPaint paint, PathClipMode clip)
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
            IsArtifact = interpreter.ArtifactDepth > 0,
        };

        foreach (var op in interpreter.PathOps)
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
        var start = interpreter.PathStart >= 0 ? interpreter.PathStart : interpreter.FrameStart;
        var ambient = interpreter.PathStart >= 0 ? interpreter.PathAmbient : state.Ctm;
        interpreter.AddSpan(path, start, interpreter.OperatorEnd, ambient, false);
        interpreter.PathStart = -1;
        interpreter.PathOps.Clear();
    }

    private static PdfRect? Intersect(PdfRect? current, PdfRect? added)
    {
        if (added is not { } bounds)
        {
            return current;
        }

        if (current is not { } existing)
        {
            return bounds;
        }

        return new PdfRect(
            Math.Max(existing.Left, bounds.Left),
            Math.Max(existing.Bottom, bounds.Bottom),
            Math.Min(existing.Right, bounds.Right),
            Math.Min(existing.Top, bounds.Top));
    }

    private static Color Rgb(List<Token> operands) => DeviceColorConverter.FromComponents(Numbers(operands, 3));

    private static Color Gray(List<Token> operands) => DeviceColorConverter.FromComponents([Number(operands, 0)]);

    private readonly record struct PathOp(string Operator, double[] Operands);

    private readonly record struct PathPaint(bool Stroke, bool Fill, bool Close, bool EvenOdd);

    private sealed class GraphicsState : ContentGraphicsState
    {
        public Color Fill { get; set; } = Color.Black;

        public Color Stroke { get; set; } = Color.Black;

        public double LineWidth { get; set; } = 1;

        public DeviceColor? FillPaint { get; set; }

        public DeviceColor? StrokePaint { get; set; }

        public string? FillColorSpace { get; set; }

        public string? StrokeColorSpace { get; set; }

        public ReadOnlyMemory<double>? DashArray { get; set; }

        public double DashPhase { get; set; }

        public PdfRect? Clip { get; set; }
    }
}

internal readonly record struct TextAdjustment(byte[]? Text, double Adjustment);

internal sealed class RawContent(string op, IReadOnlyList<Token> operands) : ContentElement
{
    public string Operator => op;

    public PdfRect? ClipBounds { get; init; }

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

internal sealed class InlineImageContent(byte[] source) : ContentElement
{
    public byte[] Source => source;

    protected override void EmitBody(ContentWriter writer)
    {
        writer.WriteBytes(source);
        writer.WriteRaw("\n");
    }
}
