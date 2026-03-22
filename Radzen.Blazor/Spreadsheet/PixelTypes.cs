using System;
using System.Text;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;
#nullable enable

/// <summary>
/// Represents a range of pixels in a spreadsheet.
/// </summary>
public readonly struct PixelRange(double start, double end)
{
    /// <summary>
    /// The start of the pixel range.
    /// </summary>
    public double Start { get; } = start;
    /// <summary>
    /// The end of the pixel range.
    /// </summary>
    public double End { get; } = end;
    /// <summary>
    /// The length of the pixel range, calculated as End - Start.
    /// </summary>
    public double Length => End - Start;

    /// <summary>
    /// Returns a new pixel range with the start offset by the specified amount.
    /// </summary>
    public PixelRange OffsetStart(double offset) => new(Start + offset, End);

    /// <summary>
    /// Returns a new pixel range with the end offset by the specified amount.
    /// </summary>
    public PixelRange OffsetEnd(double offset) => new(Start, End + offset);

    /// <summary>
    /// Checks if this pixel range contains another pixel range.
    /// </summary>
    public bool Contains(PixelRange other)
    {
        return Start <= other.Start && End >= other.End;
    }
}

/// <summary>
/// Represents a rectangle in pixel coordinates, defined by two pixel ranges: one for the x-axis and one for the y-axis.
/// </summary>
/// <param name="x"></param>
/// <param name="y"></param>
public readonly struct PixelRectangle(PixelRange x, PixelRange y)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PixelRectangle"/> struct with the specified pixel ranges.
    /// </summary>
    public PixelRectangle(double top, double left, double bottom, double right)
        : this(new PixelRange(left, right), new PixelRange(top, bottom))
    {
    }

    /// <summary>
    /// Returns the left coordinate of the rectangle.
    /// </summary>
    public double Left => x.Start;
    /// <summary>
    /// Returns the top coordinate of the rectangle.
    /// </summary>
    public double Top => y.Start;
    /// <summary>
    /// Returns the right coordinate of the rectangle.
    /// </summary>
    public double Right => x.End;
    /// <summary>
    /// Returns the bottom coordinate of the rectangle.
    /// </summary>
    public double Bottom => y.End;
    /// <summary>
    /// Returns the width of the rectangle.
    /// </summary>
    public double Width => x.Length;
    /// <summary>
    /// Returns the height of the rectangle.
    /// </summary>
    public double Height => y.Length;

    /// <summary>
    /// Returns the intersection of this rectangle with another rectangle.
    /// </summary>
    /// <param name="other"></param>
    public PixelRectangle Intersection(PixelRectangle other)
    {
        var left = Math.Max(Left, other.Left);
        var top = Math.Max(Top, other.Top);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        if (left < right && top < bottom)
        {
            return new PixelRectangle(new PixelRange(left, right), new PixelRange(top, bottom));
        }

        return new PixelRectangle(new PixelRange(0, 0), new PixelRange(0, 0));
    }

    /// <summary>
    /// Returns a string representation of the rectangle in CSS style format.
    /// </summary>
    public void AppendStyle(StringBuilder sb)
    {
        ArgumentNullException.ThrowIfNull(sb);
        sb.Append("transform: translate(");
        sb.Append(Left.ToPx());
        sb.Append(',');
        sb.Append(Top.ToPx());
        sb.Append("); width: ");
        sb.Append(Width.ToPx());
        sb.Append("; height: ");
        sb.Append(Height.ToPx());
        sb.Append(';');
    }
}

/// <summary>
/// Represents a range of indices in a spreadsheet, including the start index, end index, and an offset.
/// </summary>
public readonly struct IndexRange(int start, int end, double offset)
{
    /// <summary>
    /// The start index of the range, inclusive.
    /// </summary>
    public int Start { get; } = start;
    /// <summary>
    /// The end index of the range, inclusive.
    /// </summary>
    public int End { get; } = end;
    /// <summary>
    /// The offset of the range, which can be used to adjust the starting position of the range.
    /// </summary>
    public double Offset { get; } = offset;
}
