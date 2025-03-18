namespace Radzen.Blazor.Markdown;

/// <summary>
/// Base class for list elements (ordered and unordered).
/// </summary>
public abstract class List : BlockContainer
{
    /// <summary>
    /// Gets or sets the list marker.
    /// </summary>
    public char Marker { get; set; }

    internal int MarkerOffset { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the list is tight. Tight lists have no space between their items.
    /// </summary>
    public bool Tight { get; set; } = true;
    internal int Padding { get; set; }
    internal string Delimiter { get; set; }

    internal override BlockMatch Matches(BlockParser parser)
    {
        return BlockMatch.Match;
    }

    /// <inheritdoc />
    public override bool CanContain(Block node)
    {
        return node is ListItem;
    }

    private static bool EndsWithBlankLine(Block block)
    {
        return block.Next != null && block.Range.End.Line != block.Next.Range.Start.Line - 1;
    }

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        var item = FirstChild;

        while (item != null)
        {
            // check for non-final list item ending with blank line:
            if (item.Next != null && EndsWithBlankLine(item))
            {
                Tight = false;
                break;
            }
            // recurse into children of list item, to see if there are
            // spaces between any of them:
            var subitem = item.FirstChild;

            while (subitem != null)
            {
                if (subitem.Next != null && EndsWithBlankLine(subitem))
                {
                    Tight = false;
                    break;
                }
                subitem = subitem.Next;
            }

            item = item.Next;
        }

        if (LastChild != null)
        {
            Range.End = LastChild.Range.End;
        }
    }
}