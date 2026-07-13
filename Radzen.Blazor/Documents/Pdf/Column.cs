using System;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A table column. A <see langword="null"/> <see cref="Width"/> means the column sizes automatically.
/// </summary>
public class Column
{
    private double? relativeWidth;

    /// <summary>Gets or sets the fixed column width, or <see langword="null"/> for automatic sizing.</summary>
    public Unit? Width { get; set; }

    /// <summary>
    /// Gets or sets the star (relative) width weight. Columns without a fixed <see cref="Width"/>
    /// share the remaining table width proportionally to their weight; an unset weight counts as 1.
    /// Ignored when <see cref="Width"/> is set. Must be positive.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is not positive.</exception>
    public double? RelativeWidth
    {
        get => relativeWidth;
        set
        {
            if (value is { } weight && !(weight > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The relative width must be positive.");
            }

            relativeWidth = value;
        }
    }

    /// <summary>Gets or sets the horizontal content alignment, or <see langword="null"/> to inherit.</summary>
    public HorizontalAlignment? Alignment { get; set; }
}
