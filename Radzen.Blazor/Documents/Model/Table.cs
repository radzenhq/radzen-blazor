using Radzen.Documents.Fonts;
using Radzen.Documents.Core;
namespace Radzen.Documents;


/// <summary>
/// A block that lays out content in a grid of columns, rows and cells.
/// </summary>
public sealed class Table : Block
{
    private Unit? width;
    private Unit leftIndent;
    private Unit cornerRadius;

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

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
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? Width
    {
        get => width;
        set => width = AuthoredNumber.Absolute(value, "Table.Width");
    }

    /// <summary>Gets or sets the horizontal offset of the table from the left content edge.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit LeftIndent
    {
        get => leftIndent;
        set => leftIndent = AuthoredNumber.Absolute(value, "Table.LeftIndent");
    }

    /// <summary>
    /// Gets or sets the corner radius of the table. When positive, the table strokes a single
    /// rounded-rectangle border around its outer perimeter (when the table-level
    /// <see cref="Borders"/> are uniform) and clips its content - including the corner cells'
    /// backgrounds - to the rounded shape. The radius is clamped to half the smaller table
    /// dimension. A table that breaks across pages rounds each per-page fragment independently.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit CornerRadius
    {
        get => cornerRadius;
        set => cornerRadius = AuthoredNumber.Absolute(value, "Table.CornerRadius");
    }
}
