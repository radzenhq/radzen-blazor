namespace Radzen.Documents.Markdown;

/// <summary>
/// Base class for markdown inline nodes.
/// </summary>
public abstract class Inline : INode
{
    /// <summary>
    /// Gets the offset in the markdown source at which this inline starts.
    /// </summary>
    public int SourceStart { get; internal set; }

    /// <summary>
    /// Gets the offset in the markdown source at which this inline ends (exclusive).
    /// </summary>
    public int SourceEnd { get; internal set; }

    internal bool Pristine { get; set; }

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    public abstract void Accept(INodeVisitor visitor);
}
