using Radzen.Documents.Fonts;
using Radzen.Documents.Core;
namespace Radzen.Documents;


/// <summary>
/// A block that renders a bulleted or numbered list. Each <see cref="ListItem"/> is preceded by a
/// marker (a bullet glyph or an incrementing ordinal) with the item content hanging-indented so
/// wrapped lines align under the first content line, not under the marker.
/// </summary>
public sealed class ListBlock : Block
{
    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    private Unit? hangingIndent;
    private Unit leftIndent;

    /// <summary>Gets or sets the marker style. Defaults to <see cref="ListStyle.Bullet"/>.</summary>
    public ListStyle Style { get; set; } = ListStyle.Bullet;

    /// <summary>Gets the list font. Item content inherits any property it leaves unset from this font.</summary>
    public Font Font { get; } = new();

    /// <summary>Gets or sets the indent of the whole list (the marker column) from the content-box left edge.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit LeftIndent
    {
        get => leftIndent;
        set => leftIndent = AuthoredNumber.Absolute(value, "ListBlock.LeftIndent");
    }

    /// <summary>Gets or sets the distance from the marker column to the item content. Defaults to 18 points.</summary>
    public Unit HangingIndent
    {
        get => hangingIndent ?? Unit.FromPoint(18);
        set => hangingIndent = AuthoredNumber.Absolute(value, "ListBlock.HangingIndent");
    }

    /// <summary>Gets the list items.</summary>
    public ListItemCollection Items { get; } = [];

}
