using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A vector path built from move, line, curve and close segments, painted by
/// stroking and/or filling.
/// </summary>
public sealed class PathContent : ContentElement
{
    private readonly List<Segment> segments = [];
    private bool stroke;
    private bool fill;
    private double thickness = 1;
    private Color strokeColor = Color.Black;
    private Color fillColor = Color.Black;
    private GradientBrush? fillGradient;
    private LineCap? cap;
    private LineJoin? join;
    private double? miterLimit;
    private RenderingIntent? intent;
    private bool evenOdd;
    private PathClipMode clip;
    private ReadOnlyMemory<double>? dashArray;
    private double dashPhase;
    private DeviceColor? fillPaint;
    private DeviceColor? strokePaint;

    /// <summary>Gets or sets a value indicating whether the path is stroked.</summary>
    public bool Stroke
    {
        get => stroke;
        set => Set(ref stroke, value);
    }

    /// <summary>Gets or sets a value indicating whether the path is filled.</summary>
    public bool Fill
    {
        get => fill;
        set => Set(ref fill, value);
    }

    /// <summary>Gets or sets the stroke line width in points. Defaults to 1.</summary>
    public double Thickness
    {
        get => thickness;
        set => Set(ref thickness, value);
    }

    /// <summary>Gets or sets the stroke color. Defaults to black.</summary>
    public Color StrokeColor
    {
        get => strokeColor;
        set => Set(ref strokeColor, value);
    }

    /// <summary>Gets or sets the fill color. Defaults to black.</summary>
    public Color FillColor
    {
        get => fillColor;
        set => Set(ref fillColor, value);
    }

    /// <summary>
    /// Gets or sets a gradient the path is filled with, realized as a PDF shading pattern
    /// (<c>/Pattern cs</c> + <c>scn</c>) with no pattern matrix, so its coordinates are read
    /// in the page's default space rather than the box-relative space
    /// <see cref="GradientBrush"/> describes for modelled content. That space has no reference
    /// box, so the gradient's coordinates must be absolute lengths; a relative one throws when
    /// the page is emitted. When set it overrides
    /// <see cref="FillColor"/> and <see cref="FillPaint"/> for the fill. Defaults to
    /// <see langword="null"/> (solid fill).
    /// </summary>
    public GradientBrush? FillGradient
    {
        get => fillGradient;
        set => Set(ref fillGradient, value);
    }

    internal override ContentElement DeepClone()
    {
        var clone = CopyStateTo(new PathContent
        {
            Stroke = Stroke,
            Fill = Fill,
            Thickness = Thickness,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            FillGradient = CopyGradient(FillGradient),
            Cap = Cap,
            Join = Join,
            MiterLimit = MiterLimit,
            Intent = Intent,
            EvenOdd = EvenOdd,
            Clip = Clip,
            DashArray = DashArray is { } dash ? new ReadOnlyMemory<double>(dash.ToArray()) : null,
            DashPhase = DashPhase,
            FillPaint = ContentClone.CopyDeviceColor(FillPaint),
            StrokePaint = ContentClone.CopyDeviceColor(StrokePaint),
        });
        foreach (var segment in segments)
        {
            clone.segments.Add(new Segment(segment.Operator, [.. segment.Operands]));
        }

        return clone;
    }

    private static GradientBrush? CopyGradient(GradientBrush? source)
    {
        if (source is null)
        {
            return null;
        }

        var stops = new GradientStop[source.Stops.Count];
        for (var i = 0; i < stops.Length; i++)
        {
            stops[i] = new GradientStop(source.Stops[i].Offset, source.Stops[i].Color);
        }

        return source switch
        {
            LinearGradient linear => new LinearGradient(linear.X0, linear.Y0, linear.X1, linear.Y1, stops),
            RadialGradient radial => new RadialGradient(
                radial.X0, radial.Y0, radial.R0, radial.X1, radial.Y1, radial.R1, stops),
            _ => throw new NotSupportedException($"Gradient type '{source.GetType().FullName}' is not supported."),
        };
    }

    /// <summary>
    /// Gets or sets the line cap style (the <c>J</c> operator). When null (the
    /// default), no cap operator is emitted and the viewer default (butt) applies.
    /// </summary>
    public LineCap? Cap
    {
        get => cap;
        set => Set(ref cap, value);
    }

    /// <summary>
    /// Gets or sets the line join style (the <c>j</c> operator). When null (the
    /// default), no join operator is emitted and the viewer default (miter) applies.
    /// </summary>
    public LineJoin? Join
    {
        get => join;
        set => Set(ref join, value);
    }

    /// <summary>
    /// Gets or sets the miter limit (the <c>M</c> operator). When null (the default),
    /// no miter-limit operator is emitted.
    /// </summary>
    public double? MiterLimit
    {
        get => miterLimit;
        set => Set(ref miterLimit, value);
    }

