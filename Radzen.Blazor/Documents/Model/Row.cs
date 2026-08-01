using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A table row and its cells.
/// </summary>
public sealed class Row
{
    internal Row()
    {
    }

    internal object? Owner { get; set; }

    /// <summary>Gets the cells of the row.</summary>
    public CellCollection Cells { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the row is repeated at the top of every page the
    /// table spans. Repetition is purely visual and does not by itself make the row a header for
    /// assistive technology - see <see cref="IsHeaderRow"/>.
    /// </summary>
    public bool RepeatOnEveryPage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row's cells are header cells describing the
    /// columns beneath them, announced as such by assistive technology. A row that is repeated
    /// across pages is still a single header row - see <see cref="RepeatOnEveryPage"/>.
    /// </summary>
    public bool IsHeaderRow { get; set; }

    /// <summary>Gets or sets a value indicating whether the row is kept on a single page.</summary>
    public bool KeepTogether { get; set; }

    /// <summary>Gets the default font for cells in the row.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the horizontal content alignment set directly on this row, or <see langword="null"/>
    /// when none is set and the cells fall back to their own context. Setting <see langword="null"/>
    /// resets it.
    /// </summary>
    public HorizontalAlignment? Alignment { get; set; }

    /// <summary>Gets or sets the row background color, or <see langword="null"/> for none.</summary>
    public Color? Background { get; set; }

    /// <summary>Gets the row-level borders, cascaded to the cells' unset edges.</summary>
    public Borders Borders { get; } = new();
}
