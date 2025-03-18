using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Base class for markdown leaf block nodes.
/// </summary>
public abstract class Leaf : Block, IBlockInlineContainer
{
    /// <summary>
    /// Gets or sets the value of the leaf node.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    private readonly List<Inline> children = [];

    /// <summary>
    /// Gets the children of the leaf node.
    /// </summary>
    public IReadOnlyList<Inline> Children => children;

    /// <summary>
    /// Appends a child to the leaf node.
    /// </summary>
    public void Add(Inline node)
    {
        children.Add(node);
    }
    internal void AddLine(BlockParser blockParser)
    {
        if (blockParser.PartiallyConsumedTab)
        {
            blockParser.Offset += 1;

            var charsToTab = 4 - (blockParser.Column % 4);

            Value += new string(' ', charsToTab);
        }

        Value += blockParser.CurrentLine[blockParser.Offset..] + "\n";
    }
}