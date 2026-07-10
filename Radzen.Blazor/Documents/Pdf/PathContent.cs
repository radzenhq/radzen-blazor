using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

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
            writer.WriteColor(StrokeColor, "RG");
        }

        if (Fill)
        {
            writer.WriteColor(FillColor, "rg");
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

        var paint = (Stroke, Fill) switch
        {
            (true, true) => "B",
            (false, true) => "f",
            (true, false) => "S",
            _ => "n",
        };

        writer.WriteRaw(paint);
        writer.WriteRaw("\n");
    }

    private readonly record struct Segment(string Operator, double[] Operands);
}
