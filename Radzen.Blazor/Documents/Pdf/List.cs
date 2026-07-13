using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A block that renders a bulleted or numbered list. Each <see cref="ListItem"/> is preceded by a
/// marker (a bullet glyph or an incrementing ordinal) with the item content hanging-indented so
/// wrapped lines align under the first content line, not under the marker.
/// </summary>
public sealed class List : Block
{
    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    private Unit? hangingIndent;

    /// <summary>Gets or sets the marker style. Defaults to <see cref="ListStyle.Bullet"/>.</summary>
    public ListStyle Style { get; set; } = ListStyle.Bullet;

    /// <summary>Gets the list font. Item content inherits any property it leaves unset from this font.</summary>
    public Font Font { get; } = new();

    /// <summary>Gets or sets the indent of the whole list (the marker column) from the content-box left edge.</summary>
    public Unit LeftIndent { get; set; }

    /// <summary>Gets or sets the distance from the marker column to the item content. Defaults to 18 points.</summary>
    public Unit HangingIndent
    {
        get => hangingIndent ?? Unit.FromPoint(18);
        set => hangingIndent = value;
    }

    /// <summary>Gets the list items.</summary>
    public ListItemCollection Items { get; } = [];

    /// <summary>Appends an empty item.</summary>
    /// <returns>The newly created item.</returns>
    public ListItem AddItem() => Items.Add();

    /// <summary>Appends an item containing the specified text.</summary>
    /// <param name="text">The item text.</param>
    /// <returns>The newly created item.</returns>
    public ListItem AddItem(string text) => Items.Add(text);
}
