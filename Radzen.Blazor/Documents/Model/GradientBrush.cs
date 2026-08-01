using System;
using System.Collections.ObjectModel;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A single color stop of a <see cref="GradientBrush"/>.
/// </summary>
/// <remarks>Initializes a new <see cref="GradientStop"/>.</remarks>
/// <param name="offset">The position along the gradient axis, from 0 (start) to 1 (end).</param>
/// <param name="color">The color at this position.</param>
public sealed class GradientStop(double offset, Color color)
{
    /// <summary>Gets the position along the gradient axis, from 0 (start) to 1 (end).</summary>
    public double Offset { get; } = offset;

    /// <summary>Gets the color at this position.</summary>
    public Color Color { get; } = color;
}

/// <summary>
/// A gradient fill defined by one or more color stops. Coordinates are box-relative and
/// top-origin: they are measured within the painted box of the element the brush fills,
/// with (0, 0) at the box's top-left corner, x increasing to the right and y increasing
/// downwards. An absolute <see cref="Unit"/> is an offset from that origin along the axis
/// it is given for; a relative one is that percentage of the box's extent along the same
/// axis, so 50% of x is half the box width and 50% of y is half the box height. Radii are
/// relative to the box width. Renderers map this space onto the page themselves, so the
/// same brush paints identically wherever the box lands. Stop alpha expresses the intended
/// per-stop transparency; renderers approximate it within their capabilities, and one that
/// cannot vary transparency across a gradient paints every stop fully opaque. The first stop's
/// color fills the area before the gradient start and the last stop's color fills the area beyond
/// its end.
/// </summary>
public abstract class GradientBrush
{
    /// <summary>Initializes the shared gradient state from <paramref name="stops"/>.</summary>
    /// <param name="stops">The color stops, in non-decreasing offset order within [0, 1].</param>
    private protected GradientBrush(GradientStop[] stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length == 0)
        {
            throw new ArgumentException("A gradient requires at least one color stop.", nameof(stops));
        }

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

        Stops = new ReadOnlyCollection<GradientStop>((GradientStop[])stops.Clone());
    }

    /// <summary>Gets the ordered color stops of this gradient.</summary>
    public ReadOnlyCollection<GradientStop> Stops { get; }
}

/// <summary>
/// An axial (linear) gradient painted along the line from (<see cref="X0"/>,
/// <see cref="Y0"/>) to (<see cref="X1"/>, <see cref="Y1"/>), in the box-relative
/// top-origin space described by <see cref="GradientBrush"/>.
/// </summary>
/// <param name="x0">The gradient axis start X, from the box's left edge.</param>
/// <param name="y0">The gradient axis start Y, down from the box's top edge.</param>
/// <param name="x1">The gradient axis end X, from the box's left edge.</param>
/// <param name="y1">The gradient axis end Y, down from the box's top edge.</param>
/// <param name="stops">The color stops, in axis order.</param>
public sealed class LinearGradient(Unit x0, Unit y0, Unit x1, Unit y1, params GradientStop[] stops)
    : GradientBrush(stops)
{
    /// <summary>Gets the gradient axis start X, from the box's left edge.</summary>
    public Unit X0 { get; } = x0;

    /// <summary>Gets the gradient axis start Y, down from the box's top edge.</summary>
    public Unit Y0 { get; } = y0;

    /// <summary>Gets the gradient axis end X, from the box's left edge.</summary>
    public Unit X1 { get; } = x1;

    /// <summary>Gets the gradient axis end Y, down from the box's top edge.</summary>
    public Unit Y1 { get; } = y1;
}

/// <summary>
/// A radial gradient painted between two circles: the start circle centred at
/// (<see cref="X0"/>, <see cref="Y0"/>) with radius <see cref="R0"/> and the end
/// circle at (<see cref="X1"/>, <see cref="Y1"/>) with radius <see cref="R1"/>, in the
/// box-relative top-origin space described by <see cref="GradientBrush"/>.
/// </summary>
/// <param name="x0">The start circle centre X, from the box's left edge.</param>
/// <param name="y0">The start circle centre Y, down from the box's top edge.</param>
/// <param name="r0">The start circle radius; a relative value is a percentage of the box width.</param>
/// <param name="x1">The end circle centre X, from the box's left edge.</param>
/// <param name="y1">The end circle centre Y, down from the box's top edge.</param>
/// <param name="r1">The end circle radius; a relative value is a percentage of the box width.</param>
/// <param name="stops">The color stops, from the start circle to the end circle.</param>
public sealed class RadialGradient(Unit x0, Unit y0, Unit r0, Unit x1, Unit y1, Unit r1, params GradientStop[] stops)
    : GradientBrush(stops)
{
    /// <summary>Gets the start circle centre X, from the box's left edge.</summary>
    public Unit X0 { get; } = x0;

    /// <summary>Gets the start circle centre Y, down from the box's top edge.</summary>
    public Unit Y0 { get; } = y0;

    /// <summary>Gets the start circle radius; a relative value is a percentage of the box width.</summary>
    public Unit R0 { get; } = r0;

    /// <summary>Gets the end circle centre X, from the box's left edge.</summary>
    public Unit X1 { get; } = x1;

    /// <summary>Gets the end circle centre Y, down from the box's top edge.</summary>
    public Unit Y1 { get; } = y1;

    /// <summary>Gets the end circle radius; a relative value is a percentage of the box width.</summary>
    public Unit R1 { get; } = r1;
}
