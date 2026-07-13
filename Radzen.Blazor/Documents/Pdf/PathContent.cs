using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A vector path built from move, line, curve and close segments, painted by
/// stroking and/or filling.
/// </summary>
public sealed class PathContent : ContentElement
{
    private readonly List<Segment> segments = [];

    /// <summary>Gets or sets a value indicating whether the path is stroked.</summary>
    public bool Stroke { get; set; }

    /// <summary>Gets or sets a value indicating whether the path is filled.</summary>
    public bool Fill { get; set; }

    /// <summary>Gets or sets the stroke line width in points. Defaults to 1.</summary>
    public double Thickness { get; set; } = 1;

    /// <summary>Gets or sets the stroke color. Defaults to black.</summary>
    public Color StrokeColor { get; set; } = Color.Black;

    /// <summary>Gets or sets the fill color. Defaults to black.</summary>
    public Color FillColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets a gradient the path is filled with, realized as a PDF shading pattern
    /// (<c>/Pattern cs</c> + <c>scn</c>). When set it overrides <see cref="FillColor"/> and
    /// <see cref="FillPaint"/> for the fill. Defaults to <see langword="null"/> (solid fill).
    /// </summary>
    public GradientBrush? FillGradient { get; set; }

    /// <summary>
    /// Gets or sets the line cap style (the <c>J</c> operator). When null (the
    /// default), no cap operator is emitted and the viewer default (butt) applies.
    /// </summary>
    public LineCap? Cap { get; set; }

    /// <summary>
    /// Gets or sets the line join style (the <c>j</c> operator). When null (the
    /// default), no join operator is emitted and the viewer default (miter) applies.
    /// </summary>
    public LineJoin? Join { get; set; }

    /// <summary>
    /// Gets or sets the miter limit (the <c>M</c> operator). When null (the default),
    /// no miter-limit operator is emitted.
    /// </summary>
    public double? MiterLimit { get; set; }

    /// <summary>
    /// Gets or sets the colour rendering intent (the <c>ri</c> operator). When null
    /// (the default), no rendering-intent operator is emitted.
    /// </summary>
    public RenderingIntent? Intent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the path is filled with the even-odd
    /// rule (the <c>f*</c>/<c>B*</c> operators) instead of the default nonzero winding
    /// rule (<c>f</c>/<c>B</c>).
    /// </summary>
    public bool EvenOdd { get; set; }

    /// <summary>
    /// Gets or sets the clipping applied by this path. When not
    /// <see cref="PathClipMode.None"/>, a <c>W</c> (nonzero) or <c>W*</c> (even-odd)
    /// operator is emitted before the paint operator, intersecting the current clip
    /// region with this path.
    /// </summary>
    public PathClipMode Clip { get; set; }

    // Round-trip state carried from a decoded content stream so a re-encode preserves
    // the source operators. The dash array/phase emit a d operator; Fill/StrokePaint
    // carry a non-RGB device color (CMYK k/K, Gray g/G or a named colorspace cs+scn).
    internal double[]? DashArray { get; set; }

    internal double DashPhase { get; set; }

    internal DeviceColor? FillPaint { get; set; }

    internal DeviceColor? StrokePaint { get; set; }

    /// <summary>
    /// Sets the fill color to a DeviceCMYK color (the <c>k</c> operator). Each
    /// component is clamped to the 0..1 range. Overrides <see cref="FillColor"/>.
    /// </summary>
    /// <param name="cyan">The cyan component.</param>
    /// <param name="magenta">The magenta component.</param>
    /// <param name="yellow">The yellow component.</param>
    /// <param name="black">The black (key) component.</param>
    public void SetFillCmyk(double cyan, double magenta, double yellow, double black)
        => FillPaint = Cmyk(cyan, magenta, yellow, black);

    /// <summary>
    /// Sets the stroke color to a DeviceCMYK color (the <c>K</c> operator). Each
    /// component is clamped to the 0..1 range. Overrides <see cref="StrokeColor"/>.
    /// </summary>
    /// <param name="cyan">The cyan component.</param>
    /// <param name="magenta">The magenta component.</param>
    /// <param name="yellow">The yellow component.</param>
    /// <param name="black">The black (key) component.</param>
    public void SetStrokeCmyk(double cyan, double magenta, double yellow, double black)
        => StrokePaint = Cmyk(cyan, magenta, yellow, black);

