using System.Collections.Generic;
using System.Collections;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf.Render;

internal sealed class OrderedSet<T> : IEnumerable<T>
    where T : notnull
{
    private readonly List<T> items = [];
    private readonly HashSet<T> seen = [];

    public int Count => items.Count;

    public void Add(T item)
    {
        if (seen.Add(item))
        {
            items.Add(item);
        }
    }

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal readonly struct TextDraw
{
    public required double X { get; init; }
    public required double Baseline { get; init; }
    public required double Size { get; init; }
    public required Color Color { get; init; }
    public required EmittedFont Font { get; init; }
    public required byte[] Bytes { get; init; }
    public double StrokeWidth { get; init; }
    public double Shear { get; init; }
    public double CharSpacing { get; init; }
    public double Rise { get; init; }

    public double WordSpacing { get; init; }

    public double HorizontalScale { get; init; }

    public int RenderMode { get; init; }

    public double[]? Kerns { get; init; }
    public StructureElement? Element { get; init; }
    public SemanticArtifactKind? Artifact { get; init; }
    public int Sequence { get; init; }
    public PdfRect? Clip { get; init; }

    public double ClipRadius { get; init; }
    public string? ExtGState { get; init; }

    public Matrix? Transform { get; init; }

    public PaintStack? Stack { get; init; }
}

internal readonly struct ImageDraw
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required OutputImage Image { get; init; }
    public StructureElement? Element { get; init; }
    public SemanticArtifactKind? Artifact { get; init; }
    public int Sequence { get; init; }
    public PdfRect? Clip { get; init; }
    public double ClipRadius { get; init; }
    public string? ExtGState { get; init; }
    public Matrix? Transform { get; init; }

    public PaintStack? Stack { get; init; }
}

internal readonly struct FillDraw
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required Color Color { get; init; }

    public StructureElement? Element { get; init; }
    public SemanticArtifactKind? Artifact { get; init; }
    public int Sequence { get; init; }

    public double Radius { get; init; }
    public PdfRect? Clip { get; init; }
    public double ClipRadius { get; init; }
    public string? ExtGState { get; init; }

    public GradientPaint? Gradient { get; init; }

    public PaintStack? Stack { get; init; }
}

internal readonly struct RoundedStrokeDraw
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required double Radius { get; init; }
    public required double LineWidth { get; init; }
    public required Color Color { get; init; }
    public required BorderStyle Style { get; init; }
    public StructureElement? Element { get; init; }
    public SemanticArtifactKind? Artifact { get; init; }
    public int Sequence { get; init; }
    public string? ExtGState { get; init; }

    public PaintStack? Stack { get; init; }
}

internal readonly struct EdgeDraw
{
    public required double X1 { get; init; }
    public required double Y1 { get; init; }
    public required double X2 { get; init; }
    public required double Y2 { get; init; }
    public required double LineWidth { get; init; }
    public required Color Color { get; init; }
    public required BorderStyle Style { get; init; }
    public SemanticArtifactKind? Artifact { get; init; }
    public PdfRect? Clip { get; init; }
    public double ClipRadius { get; init; }
    public string? ExtGState { get; init; }

    public PaintStack? Stack { get; init; }
}

internal readonly record struct PaintStack(int Layer, PaintStackOrder Order);

internal sealed class PaintStackOrder(PaintStackOrder? parent, int zOrder) : IComparable<PaintStackOrder>
{
    private readonly int[] path = parent is null ? [zOrder] : [.. parent.path, zOrder];

    public PaintStackOrder? Parent { get; } = parent;

