namespace Radzen.Documents.Pdf;


/// <summary>
/// A table column. A <see langword="null"/> <see cref="Width"/> means the column sizes automatically.
/// </summary>
public class Column
{
    /// <summary>Gets or sets the fixed column width, or <see langword="null"/> for automatic sizing.</summary>
    public Unit? Width { get; set; }

    /// <summary>Gets or sets the horizontal content alignment, or <see langword="null"/> to inherit.</summary>
    public HorizontalAlignment? Alignment { get; set; }
}
