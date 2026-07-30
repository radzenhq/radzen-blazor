using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Geometry;

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

internal struct TextDraw
{
    public required double X { get; init; }
    public required double Baseline { get; init; }
    public required double Size { get; init; }
    public required Color Color { get; init; }
    public required GeneratedFont Font { get; init; }
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
    public PdfRect? Clip { get; set; }

    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }

    public Matrix? Transform { get; set; }
}

internal struct ImageDraw
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required GeneratedImage Image { get; init; }
    public StructureElement? Element { get; init; }
    public SemanticArtifactKind? Artifact { get; init; }
    public int Sequence { get; init; }
    public PdfRect? Clip { get; set; }
    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }
    public Matrix? Transform { get; set; }
}

internal struct FillDraw
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
    public PdfRect? Clip { get; set; }
    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }

    public GradientPaint? Gradient { get; init; }
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
    public SemanticArtifactKind? Artifact { get; init; }
    public string? ExtGState { get; init; }
}

internal struct EdgeDraw
{
    public required double X1 { get; init; }
    public required double Y1 { get; init; }
    public required double X2 { get; init; }
    public required double Y2 { get; init; }
    public required double LineWidth { get; init; }
    public required Color Color { get; init; }
    public required BorderStyle Style { get; init; }
    public SemanticArtifactKind? Artifact { get; init; }
    public PdfRect? Clip { get; set; }
    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }
}

internal sealed class WatermarkDraw
{
    public required double CenterX { get; init; }
    public required double CenterY { get; init; }
    public required double Rotation { get; init; }
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
    public List<GeneratedLink> Links { get; } = [];
    private int sequence;

    public int NextSequence() => sequence++;
    private readonly ResourceKeyRegistry<string, GeneratedExtGState> extGStates =
        new("GS", StringComparer.Ordinal);

    private readonly ResourceKeyRegistry<(GradientPaint Gradient, Matrix Matrix), GeneratedPattern> patterns =
        new("P", GradientPatternComparer.Instance);

    private readonly Dictionary<string, GeneratedExtGState> extGStatesByKey = new(StringComparer.Ordinal);

    public IReadOnlyList<GeneratedExtGState> ExtGStates => extGStates.Values;

    public IReadOnlyList<GeneratedPattern> Patterns => patterns.Values;

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
    public OrderedSet<GeneratedFont> UsedFonts { get; } = [];
    public OrderedSet<GeneratedImage> UsedImages { get; } = [];

    public string RegisterExtGState(double fillAlpha, double strokeAlpha)
        => RegisterExtGState(fillAlpha, strokeAlpha, null);

    public string RegisterExtGState(double fillAlpha, double strokeAlpha, BlendMode? blend)
    {
        fillAlpha = Math.Clamp(fillAlpha, 0, 1);
        strokeAlpha = Math.Clamp(strokeAlpha, 0, 1);
        var dedupKey = string.Create(
            CultureInfo.InvariantCulture,
            $"a|{fillAlpha}|{strokeAlpha}|{blend}");
        return extGStates.GetOrAdd(dedupKey, key => Track(new GeneratedExtGState
        {
            Key = key,
            FillAlpha = fillAlpha,
            StrokeAlpha = strokeAlpha,
            Blend = blend,
        }));
    }

    private GeneratedExtGState Track(GeneratedExtGState state)
    {
        extGStatesByKey[state.Key] = state;
        return state;
    }

    public GeneratedExtGState? FindExtGState(string key)
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

        if (FindExtGState(extGState) is not { SoftMask: null, ClearSoftMask: false } state)
        {
            return extGState;
        }

