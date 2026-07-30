namespace Radzen.Documents;


/// <summary>
/// A table column. A <see langword="null"/> <see cref="Width"/> means the column sizes automatically.
/// </summary>
public sealed class Column
{
    private double? relativeWidth;

    /// <summary>Gets or sets the fixed column width, or <see langword="null"/> for automatic sizing.</summary>
    public Unit? Width { get; set; }

    /// <summary>
    /// Gets or sets the star (relative) width weight. Columns without a fixed <see cref="Width"/>
    /// share the remaining table width proportionally to their weight; an unset weight counts as 1.
    /// Ignored when <see cref="Width"/> is set. Must be positive.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is not a finite positive number.</exception>
    public double? RelativeWidth
    {
        get => relativeWidth;
        set => relativeWidth = value is { } weight
            ? AuthoredNumber.Positive(weight, "Column.RelativeWidth")
            : null;
    }

    /// <summary>Gets or sets the horizontal content alignment, or <see langword="null"/> to inherit.</summary>
    public HorizontalAlignment? Alignment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column's cells are header cells describing the
    /// rows beside them, announced as such by assistive technology. A cell that is in both a header
    /// column and a header row - see <see cref="Row.IsHeaderRow"/> - heads both its row and its column.
    /// </summary>
    public bool IsHeaderColumn { get; set; }

    internal object? Owner { get; set; }
}
