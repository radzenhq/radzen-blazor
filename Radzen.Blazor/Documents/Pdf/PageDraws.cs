using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// A set that preserves first-insertion order on enumeration. Used where the collected
// members reach the serialized page /Resources: HashSet enumeration order is not
// contractual across runtimes, and byte-identical output requires a stable order.
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

    // Word spacing (Tw). Default 0 emits nothing, preserving byte-identity.
    public double WordSpacing { get; init; }

    // Horizontal scaling percent (Tz). 0 (unset) and 100 both emit nothing; only a
    // genuinely non-100 scale emits a Tz operator.
    public double HorizontalScale { get; init; }

    // Text rendering mode (Tr). 0 (fill) emits nothing beyond the synthetic-bold path.
    public int RenderMode { get; init; }

    // A non-RGB device fill colour (Gray g, CMYK k or a named colorspace cs+scn) used
    // instead of rg when set.
    public DeviceColor? FillPaint { get; init; }

    // Per-pair kern adjustments (TJ number convention, thousandths of text space:
    // positive tightens) inserted between glyphs when kerning is enabled; null = plain Tj.
    public double[]? Kerns { get; init; }
    public StructureElement? Element { get; init; }
    public Rect? Clip { get; set; }

    // Corner radius of the clip path; 0 clips to the plain `re` rectangle.
    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }

    // Applied as a q .. cm wrap around the whole draw (clip included), so the clip
    // rectangle rotates with the content it clips.
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
    public Rect? Clip { get; set; }
    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }
    public Matrix? Transform { get; set; }

    // Set for an /ImageMask stencil so its fill colour is emitted inside the image's own
    // q..Q, making the painted colour deterministic instead of the ambient fill colour.
    public Color? StencilColor { get; init; }
}

internal struct FillDraw
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required Color Color { get; init; }

    // Corner radius of the filled rounded rectangle; 0 fills a plain `re` rectangle.
    public double Radius { get; init; }
    public Rect? Clip { get; set; }
    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }

    // When set, the rectangle is painted with a shading pattern (/Pattern cs + scn)
    // instead of the solid Color; opt-in, default null keeps the solid-fill path.
    public GradientBrush? Gradient { get; init; }
}

// A uniform border stroked as a single rounded-rectangle path (one S, not four edges).
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
    public Rect? Clip { get; set; }
    public double ClipRadius { get; set; }
    public string? ExtGState { get; init; }
}

