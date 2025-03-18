namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a markdown document.
/// </summary>
public class Document : BlockContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Document"/> class.
    /// </summary>
    public Document()
    {
        Range.Start.Line = 1;
        Range.Start.Column = 1;
    }

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitDocument(this);
    }

    /// <inheritdoc />
    public override bool CanContain(Block node)
    {
        return node is not ListItem;
    }

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        LinkReferenceParser.Parse(parser, this);
    }
}
