using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

#nullable enable

/// <summary>
/// Represents a list item node.
/// </summary>
public class ListItem : BlockContainer
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitListItem(this);
    }

    /// <inheritdoc />
    public override bool CanContain(Block node)
    {
        return node is not ListItem;
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        if (parser.Blank)
        {
            if (Children.Count == 0)
            {
                // Blank line after empty list item
                return BlockMatch.Skip;
            }
            else
            {
                parser.AdvanceNextNonSpace();
            }
        }
        else if (parser.Indent >= data.MarkerOffset + data.Padding)
        {
            parser.AdvanceOffset(data.MarkerOffset + data.Padding, true);
        }
        else
        {
            return BlockMatch.Skip;
        }

        return BlockMatch.Match;
    }

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        if (LastChild != null)
        {
            Range.End = LastChild.Range.End;
        }
        else
        {
            // Empty list item
            Range.End.Line = Range.Start.Line;

            if (Parent is List list)
            {
                Range.End.Column = list.MarkerOffset + list.Padding;
            }
        }
    }

    internal static BlockStart Start(BlockParser parser, Block container)
    {
        if ((!parser.Indented || container is List) && TryParseListMarker(parser, container, out var data))
        {
            parser.CloseUnmatchedBlocks();

            var list = container as List ?? (container.Parent is ListItem item ? item.data : null);

            // add the list if needed
            if (parser.Tip is not List || !ListsMatch(list, data))
            {
                parser.AddChild(data, parser.NextNonSpace);
            }

            var node = parser.AddChild<ListItem>(parser.NextNonSpace);
            node.data = data;

            return BlockStart.Container;
        }

        return BlockStart.Skip;
    }

    private List data = null!;

    private static readonly Regex UnorderedMarkerRegex = new(@"^[*+-]");

    private static readonly Regex OrderedMarkerRegex = new(@"^(\d{1,9})([.)])");

    private static bool TryParseListMarker(BlockParser parser, Block container, out List data)
    {
        data = null!;

        if (parser.Indent >= 4)
        {
            return false;
        }

        var rest = parser.CurrentLine[parser.NextNonSpace..];

        var match = UnorderedMarkerRegex.Match(rest);

        if (match.Success)
        {
            data = new UnorderedList
            {
                Marker = match.Value[0],
                MarkerOffset = parser.Indent
            };
        }
        else
        {
            match = OrderedMarkerRegex.Match(rest);

            if (match.Success && (container is not Paragraph || match.Groups[1].Value == "1"))
            {
                var list = new OrderedList
                {
                    MarkerOffset = parser.Indent,
                    Start = int.Parse(match.Groups[1].Value),
                    Delimiter = match.Groups[2].Value
                };
                data = list;
            }
            else
            {
                return false;
            }
        }

        // make sure we have spaces after
        var ch = parser.PeekNonSpace(match.Length);

        if (ch != default && !ch.IsSpaceOrTab())
        {
            return false;
        }

        // if it interrupts paragraph, make sure first line isn't blank
        if (container is Paragraph && string.IsNullOrWhiteSpace(parser.CurrentLine[(parser.NextNonSpace + match.Length)..]))
        {
            return false;
        }

        // we've got a match! advance offset and calculate padding
        parser.AdvanceNextNonSpace(); // to start of marker
        parser.AdvanceOffset(match.Length, true); // to end of marker

        var startColumn = parser.Column;
        var startOffset = parser.Offset;

        do
        {
            parser.AdvanceOffset(1, true);
            ch = parser.Peek();
        } while (parser.Column - startColumn < 5 && ch.IsSpaceOrTab());

        var blank = parser.Peek() == default;

        var spacesAfterMarker = parser.Column - startColumn;

        if (spacesAfterMarker >= 5 || spacesAfterMarker < 1 || blank)
        {
            data.Padding = match.Length + 1;
            parser.Column = startColumn;
            parser.Offset = startOffset;

            if (parser.Peek().IsSpaceOrTab())
            {
                parser.AdvanceOffset(1, true);
            }
        }
        else
        {
            data.Padding = match.Length + spacesAfterMarker;
        }

        return true;
    }

    private static bool ListsMatch(List? x, List y)
    {
        if (x == null)
        {
            return false;
        }

        return x.GetType() == y.GetType() && x.Marker == y.Marker && x.Delimiter == y.Delimiter;
    }
}
