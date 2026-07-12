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

    // Round-trip state carried from a decoded content stream so a re-encode preserves
    // the source operators. Even-odd selects the f*/B* fill rule; Clip emits W/W* before
    // the paint operator; the dash array/phase emit a d operator; Fill/StrokePaint carry
    // a non-RGB device color (CMYK k/K or a named colorspace cs+scn) verbatim.
    internal bool EvenOdd { get; set; }

    internal PathClipMode Clip { get; set; }

    internal double[]? DashArray { get; set; }

    internal double DashPhase { get; set; }

    internal DeviceColor? FillPaint { get; set; }

    internal DeviceColor? StrokePaint { get; set; }

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
            if (FillPaint is { } fillPaint)
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

        var op = color.Kind == DeviceColorKind.Named
            ? (stroke ? "SCN" : "scn")
            : (stroke ? "K" : "k");
        writer.WriteRaw(op);
        writer.WriteRaw("\n");
    }

    private readonly record struct Segment(string Operator, double[] Operands);
}

internal enum PathClipMode
{
    None,
    NonZero,
    EvenOdd,
}

internal enum DeviceColorKind
{
    Cmyk,
    Named,
}

// A path color set by an operator other than rg/RG: CMYK (k/K) or a color in a named
// colorspace (cs/scn). Operands are preserved verbatim so the path re-emits equivalently.
internal readonly record struct DeviceColor(DeviceColorKind Kind, string? ColorSpace, double[] Operands);
