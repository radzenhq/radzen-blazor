namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A table row and its cells.
/// </summary>
public class Row
{
    internal Row()
    {
    }

    /// <summary>Gets the cells of the row.</summary>
    public CellCollection Cells { get; } = new();

    /// <summary>Gets or sets a value indicating whether the row is a repeating header row.</summary>
    public bool IsHeader { get; set; }

    /// <summary>Gets or sets a value indicating whether the row is kept on a single page.</summary>
    public bool KeepTogether { get; set; }

    /// <summary>Gets the default font for cells in the row.</summary>
    public Font Font { get; } = new();

    private HorizontalAlignment? alignment;

    /// <summary>Gets or sets the horizontal content alignment. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment
    {
        get => alignment ?? HorizontalAlignment.Left;
        set => alignment = value;
    }

    internal HorizontalAlignment? AlignmentValue => alignment;

    /// <summary>Gets or sets the row background color, or <see langword="null"/> for none.</summary>
    public Color? Background { get; set; }

    /// <summary>Gets the row-level borders, cascaded to the cells' unset edges.</summary>
    public Borders Borders { get; } = new();
}