        return RegisterExtGState(
            state.FillAlpha * alpha,
            state.StrokeAlpha * alpha,
            state.Blend);
    }

    public string RegisterSoftMaskExtGState(double fillAlpha, double strokeAlpha, GeneratedSoftMask softMask)
    {
        GeneratedExtGState Create(string key) => Track(new GeneratedExtGState
        {
            Key = key,
            FillAlpha = Math.Clamp(fillAlpha, 0, 1),
            StrokeAlpha = Math.Clamp(strokeAlpha, 0, 1),
            SoftMask = softMask,
        });

        return softMask.ContentKey is { } contentKey
            ? extGStates.GetOrAdd("m|" + contentKey, Create)
            : extGStates.Add(Create);
    }

    public string RegisterPattern(GradientPaint gradient, Matrix matrix)
        => patterns.GetOrAdd(
            (gradient, matrix),
            key => new GeneratedPattern { Key = key, Pattern = ShadingBuilder.BuildPattern(gradient, matrix) });

    public PlanMarks Mark() => new(Fills.Count, Edges.Count, Images.Count, Texts.Count, RoundedStrokes.Count);

    public void ApplyRoundedClip(PdfRect bounds, double radius, PlanMarks mark)
    {
        ApplyClip(Fills, mark.Fills, fill =>
        {
            if (fill.Clip is null)
            {
                fill.Clip = bounds;
                fill.ClipRadius = radius;
            }

            return fill;
        });
        ApplyClip(Edges, mark.Edges, edge =>
        {
            if (edge.Clip is null)
            {
                edge.Clip = bounds;
                edge.ClipRadius = radius;
            }

            return edge;
        });
        ApplyClip(Images, mark.Images, image =>
        {
            if (image.Clip is null)
            {
                image.Clip = bounds;
                image.ClipRadius = radius;
            }

            return image;
        });
        ApplyClip(Texts, mark.Texts, text =>
        {
            if (text.Clip is null)
            {
                text.Clip = bounds;
                text.ClipRadius = radius;
            }

            return text;
        });
    }

    private static void ApplyClip<T>(List<T> items, int start, Func<T, T> clip)
    {
        for (var i = start; i < items.Count; i++)
        {
            items[i] = clip(items[i]);
        }
    }

    public void ApplyTransform(Matrix transform, PlanMarks mark)
    {
        for (var i = mark.Fills; i < Fills.Count; i++)
        {
            var fill = Fills[i];
            if (fill.Radius > 0 || (fill.Clip is not null && fill.ClipRadius > 0))
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve rounded corners or a rounded clip; remove the corner radius or the rotation.");
            }

            if (fill.Gradient is not null)
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve a gradient background; remove the gradient or the rotation.");
            }
        }

        for (var i = mark.Rounded; i < RoundedStrokes.Count; i++)
        {
            if (RoundedStrokes[i].Radius > 0)
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve a rounded border; remove the corner radius or the rotation.");
            }
        }

        for (var i = mark.Edges; i < Edges.Count; i++)
        {
            if (Edges[i].Clip is not null && Edges[i].ClipRadius > 0)
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve a rounded clip on a border edge; remove the corner radius or the rotation.");
            }
        }

        for (var i = mark.Edges; i < Edges.Count; i++)
        {
            var edge = Edges[i];
            var (x1, y1) = transform.Transform(edge.X1, edge.Y1);
            var (x2, y2) = transform.Transform(edge.X2, edge.Y2);
            Edges[i] = new EdgeDraw
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                LineWidth = edge.LineWidth,
                Color = edge.Color,
                Style = edge.Style,
                Artifact = edge.Artifact,
                ExtGState = edge.ExtGState,
            };
        }

        if (Fills.Count > mark.Fills)
        {
            var converted = new List<EdgeDraw>(Fills.Count - mark.Fills);
            for (var i = mark.Fills; i < Fills.Count; i++)
            {
                var fill = Fills[i];
                if (fill.Width <= 0 || fill.Height <= 0)
                {
                    continue;
                }

                var midY = fill.Y + fill.Height / 2;
                var (x1, y1) = transform.Transform(fill.X, midY);
                var (x2, y2) = transform.Transform(fill.X + fill.Width, midY);
                converted.Add(new EdgeDraw
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    LineWidth = fill.Height,
                    Color = fill.Color,
                    Style = BorderStyle.Solid,
                    Artifact = fill.Artifact ?? SemanticArtifactKind.LayoutDecoration,
                    ExtGState = fill.ExtGState,
                });
            }

            Fills.RemoveRange(mark.Fills, Fills.Count - mark.Fills);
            Edges.InsertRange(mark.Edges, converted);
        }

        if (RoundedStrokes.Count > mark.Rounded)
        {
            for (var i = mark.Rounded; i < RoundedStrokes.Count; i++)
            {
                var rounded = RoundedStrokes[i];
                var left = rounded.X;
                var bottom = rounded.Y;
                var right = rounded.X + rounded.Width;
                var top = rounded.Y + rounded.Height;
                AddTransformedEdge(transform, left, top, right, top, rounded);
                AddTransformedEdge(transform, right, bottom, right, top, rounded);
                AddTransformedEdge(transform, left, bottom, right, bottom, rounded);
                AddTransformedEdge(transform, left, bottom, left, top, rounded);
            }

            RoundedStrokes.RemoveRange(mark.Rounded, RoundedStrokes.Count - mark.Rounded);
        }

        for (var i = mark.Images; i < Images.Count; i++)
        {
            var image = Images[i];
            image.Transform = transform;
            Images[i] = image;
        }

        for (var i = mark.Texts; i < Texts.Count; i++)
        {
            var text = Texts[i];
            text.Transform = transform;
            Texts[i] = text;
        }
    }

    private void AddTransformedEdge(Matrix transform, double x1, double y1, double x2, double y2, RoundedStrokeDraw rounded)
    {
        var (tx1, ty1) = transform.Transform(x1, y1);
        var (tx2, ty2) = transform.Transform(x2, y2);
        Edges.Add(new EdgeDraw
        {
            X1 = tx1,
            Y1 = ty1,
            X2 = tx2,
            Y2 = ty2,
            LineWidth = rounded.LineWidth,
            Color = rounded.Color,
            Style = rounded.Style,
            Artifact = rounded.Artifact,
            ExtGState = rounded.ExtGState,
        });
    }
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
