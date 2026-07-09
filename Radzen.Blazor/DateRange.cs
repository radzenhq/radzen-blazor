using System;

namespace Radzen;

/// <summary>
/// Represents a range of dates with optional start and end. Used as the value of <see cref="Radzen.Blazor.RadzenDateRangePicker" />.
/// </summary>
public class DateRange : IEquatable<DateRange>
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="DateRange"/> class.
    /// </summary>
    public DateRange()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateRange"/> class with the specified start and end dates.
    /// </summary>
    /// <param name="start">The start date of the range.</param>
    /// <param name="end">The end date of the range.</param>
    public DateRange(DateTime? start, DateTime? end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets or sets the start date of the range.
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// Gets or sets the end date of the range.
    /// </summary>
    public DateTime? End { get; set; }

    /// <summary>
    /// Determines whether the specified <see cref="DateRange"/> has the same start and end dates.
    /// </summary>
    /// <param name="other">The range to compare with.</param>
    /// <returns><c>true</c> if both ranges have equal start and end dates; otherwise, <c>false</c>.</returns>
    public bool Equals(DateRange? other)
    {
        return other is not null && Nullable.Equals(Start, other.Start) && Nullable.Equals(End, other.End);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as DateRange);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }
}