    /// <summary>
    /// Gets or sets the color rendering intent (the <c>ri</c> operator). When null
    /// (the default), no rendering-intent operator is emitted.
    /// </summary>
    public RenderingIntent? Intent
    {
        get => intent;
        set => Set(ref intent, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the path is filled with the even-odd
    /// rule (the <c>f*</c>/<c>B*</c> operators) instead of the default nonzero winding
    /// rule (<c>f</c>/<c>B</c>).
    /// </summary>
    public bool EvenOdd
    {
        get => evenOdd;
        set => Set(ref evenOdd, value);
    }

    /// <summary>
    /// Gets or sets the clipping applied by this path. When not
    /// <see cref="PathClipMode.None"/>, a <c>W</c> (nonzero) or <c>W*</c> (even-odd)
    /// operator is emitted before the paint operator, intersecting the current clip
    /// region with this path.
    /// </summary>
    public PathClipMode Clip
    {
        get => clip;
        set => Set(ref clip, value);
    }

    internal ReadOnlyMemory<double>? DashArray
    {
        get => dashArray;
        set => Set(ref dashArray, value);
    }

    internal double DashPhase
    {
        get => dashPhase;
        set => Set(ref dashPhase, value);
    }

    internal DeviceColor? FillPaint
    {
        get => fillPaint;
        set => Set(ref fillPaint, value);
    }

    internal DeviceColor? StrokePaint
    {
        get => strokePaint;
        set => Set(ref strokePaint, value);
    }

    /// <summary>
    /// Sets the fill color to a DeviceCMYK color (the <c>k</c> operator). Each
    /// component is clamped to the 0..1 range. Overrides <see cref="FillColor"/>.
    /// </summary>
    /// <param name="cyan">The cyan component.</param>
    /// <param name="magenta">The magenta component.</param>
    /// <param name="yellow">The yellow component.</param>
    /// <param name="black">The black (key) component.</param>
    public void SetFillCmyk(double cyan, double magenta, double yellow, double black)
        => FillPaint = DeviceColor.Cmyk(cyan, magenta, yellow, black);

    /// <summary>
    /// Sets the stroke color to a DeviceCMYK color (the <c>K</c> operator). Each
    /// component is clamped to the 0..1 range. Overrides <see cref="StrokeColor"/>.
    /// </summary>
    /// <param name="cyan">The cyan component.</param>
    /// <param name="magenta">The magenta component.</param>
    /// <param name="yellow">The yellow component.</param>
    /// <param name="black">The black (key) component.</param>
    public void SetStrokeCmyk(double cyan, double magenta, double yellow, double black)
        => StrokePaint = DeviceColor.Cmyk(cyan, magenta, yellow, black);

    /// <summary>
    /// Sets the fill color to a DeviceGray color (the <c>g</c> operator), from 0
    /// (black) to 1 (white). Overrides <see cref="FillColor"/>.
    /// </summary>
    /// <param name="gray">The gray level, clamped to the 0..1 range.</param>
    public void SetFillGray(double gray)
        => FillPaint = DeviceColor.Gray(gray);

    /// <summary>
    /// Sets the stroke color to a DeviceGray color (the <c>G</c> operator), from 0
    /// (black) to 1 (white). Overrides <see cref="StrokeColor"/>.
    /// </summary>
    /// <param name="gray">The gray level, clamped to the 0..1 range.</param>
    public void SetStrokeGray(double gray)
        => StrokePaint = DeviceColor.Gray(gray);

    /// <summary>
    /// Sets the dash pattern (the <c>d</c> operator). The pattern is a sequence of
    /// alternating on/off dash lengths in points; an empty pattern draws a solid line.
    /// </summary>
    /// <param name="pattern">The alternating dash and gap lengths, in points.</param>
    /// <param name="phase">The distance into the pattern at which the line starts.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    public void SetDash(double[] pattern, double phase = 0)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        DashArray = (double[])pattern.Clone();
        DashPhase = phase;
    }

    /// <summary>Begins a new subpath at the given point.</summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public void MoveTo(Unit x, Unit y) => AddSegment(new Segment("m", [x.Point, y.Point]));

    /// <summary>Appends a straight line to the given point.</summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public void LineTo(Unit x, Unit y) => AddSegment(new Segment("l", [x.Point, y.Point]));

    /// <summary>Appends a cubic Bezier curve.</summary>
    /// <param name="x1">The first control point X.</param>
    /// <param name="y1">The first control point Y.</param>
    /// <param name="x2">The second control point X.</param>
    /// <param name="y2">The second control point Y.</param>
    /// <param name="x3">The end point X.</param>
    /// <param name="y3">The end point Y.</param>
    public void CurveTo(Unit x1, Unit y1, Unit x2, Unit y2, Unit x3, Unit y3)
        => AddSegment(new Segment("c", [x1.Point, y1.Point, x2.Point, y2.Point, x3.Point, y3.Point]));

    /// <summary>Closes the current subpath.</summary>
    public void Close() => AddSegment(new Segment("h", []));

    private void AddSegment(Segment segment)
    {
        segments.Add(segment);
        Touch();
    }

    private double ColorAlpha()
    {
        var fill = Fill && FillGradient is null && FillPaint is null ? FillColor.A / 255.0 : 1;
        var stroke = Stroke && StrokePaint is null ? StrokeColor.A / 255.0 : 1;
        if (fill < 1 && stroke < 1 && fill != stroke)
        {
            throw new NotSupportedException(
                "A path cannot fill and stroke with different color alphas; give FillColor and StrokeColor the same alpha, or split the fill and the stroke into two paths.");
        }

        return Math.Min(fill, stroke);
    }

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
    {
        var leaksStrokeState = Cap is not null || Join is not null || MiterLimit is not null
            || Intent is not null || DashArray is not null;

        var alpha = ColorAlpha();
        var scoped = Clip != PathClipMode.None || FillGradient is not null || leaksStrokeState || alpha < 1;
        if (scoped)
        {
            writer.WriteRaw("q\n");
        }

        if (alpha < 1)
        {
            writer.WriteName(writer.RegisterOpacity(alpha));
            writer.WriteRaw(" gs\n");
        }

        if (Stroke)
        {
            ContentEmitter.WriteStrokeWidth(writer, Thickness);

            if (StrokePaint is { } strokePaint)
            {
                ContentEmitter.WriteDeviceColor(writer, strokePaint, stroke: true);
            }
            else
            {
                writer.WriteColor(StrokeColor, "RG");
            }
        }

        if (Cap is { } cap)
        {
            writer.WriteNumber((int)cap);
            writer.WriteRaw(" J\n");
        }

        if (Join is { } join)
        {
            writer.WriteNumber((int)join);
            writer.WriteRaw(" j\n");
        }

        if (MiterLimit is { } miter)
        {
            writer.WriteNumber(miter);
            writer.WriteRaw(" M\n");
        }

        if (Intent is { } intent)
        {
            writer.WriteName(intent.PdfName());
            writer.WriteRaw(" ri\n");
        }

        if (DashArray is { } dash)
        {
            ContentEmitter.WriteDashPattern(writer, dash.Span, DashPhase);
        }

        if (Fill)
        {
            if (FillGradient is { } fillGradient)
            {
                writer.WriteRaw("/Pattern cs\n");
                writer.WriteName(writer.RegisterPattern(fillGradient));
                writer.WriteRaw(" scn\n");
            }
            else if (FillPaint is { } fillPaint)
            {
                ContentEmitter.WriteDeviceColor(writer, fillPaint, stroke: false);
            }
            else
            {
                writer.WriteColor(FillColor, "rg");
            }
        }

        foreach (var segment in segments)
        {
            foreach (var operand in segment.Operands)
            {
                writer.WriteNumber(operand);
                writer.WriteRaw(" ");
            }

            writer.WriteRaw(segment.Operator);
            writer.WriteRaw("\n");
        }

        if (Clip == PathClipMode.NonZero)
        {
            writer.WriteRaw("W\n");
        }
        else if (Clip == PathClipMode.EvenOdd)
        {
            writer.WriteRaw("W*\n");
        }

        writer.WriteRaw(Paint());
        writer.WriteRaw("\n");
        if (scoped)
        {
            writer.WriteRaw("Q\n");
        }
    }

    private string Paint() => (Stroke, Fill) switch
    {
        (true, true) => EvenOdd ? "B*" : "B",
        (false, true) => EvenOdd ? "f*" : "f",
        (true, false) => "S",
        _ => "n",
    };

    internal PdfRect? GetBounds()
    {
        var bounds = new PdfRectBounds();
        foreach (var segment in segments)
        {
            for (var i = 0; i + 1 < segment.Operands.Length; i += 2)
            {
                var point = Transform.Transform(segment.Operands[i], segment.Operands[i + 1]);
                bounds.Include(point.X, point.Y);
            }
        }

        return bounds.ToRectOrNull();
    }

    internal static PathContent Rectangle(double x, double y, double width, double height)
    {
        var path = new PathContent();
        path.MoveTo(x, y);
        path.LineTo(x + width, y);
        path.LineTo(x + width, y + height);
        path.LineTo(x, y + height);
        path.Close();
        return path;
    }

    private readonly record struct Segment(string Operator, double[] Operands);
}

/// <summary>
/// How a <see cref="PathContent"/> intersects the current clipping region with its
/// own path (ISO 32000-1 8.5.4, the <c>W</c> and <c>W*</c> operators).
/// </summary>
public enum PathClipMode
{
    /// <summary>The path does not clip.</summary>
    None,

    /// <summary>Clip using the nonzero winding rule (the <c>W</c> operator).</summary>
    NonZero,

    /// <summary>Clip using the even-odd rule (the <c>W*</c> operator).</summary>
    EvenOdd,
}
