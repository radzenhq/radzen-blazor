using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;


// Parses a page content stream back into ContentElement objects, folding the
// graphics-state (q/Q, cm) and text (Tm/Td) stacks into an absolute Transform per
// painted element. The inverse of the C3 emitter.
internal static class ContentInterpreter
{
    public static void Materialize(byte[] content, ContentCollection target, IReadOnlyDictionary<string, ReverseFont>? fonts = null, ContentTokenizer.Cache? cache = null)
    {
        var tokens = ContentTokenizer.Tokenize(content, cache);
        var interpreter = new InterpreterState(fonts);

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
                    FinalizeMerge(interpreter);
                    interpreter.ResetOperandFrame();
                    continue;
            }

            var op = token.Text;

            // The shared machine owns q/Q, cm, the text matrices and the text state. What it
            // consumes has no element of its own, which is also why those operators need no
            // passthrough: the source bytes carrying them are copied verbatim by the editor.
            if (!interpreter.Machine.Apply(op, interpreter.Operands)
                && !HandleGraphicsOperators(op, interpreter, target)
                && !HandleTextOperators(op, interpreter, target)
                && !HandlePathOperators(op, interpreter, target)
                && !HandleMarkedContentOperators(op, interpreter))
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
    }

    // Writes a fold chain's accumulated bytes/text onto the run once, when the chain ends.
    // Folding leaves the run's SourceBytes/SourceText at their pre-fold values, so every
    // path that abandons PendingMerge must come through here. The accumulators stay inside
    // the interpreter and are frozen onto the run here: assigning a still-growing list would
    // leave the run aliasing a buffer that keeps changing behind its change-detection door.
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

    // The machine has already advanced the line and applied a " operator's own aw/ac, so
    // every show reads the same text state a viewer would have in effect here.
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
        if (op is not null)
        {
            target.Add(new RawContent(op, [.. interpreter.Operands])
            {
                IsArtifact = interpreter.ArtifactDepth > 0,
                ClipBounds = interpreter.Graphics.Clip,
            });
        }
    }

    private sealed class InterpreterState(IReadOnlyDictionary<string, ReverseFont>? fonts)
    {
        // An unresolvable Tf leaves the font null so an edited run re-encodes through
        // WinAnsi substitution rather than failing on a font that was never really there.
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

        // Growable accumulators for the run PendingMerge points at; empty until a fold happens.
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
            // The first fold seeds the growable buffers from the run as authored; later folds
            // append, so a k-show chain copies each chunk once instead of re-copying the whole
            // run per show. FinalizeMerge writes the result back when the chain ends.
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
            // Only carry the TJ array when it holds a numeric adjustment; a plain string
            // (Tj or a single-element TJ) re-emits through the simpler Tj path unchanged.
            SourceAdjustments = HasAdjustment(tjSegments) ? [.. tjSegments!] : null,
            Color = state.Fill,
            FillPaint = state.FillPaint,
            WordSpacing = text.Spacing.WordSpacing,
            CharSpacing = text.Spacing.CharSpacing,
            Transform = transform,
            IsArtifact = artifactDepth > 0,
        };
        target.Add(run);
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

    // The paint disposition of a path operator: stroke/fill, whether it closes the last
    // subpath, and even-odd vs nonzero fill.
    private readonly record struct PathPaint(bool Stroke, bool Fill, bool Close, bool EvenOdd);

    // The CTM and text state live in the shared base, which is also what q/Q saves; these
    // are the extra members only materialization needs.
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

        // A bounding box the current clipping path is known to fit inside, or null while no
        // clip is active. Only ever a superset of the real region, so it can bound an
        // operator that paints the whole clip (sh) without under-reporting its extent.
        public PdfRect? Clip { get; set; }
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
    public PdfRect? ClipBounds { get; init; }

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
