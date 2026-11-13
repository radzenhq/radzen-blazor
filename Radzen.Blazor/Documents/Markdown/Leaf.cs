using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Markdown;

#nullable enable

/// <summary>
/// Base class for markdown leaf block nodes.
/// </summary>
public abstract class Leaf : Block, IBlockInlineContainer
{
    /// <summary>
    /// Gets or sets the value of the leaf node.
    /// </summary>
    public string Value
    {
        get
        {
            if (pendingValue != null)
            {
                value = pendingValue.ToString();
                pendingValue = null;
            }

            return value;
        }
        set
        {
            pendingValue = null;
            this.value = value;
        }
    }

    private string value = string.Empty;
    private StringBuilder? pendingValue;

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

    internal void ReplaceInlines(IEnumerable<Inline> inlines)
    {
        children.Clear();
        children.AddRange(inlines);
    }
    internal ContentMap Content { get; } = new();

    internal void AddLine(BlockParser blockParser)
    {
        if (blockParser.PartiallyConsumedTab)
        {
            blockParser.Offset += 1;

            var charsToTab = 4 - (blockParser.Column % 4);

            AppendValue().Append(' ', charsToTab);

            Content.Append(charsToTab, blockParser.SourceOffset - 1, 1);
        }

        var text = blockParser.CurrentLine[blockParser.Offset..];

        AppendValue().Append(text).Append('\n');

        Content.Append(text.Length, blockParser.SourceOffset, text.Length);

        Content.Append(1, blockParser.LineEnd, blockParser.NextLineStart - blockParser.LineEnd);
    }

    private StringBuilder AppendValue()
    {
        return pendingValue ??= new StringBuilder(value);
    }

    internal void SetContent(string value, int sourceStart)
    {
        Value = value;

        Content.Clear();

        Content.Append(value.Length, sourceStart, value.Length);
    }

    internal void TrimContentStart(int count)
    {
        Value = Value[count..];

        Content.TrimStart(count);
    }

    internal void TrimContentEnd(int length)
    {
        Value = Value[..length];

        Content.TrimEnd(length);
    }

    internal void CopyContent(Leaf source)
    {
        Value = source.Value;

        Content.Clear();

        Content.Append(source.Content.Segments);
    }
}