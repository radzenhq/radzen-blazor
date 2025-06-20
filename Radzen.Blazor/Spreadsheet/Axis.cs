using System;
using System.Collections.Generic;
using System.Text;

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
    /// Initializes a new instance of the <see cref="PixelRange"/> struct.
    /// </summary>
    public PixelRange OffsetStart(double offset) => new(Start + offset, End);

    /// <summary>
    /// Initializes a new instance of the <see cref="PixelRange"/> struct.
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

/// <summary>
/// Represents an axis in a spreadsheet, which can be used to manage the layout of rows or columns.
/// </summary>
/// <param name="defaultValue"></param>
/// <param name="count"></param>
public class Axis(double defaultValue, int count)
{
    /// <summary>
    /// The total size of the axis when.
    /// </summary>
    public int Count { get; } = count;

    /// <summary>
    /// Fires when an axis property changes, such as when a row or column is hidden or shown, or when the size of a row or column changes.
    /// </summary>
    public event Action? Changed;

    private readonly Dictionary<int, double> data = [];

    private readonly HashSet<int> hidden = [];

    private bool isUpdating;

    /// <summary>
    /// Suspend updates to the axis. This is useful when making multiple changes at once to prevent unnecessary updates.
    /// </summary>
    public void BeginUpdate()
    {
        isUpdating = true;
    }

    /// <summary>
    /// Resume updates to the axis and trigger a change event.
    /// </summary>
    public void EndUpdate()
    {
        isUpdating = false;
        Changed?.Invoke();
    }

    private void TriggerChange()
    {
        if (!isUpdating)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Checks if the specified index is hidden.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool IsHidden(int index)
    {
        return hidden.Contains(index);
    }

    /// <summary>
    /// Hides the specified index.
    ///</summary>

    public void Hide(int index)
    {
        if (!IsHidden(index))
        {
            hidden.Add(index);
            TriggerChange();
        }
    }

    /// <summary>
    /// Shows the specified index if it is hidden.
    /// </summary>
    public void Show(int index)
    {
        if (IsHidden(index))
        {
            hidden.Remove(index);
            TriggerChange();
        }
    }

    /// <summary>
    /// Gets or sets the size of the axis at the specified index.
    /// </summary>
    public double this[int index]
    {
        get
        {
            if (data.TryGetValue(index, out var value))
            {
                return value;
            }

            return defaultValue;
        }
        set
        {
            data[index] = value;
            TriggerChange();
        }
    }

    private int frozen;

    /// <summary>
    /// Gets or sets the number of frozen items in the axis.
    /// </summary>
    public int Frozen
    {
        get => frozen;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Frozen cannot be negative.");
            }

            frozen = value;
            TriggerChange();
        }
    }

    /// <summary>
    /// Gets or sets the offset of the axis. This is used to adjust the starting position of the axis, for example, to render headers.
    /// </summary>
    public double Offset { get; set; }

    internal IndexRange GetIndexRange(double start, double end, bool includeFrozen = false)
    {
        var currentPosition = Offset;
        var startOffset = 0d;

        if (!includeFrozen)
        {
            if (Frozen > 0)
            {
                // Calculate position after frozen items
                for (int index = 0; index < Frozen; index++)
                {
                    if (!IsHidden(index))
                    {
                        currentPosition += this[index];
                    }
                }
            }
        }

        // Find start index - include items that start before the viewport end
        int startIndex = includeFrozen ? 0 : Frozen;

        for (; startIndex < Count - 1; startIndex++)
        {
            if (IsHidden(startIndex))
            {
                continue;
            }

            var segmentSize = this[startIndex];

            if (currentPosition + segmentSize > start)
            {
                startOffset = start - currentPosition;
                break;
            }

            currentPosition += segmentSize;
        }

        // Find end index - include items that end after the viewport start
        int endIndex;
        for (endIndex = startIndex; endIndex < Count - 1; endIndex++)
        {
            if (IsHidden(endIndex))
            {
                continue;
            }

            var segmentSize = this[endIndex];
            currentPosition += segmentSize;

            if (currentPosition >= end)
            {
                break;
            }
        }

        return new IndexRange(startIndex, endIndex, startOffset);
    }

    /// <summary>
    /// Gets the total size of the axis, including all visible items, default values for hidden items, and the offset.
    /// </summary>
    public double Total
    {
        get
        {
            var total = 0d;

            foreach (var item in data)
            {
                if (!IsHidden(item.Key))
                {
                    total += item.Value;
                }
            }

            return total + defaultValue * (Count - data.Count - hidden.Count) + Offset;
        }
    }

    /// <summary>
    /// Gets the pixel range for the specified start and end indices.
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    public PixelRange GetPixelRange(int startIndex, int endIndex)
    {
        double start;
        double end;
        var currentPosition = Offset;

        for (var index = 0; index < startIndex; index++)
        {
            if (!IsHidden(index))
            {
                var segmentSize = this[index];
                currentPosition += segmentSize;
            }
        }
        start = currentPosition;

        for (var index = startIndex; index <= endIndex; index++)
        {
            if (!IsHidden(index))
            {
                var segmentSize = this[index];
                currentPosition += segmentSize;
            }
        }

        end = currentPosition;

        return new PixelRange(start, end);
    }

    /// <summary>
    /// Gets the pixel range for a single index, which is equivalent to calling GetPixelRange with the same start and end index.
    /// </summary>
    public PixelRange GetPixelRange(int startIndex) => GetPixelRange(startIndex, startIndex);
}