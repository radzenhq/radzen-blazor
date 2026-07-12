namespace Radzen.Documents.Pdf;


/// <summary>
/// A block that lays out content in a grid of columns, rows and cells.
/// </summary>
public sealed class Table : Block
{
    /// <summary>
    /// Initializes a new empty <see cref="Table"/>.
    /// </summary>
    public Table()
    {
        Columns = new ColumnCollection(this);
        Rows = new RowCollection(this);
    }

    /// <summary>Gets the table columns.</summary>
    public ColumnCollection Columns { get; }

    /// <summary>Gets the table rows.</summary>
    public RowCollection Rows { get; }

    /// <summary>Gets the table-level borders.</summary>
    public Borders Borders { get; } = new();

    /// <summary>Gets the default font for cells that do not specify their own.</summary>
    public Font Font { get; } = new();

    /// <summary>Gets or sets the fixed table width, or <see langword="null"/> for automatic sizing.</summary>
    public Unit? Width { get; set; }

    /// <summary>Gets or sets the horizontal offset of the table from the left content edge.</summary>
    public Unit LeftIndent { get; set; }
}