    /// <summary>
    /// Sets the fill color to a DeviceGray color (the <c>g</c> operator), from 0
    /// (black) to 1 (white). Overrides <see cref="FillColor"/>.
    /// </summary>
    /// <param name="gray">The gray level, clamped to the 0..1 range.</param>
    public void SetFillGray(double gray)
        => FillPaint = new DeviceColor(DeviceColorKind.Gray, null, [Clamp(gray)]);

    /// <summary>
    /// Sets the stroke color to a DeviceGray color (the <c>G</c> operator), from 0
    /// (black) to 1 (white). Overrides <see cref="StrokeColor"/>.
    /// </summary>
    /// <param name="gray">The gray level, clamped to the 0..1 range.</param>
    public void SetStrokeGray(double gray)
        => StrokePaint = new DeviceColor(DeviceColorKind.Gray, null, [Clamp(gray)]);

    private static DeviceColor Cmyk(double c, double m, double y, double k)
        => new(DeviceColorKind.Cmyk, null, [Clamp(c), Clamp(m), Clamp(y), Clamp(k)]);

    private static double Clamp(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

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
    public void MoveTo(Unit x, Unit y) => segments.Add(new Segment("m", [x.Point, y.Point]));

    /// <summary>Appends a straight line to the given point.</summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public void LineTo(Unit x, Unit y) => segments.Add(new Segment("l", [x.Point, y.Point]));

    /// <summary>Appends a cubic Bezier curve.</summary>
    /// <param name="x1">The first control point X.</param>
    /// <param name="y1">The first control point Y.</param>
    /// <param name="x2">The second control point X.</param>
    /// <param name="y2">The second control point Y.</param>
    /// <param name="x3">The end point X.</param>
    /// <param name="y3">The end point Y.</param>
    public void CurveTo(Unit x1, Unit y1, Unit x2, Unit y2, Unit x3, Unit y3)
        => segments.Add(new Segment("c", [x1.Point, y1.Point, x2.Point, y2.Point, x3.Point, y3.Point]));

    /// <summary>Closes the current subpath.</summary>
    public void Close() => segments.Add(new Segment("h", []));

    internal override void EmitBody(ContentWriter writer)
    {
        // A path that intersects the clip region (W/W*) must be balanced by a q..Q so the
        // clip is confined to this element; otherwise it leaks and shrinks the paintable
        // region of every element that follows on the page. A pattern colour space also
        // persists in the graphics state, so a gradient fill is scoped the same way. The
        // optional stroke state (cap J, join j, miter M, intent ri, dash d) likewise persists,
        // so a path that sets any of it is scoped too, keeping later paths at the viewer defaults.
        var leaksStrokeState = Cap is not null || Join is not null || MiterLimit is not null
            || Intent is not null || DashArray is not null;
        var scoped = Clip != PathClipMode.None || FillGradient is not null || leaksStrokeState;
        if (scoped)
        {
            writer.WriteRaw("q\n");
        }

        if (Stroke)
        {
            writer.WriteNumber(Thickness);
            writer.WriteRaw(" w\n");

            if (StrokePaint is { } strokePaint)
            {
                EmitDeviceColor(writer, strokePaint, stroke: true);
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
            writer.WriteRaw("[");
            for (var i = 0; i < dash.Length; i++)
            {
                if (i > 0)
                {
                    writer.WriteRaw(" ");
                }

                writer.WriteNumber(dash[i]);
            }

            writer.WriteRaw("] ");
            writer.WriteNumber(DashPhase);
            writer.WriteRaw(" d\n");
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
                EmitDeviceColor(writer, fillPaint, stroke: false);
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

    private static void EmitDeviceColor(ContentWriter writer, DeviceColor color, bool stroke)
    {
        if (color.Kind == DeviceColorKind.Named && color.ColorSpace is { } name)
        {
            writer.WriteName(name);
            writer.WriteRaw(stroke ? " CS\n" : " cs\n");
        }

        foreach (var operand in color.Operands)
        {
            writer.WriteNumber(operand);
            writer.WriteRaw(" ");
        }

        var op = color.Kind switch
        {
            DeviceColorKind.Named => stroke ? "SCN" : "scn",
            DeviceColorKind.Gray => stroke ? "G" : "g",
            _ => stroke ? "K" : "k",
        };
        writer.WriteRaw(op);
        writer.WriteRaw("\n");
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

internal enum DeviceColorKind
{
    Cmyk,
    Named,
    Gray,
}

// A path color set by an operator other than rg/RG: CMYK (k/K) or a color in a named
// colorspace (cs/scn). Operands are preserved verbatim so the path re-emits equivalently.
internal readonly record struct DeviceColor(DeviceColorKind Kind, string? ColorSpace, double[] Operands);
