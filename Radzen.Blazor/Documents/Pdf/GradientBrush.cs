using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A single colour stop of a <see cref="GradientBrush"/>.
/// </summary>
/// <remarks>Initializes a new <see cref="GradientStop"/>.</remarks>
/// <param name="offset">The position along the gradient axis, from 0 (start) to 1 (end).</param>
/// <param name="color">The colour at this position.</param>
public sealed class GradientStop(double offset, Color color)
{
    /// <summary>Gets the position along the gradient axis, from 0 (start) to 1 (end).</summary>
    public double Offset { get; } = offset;

    /// <summary>Gets the colour at this position.</summary>
    public Color Color { get; } = color;
}

/// <summary>
/// A gradient fill defined by two or more colour stops, realized in PDF as a shading
/// (ISO 32000-1 8.7.4.5) used through a shading pattern. Coordinates are given in the
/// coordinate space in which the brush is painted (points).
/// </summary>
public abstract class GradientBrush
{
    private protected GradientBrush(GradientStop[] stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length == 0)
        {
            throw new ArgumentException("A gradient requires at least one colour stop.", nameof(stops));
        }

        // A Type 3 stitching function requires a non-decreasing offset sequence within [0 1];
        // equal adjacent offsets (CSS hard stops) are allowed and split by an epsilon at build.
        for (var i = 0; i < stops.Length; i++)
        {
            var stop = stops[i];
            ArgumentNullException.ThrowIfNull(stop);
            if (stop.Offset < 0 || stop.Offset > 1)
            {
                throw new ArgumentException($"Gradient stop offset {stop.Offset} is outside the range [0, 1].", nameof(stops));
            }

            if (i > 0 && stop.Offset < stops[i - 1].Offset)
            {
                throw new ArgumentException("Gradient stop offsets must be in non-decreasing order.", nameof(stops));
            }
        }

        Stops = (GradientStop[])stops.Clone();
    }

    /// <summary>Gets the ordered colour stops of this gradient.</summary>
    public IReadOnlyList<GradientStop> Stops { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the gradient extends beyond its start
    /// point (the first entry of the shading <c>/Extend</c> array). Defaults to true.
    /// </summary>
    public bool ExtendStart { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the gradient extends beyond its end
    /// point (the second entry of the shading <c>/Extend</c> array). Defaults to true.
    /// </summary>
    public bool ExtendEnd { get; set; } = true;

    // The shading /ShadingType of this gradient kind (ISO 32000-1 8.7.4.5): 2 axial, 3 radial.
    internal abstract int ShadingType { get; }

    // The shading /Coords array in this gradient's own coordinate space (points).
    internal abstract ArrayObject BuildCoords();
}

/// <summary>
/// An axial (linear) gradient painted along the line from (<see cref="X0"/>,
/// <see cref="Y0"/>) to (<see cref="X1"/>, <see cref="Y1"/>). Emitted as a
/// <c>/ShadingType 2</c> shading.
/// </summary>
/// <param name="x0">The gradient axis start X, in points.</param>
/// <param name="y0">The gradient axis start Y, in points.</param>
/// <param name="x1">The gradient axis end X, in points.</param>
/// <param name="y1">The gradient axis end Y, in points.</param>
/// <param name="stops">The colour stops, in axis order.</param>
public sealed class LinearGradient(double x0, double y0, double x1, double y1, params GradientStop[] stops)
    : GradientBrush(stops)
{
    /// <summary>Gets the gradient axis start X, in points.</summary>
    public double X0 { get; } = x0;

    /// <summary>Gets the gradient axis start Y, in points.</summary>
    public double Y0 { get; } = y0;

    /// <summary>Gets the gradient axis end X, in points.</summary>
    public double X1 { get; } = x1;

    /// <summary>Gets the gradient axis end Y, in points.</summary>
    public double Y1 { get; } = y1;

    internal override int ShadingType => 2;

    internal override ArrayObject BuildCoords() =>
    [
        new NumberObject(X0), new NumberObject(Y0),
        new NumberObject(X1), new NumberObject(Y1),
    ];
}

/// <summary>
/// A radial gradient painted between two circles: the start circle centred at
/// (<see cref="X0"/>, <see cref="Y0"/>) with radius <see cref="R0"/> and the end
/// circle at (<see cref="X1"/>, <see cref="Y1"/>) with radius <see cref="R1"/>.
/// Emitted as a <c>/ShadingType 3</c> shading.
/// </summary>
/// <param name="x0">The start circle centre X, in points.</param>
/// <param name="y0">The start circle centre Y, in points.</param>
/// <param name="r0">The start circle radius, in points.</param>
/// <param name="x1">The end circle centre X, in points.</param>
/// <param name="y1">The end circle centre Y, in points.</param>
/// <param name="r1">The end circle radius, in points.</param>
/// <param name="stops">The colour stops, from the start circle to the end circle.</param>
public sealed class RadialGradient(double x0, double y0, double r0, double x1, double y1, double r1, params GradientStop[] stops)
    : GradientBrush(stops)
{
    /// <summary>Gets the start circle centre X, in points.</summary>
    public double X0 { get; } = x0;

    /// <summary>Gets the start circle centre Y, in points.</summary>
    public double Y0 { get; } = y0;

    /// <summary>Gets the start circle radius, in points.</summary>
    public double R0 { get; } = r0;

    /// <summary>Gets the end circle centre X, in points.</summary>
    public double X1 { get; } = x1;

    /// <summary>Gets the end circle centre Y, in points.</summary>
    public double Y1 { get; } = y1;

    /// <summary>Gets the end circle radius, in points.</summary>
    public double R1 { get; } = r1;

    internal override int ShadingType => 3;

    internal override ArrayObject BuildCoords() =>
    [
        new NumberObject(X0), new NumberObject(Y0), new NumberObject(R0),
        new NumberObject(X1), new NumberObject(Y1), new NumberObject(R1),
    ];
}
