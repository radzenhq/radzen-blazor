using Radzen.Documents.Fonts;

namespace Radzen.Documents;


/// <summary>
/// A single item of a <see cref="ListBlock"/>, containing block-level content that flows after the item marker.
/// </summary>
public sealed class ListItem
{
    internal object? Owner { get; set; }

    /// <summary>Gets the block-level content of the item.</summary>
    public BlockCollection Blocks { get; } = [];

    /// <summary>
    /// Gets the inline content runs of the first paragraph, creating that paragraph at the start
    /// of <see cref="Blocks"/> when necessary.
    /// </summary>
    public InlineCollection Inlines => TextParagraph().Inlines;

    /// <summary>Gets the item font. Child content inherits any property it leaves unset from this font, then the list font.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the first nested list in <see cref="Blocks"/>. Setting a list appends it after
    /// the other item content and replaces the current first nested list. Assigning a list that
    /// already belongs to a document tree throws; remove it from its current parent first.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// The list already has a parent, or attaching it would make the tree cyclic.
    /// </exception>
    public ListBlock? NestedList
    {
        get
        {
            foreach (var block in Blocks)
            {
                if (block is ListBlock list)
                {
                    return list;
                }
            }

            return null;
        }
        set
        {
            var current = NestedList;
            if (ReferenceEquals(current, value))
            {
                return;
            }

            if (value is not null)
            {
                Blocks.Add(value);
            }

            if (current is not null)
            {
                Blocks.Remove(current);
            }
        }
    }

    /// <summary>Creates a nested sub-list with the specified marker style and attaches it to this item.</summary>
    /// <param name="style">The marker style of the nested list.</param>
    /// <returns>The newly created nested list.</returns>
    public ListBlock AddList(ListStyle style = ListStyle.Bullet) => NestedList = new ListBlock { Style = style };

    /// <summary>
    /// Gets or sets the text of the first paragraph. Getting returns <see langword="null"/> when
    /// there is no paragraph at the start of <see cref="Blocks"/>. Setting creates that paragraph
    /// when necessary and replaces its runs with a single run holding the value.
    /// </summary>
    public string? Text
    {
        get => Blocks.Count > 0 && Blocks[0] is Paragraph paragraph ? paragraph.Text : null;
        set => TextParagraph().Text = value;
    }

    /// <summary>
    /// Gets or sets the name of the named style applied to this item. The style supplies the item
    /// font and keep-together value when they are not set directly.
    /// </summary>
    public string? StyleName { get; set; }

    /// <summary>
    /// Gets or sets whether all blocks in this item are kept on one page when they fit, or
    /// <see langword="null"/> when none is set directly and the effective value comes from
    /// <see cref="StyleName"/> or the built-in default. Setting <see langword="null"/> resets it.
    /// </summary>
    public bool? KeepTogether { get; set; }

    private Paragraph TextParagraph()
    {
        if (Blocks.Count > 0 && Blocks[0] is Paragraph paragraph)
        {
            return paragraph;
        }

        return Blocks.Insert(0, new Paragraph());
    }
}
