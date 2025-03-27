
namespace Radzen.Blazor.Markdown;

#nullable enable
/// <summary>
/// Base class for a markdown block nodes.
/// </summary>
public abstract class Block : INode
{
    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <param name="visitor"></param>
    public abstract void Accept(INodeVisitor visitor);

    /// <summary>
    /// Returns the last child of the block.
    /// </summary>
    public virtual Block? LastChild => null;

    /// <summary>
    /// Returns the first child of the block.
    /// </summary>
    public virtual Block? FirstChild => null;

    /// <summary>
    /// Returns the next sibling of the block.
    /// </summary>
    public virtual Block? Next => Parent.NextSibling(this);

    /// <summary>
    /// Returns the parent node of the block.
    /// </summary>
    public BlockContainer Parent { get; set; } = null!;

    /// <summary>
    /// Removes the block from its parent.
    /// </summary>
    public void Remove()
    {
        Parent.Remove(this);
    }

    internal virtual BlockMatch Matches(BlockParser parser) => 0;

    internal bool Open { get; set; } = true;

    internal Range Range;

    internal virtual void Close(BlockParser parser)
    {
        Open = false;
    }
}

enum BlockMatch
{
    Match,
    Skip,
    Break
}

struct Position
{
    public int Line { get; set; }
    public int Column { get; set; }
}

struct Range
{
    public Position Start;
    public Position End;
}