// A watermark overlay serialized after all page content: text segments and/or an
// image drawn in a coordinate system rotated around the page center, made
// semi-transparent through the page's ExtGState resource.
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
    public List<GeneratedExtGState> ExtGStates { get; } = [];
    public List<GeneratedPattern> Patterns { get; } = [];
    public WatermarkDraw? Watermark { get; set; }
    public OrderedSet<GeneratedFont> UsedFonts { get; } = [];
    public OrderedSet<GeneratedImage> UsedImages { get; } = [];

    // One ExtGState per distinct (fill, stroke) alpha pair, keyed GS0, GS1, ...
    public string RegisterExtGState(double fillAlpha, double strokeAlpha)
        => RegisterExtGState(fillAlpha, strokeAlpha, null, null, null, null, null);

    // One ExtGState per distinct full (alpha + blend + overprint + intent) tuple. An
    // alpha-only call reuses (and produces) exactly the same entries as before, so a
    // document that sets none of the extra fields stays byte identical.
    public string RegisterExtGState(
        double fillAlpha,
        double strokeAlpha,
        BlendMode? blend,
        bool? overprintStroke,
        bool? overprintFill,
        int? overprintMode,
        RenderingIntent? intent)
    {
        fillAlpha = System.Math.Clamp(fillAlpha, 0, 1);
        strokeAlpha = System.Math.Clamp(strokeAlpha, 0, 1);
        foreach (var state in ExtGStates)
        {
            if (state.FillAlpha == fillAlpha
                && state.StrokeAlpha == strokeAlpha
                && state.Blend == blend
                && state.OverprintStroke == overprintStroke
                && state.OverprintFill == overprintFill
                && state.OverprintMode == overprintMode
                && state.Intent == intent
                && state.SoftMask is null
                && !state.ClearSoftMask)
            {
                return state.Key;
            }
        }

        var key = "GS" + ExtGStates.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ExtGStates.Add(new GeneratedExtGState
        {
            Key = key,
            FillAlpha = fillAlpha,
            StrokeAlpha = strokeAlpha,
            Blend = blend,
            OverprintStroke = overprintStroke,
            OverprintFill = overprintFill,
            OverprintMode = overprintMode,
            Intent = intent,
        });
        return key;
    }

    // A soft-mask graphics state is never deduplicated: each carries its own transparency
    // group, so a fresh GS entry is always appended.
    public string RegisterSoftMaskExtGState(double fillAlpha, double strokeAlpha, GeneratedSoftMask softMask)
    {
        var key = "GS" + ExtGStates.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ExtGStates.Add(new GeneratedExtGState
        {
            Key = key,
            FillAlpha = System.Math.Clamp(fillAlpha, 0, 1),
            StrokeAlpha = System.Math.Clamp(strokeAlpha, 0, 1),
            SoftMask = softMask,
        });
        return key;
    }

    // One shading pattern per gradient brush instance, keyed P0, P1, ...
    public string RegisterPattern(GradientBrush gradient)
    {
        foreach (var pattern in Patterns)
        {
            if (ReferenceEquals(pattern.Gradient, gradient))
            {
                return pattern.Key;
            }
        }

        var key = "P" + Patterns.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Patterns.Add(new GeneratedPattern { Key = key, Gradient = gradient });
        return key;
    }

    public PlanMarks Mark() => new(Fills.Count, Edges.Count, Images.Count, Texts.Count, RoundedStrokes.Count);

    // Clips every fill, edge, image and text added after the mark to a rounded rectangle,
    // so a rounded container/cell/table confines its children to the rounded shape. Draws
    // that already carry a clip keep it - an inner rounded box wins over an outer one.
    public void ApplyRoundedClip(Rect bounds, double radius, PlanMarks mark)
    {
        for (var i = mark.Fills; i < Fills.Count; i++)
        {
            var fill = Fills[i];
            if (fill.Clip is null)
            {
                fill.Clip = bounds;
                fill.ClipRadius = radius;
                Fills[i] = fill;
            }
        }

        for (var i = mark.Edges; i < Edges.Count; i++)
        {
            var edge = Edges[i];
            if (edge.Clip is null)
            {
                edge.Clip = bounds;
                edge.ClipRadius = radius;
                Edges[i] = edge;
            }
        }

        for (var i = mark.Images; i < Images.Count; i++)
        {
            var image = Images[i];
            if (image.Clip is null)
            {
                image.Clip = bounds;
                image.ClipRadius = radius;
                Images[i] = image;
            }
        }

        for (var i = mark.Texts; i < Texts.Count; i++)
        {
            var text = Texts[i];
            if (text.Clip is null)
            {
                text.Clip = bounds;
                text.ClipRadius = radius;
                Texts[i] = text;
            }
        }
    }

    // Applies an affine transform to every draw added after the mark. Texts and images
    // carry the matrix into ContentEmitter, which wraps them in q cm .. Q. Edges bake the
    // transform into their endpoints (line width is rotation-invariant). Fills cannot stay
    // axis-aligned rectangles, so each becomes an equivalent solid stroke along the rect
    // centerline with line width = rect height (exact under butt caps); the converted
    // strokes are inserted BEFORE the marked edges so backgrounds stay under borders.
    // Fill clips are dropped in the process - rotated overflow clipping is not supported.
    // Rounded corners are also dropped under rotation: a rounded fill converts to the same
    // plain centerline stroke, and a rounded uniform border falls back to four square edges.
    public void ApplyTransform(Matrix transform, PlanMarks mark)
    {
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
            ExtGState = rounded.ExtGState,
        });
    }
}

internal readonly record struct PlanMarks(int Fills, int Edges, int Images, int Texts, int Rounded);