    public int CompareTo(PaintStackOrder? other)
    {
        if (other is null)
        {
            return 1;
        }

        var count = Math.Min(path.Length, other.path.Length);
        for (var i = 0; i < count; i++)
        {
            var comparison = path[i].CompareTo(other.path[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return path.Length.CompareTo(other.path.Length);
    }
}

internal sealed class WatermarkDraw
{
    public required double CenterX { get; init; }
    public required double CenterY { get; init; }
    public required double Rotation { get; init; }
    public required SemanticArtifactKind Artifact { get; init; }
    public string? ExtGState { get; init; }
    public List<TextDraw> Texts { get; } = [];
    public ImageDraw? Image { get; set; }
}

internal sealed class PagePlan
{
    public required PageSize Size { get; init; }
    public List<FillDraw> Fills { get; } = [];
    public List<EdgeDraw> Edges { get; } = [];
    public List<RoundedStrokeDraw> RoundedStrokes { get; } = [];
    public List<ImageDraw> Images { get; } = [];
    public List<TextDraw> Texts { get; } = [];
    public List<OutputLink> Links { get; } = [];
    private int sequence;

    public int NextSequence() => sequence++;

    public StackMark BeginStack(int layer, PaintStackOrder order)
        => new(new PaintStack(layer, order), Mark());

    public void EndStack(in StackMark mark)
    {
        for (var i = mark.Marks.Fills; i < Fills.Count; i++)
        {
            if (Fills[i].Stack is null)
            {
                Fills[i] = Fills[i] with { Stack = mark.Stack };
            }
        }

        for (var i = mark.Marks.Edges; i < Edges.Count; i++)
        {
            if (Edges[i].Stack is null)
            {
                Edges[i] = Edges[i] with { Stack = mark.Stack };
            }
        }

        for (var i = mark.Marks.Images; i < Images.Count; i++)
        {
            if (Images[i].Stack is null)
            {
                Images[i] = Images[i] with { Stack = mark.Stack };
            }
        }

        for (var i = mark.Marks.Texts; i < Texts.Count; i++)
        {
            if (Texts[i].Stack is null)
            {
                Texts[i] = Texts[i] with { Stack = mark.Stack };
            }
        }

        for (var i = mark.Marks.Rounded; i < RoundedStrokes.Count; i++)
        {
            if (RoundedStrokes[i].Stack is null)
            {
                RoundedStrokes[i] = RoundedStrokes[i] with { Stack = mark.Stack };
            }
        }
    }
    private readonly ResourceNameAllocator<string, OutputExtGState> extGStates =
        new(ContentResourcePrefixes.Page.ExtGState, reserved: null, StringComparer.Ordinal);

    private readonly ResourceNameAllocator<(GradientPaint Gradient, Matrix Matrix), OutputPattern> patterns =
        new(ContentResourcePrefixes.Page.Pattern, reserved: null, GradientPatternComparer.Instance);

    private readonly Dictionary<string, OutputExtGState> extGStatesByKey = new(StringComparer.Ordinal);

    public IReadOnlyList<OutputExtGState> ExtGStates => extGStates.Values;

    public IReadOnlyList<OutputPattern> Patterns => patterns.Values;

    private readonly Dictionary<(double Width, double Height, double Radius, double Blur), ShadowMask> shadowMasks = [];

    public ShadowMask RenderShadowMask(double shapeWidthPt, double shapeHeightPt, double radiusPt, double blurPt)
    {
        var key = (shapeWidthPt, shapeHeightPt, radiusPt, blurPt);
        if (!shadowMasks.TryGetValue(key, out var mask))
        {
            mask = GaussianBlur.Render(shapeWidthPt, shapeHeightPt, radiusPt, blurPt);
            shadowMasks[key] = mask;
        }

        return mask;
    }
    public WatermarkDraw? Watermark { get; set; }
    public OrderedSet<EmittedFont> UsedFonts { get; } = [];
    public OrderedSet<OutputImage> UsedImages { get; } = [];

    public string RegisterExtGState(double fillAlpha, double strokeAlpha)
        => RegisterExtGState(fillAlpha, strokeAlpha, null);

    public string RegisterExtGState(double fillAlpha, double strokeAlpha, BlendMode? blend)
    {
        fillAlpha = Math.Clamp(fillAlpha, 0, 1);
        strokeAlpha = Math.Clamp(strokeAlpha, 0, 1);
        return ExtGStateRegistration.RegisterAlpha(extGStates, fillAlpha, strokeAlpha, blend, key => Track(new OutputExtGState(
            key,
            fillAlpha,
            strokeAlpha,
            blend,
            softMask: null)));
    }

    private OutputExtGState Track(OutputExtGState state)
    {
        extGStatesByKey[state.Key] = state;
        return state;
    }

    public OutputExtGState? FindExtGState(string key)
        => extGStatesByKey.TryGetValue(key, out var state) ? state : null;

    public string? ApplyAlpha(string? extGState, double alpha)
    {
        if (alpha >= 1)
        {
            return extGState;
        }

        if (extGState is null)
        {
            return RegisterExtGState(alpha, alpha);
        }

        if (FindExtGState(extGState) is not { SoftMask: null } state)
        {
            return extGState;
        }

        return RegisterExtGState(
            state.FillAlpha * alpha,
            state.StrokeAlpha * alpha,
            state.Blend);
    }

    public string RegisterSoftMaskExtGState(
        double fillAlpha,
        double strokeAlpha,
        OutputSoftMask softMask,
        string? contentKey)
    {
        OutputExtGState Create(string key) => Track(new OutputExtGState(
            key,
            Math.Clamp(fillAlpha, 0, 1),
            Math.Clamp(strokeAlpha, 0, 1),
            blend: null,
            softMask));

        return contentKey is { } dedup
            ? extGStates.GetOrAdd("m|" + dedup, Create)
            : extGStates.Add(Create);
    }

    public string RegisterPattern(GradientPaint gradient, Matrix matrix)
        => patterns.GetOrAdd((gradient, matrix), key => new OutputPattern(key, gradient, matrix));

    public PlanMarks Mark() => new(Fills.Count, Edges.Count, Images.Count, Texts.Count, RoundedStrokes.Count);
}

internal sealed class GradientPatternComparer : IEqualityComparer<(GradientPaint Gradient, Matrix Matrix)>
{
    public static GradientPatternComparer Instance { get; } = new();

    public bool Equals(
        (GradientPaint Gradient, Matrix Matrix) x,
        (GradientPaint Gradient, Matrix Matrix) y)
    {
        var left = x.Gradient;
        var right = y.Gradient;
        if (left.Identity != right.Identity
            || left.Kind != right.Kind
            || left.X0 != right.X0
            || left.Y0 != right.Y0
            || left.R0 != right.R0
            || left.X1 != right.X1
            || left.Y1 != right.Y1
            || left.R1 != right.R1
            || left.Stops.Length != right.Stops.Length
            || !x.Matrix.Equals(y.Matrix))
        {
            return false;
        }

        for (var i = 0; i < left.Stops.Length; i++)
        {
            if (left.Stops[i] != right.Stops[i])
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode((GradientPaint Gradient, Matrix Matrix) value)
    {
        var gradient = value.Gradient;
        var hash = new HashCode();
        hash.Add(gradient.Identity);
        hash.Add(gradient.Kind);
        hash.Add(gradient.X0);
        hash.Add(gradient.Y0);
        hash.Add(gradient.R0);
        hash.Add(gradient.X1);
        hash.Add(gradient.Y1);
        hash.Add(gradient.R1);
        foreach (var stop in gradient.Stops)
        {
            hash.Add(stop);
        }

        hash.Add(value.Matrix);
        return hash.ToHashCode();
    }
}

internal readonly record struct PlanMarks(int Fills, int Edges, int Images, int Texts, int Rounded);

internal readonly record struct StackMark(PaintStack Stack, PlanMarks Marks);
