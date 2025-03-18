
using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

#nullable enable
/// <summary>
/// Base class for markdown block nodes that can contain other blocks.
/// </summary>
public abstract class BlockContainer : Block
{
    /// <summary>
    /// Returns the children of the block.
    /// </summary>
    public IReadOnlyList<Block> Children => children;

    private readonly List<Block> children = [];

    /// <summary>
    /// Determines if the block can contain the specified node.
    /// </summary>
    public virtual bool CanContain(Block node) => false;

    /// <summary>
    /// Appends a block to the children of the block.
    /// </summary>
    /// <typeparam name="T">The type of the block.</typeparam>
    /// <param name="block">The block to add.</param>
    /// <returns>The added block.</returns>
    public virtual T Add<T>(T block) where T : Block
    {
        children.Add(block);

        block.Parent = this;

        return block;
    }

    /// <summary>
    /// Replaces a block with another block.
    /// </summary>
    /// <param name="source">The block to replace.</param>
    /// <param name="target">The block to replace with.</param>
    public void Replace(Block source, Block target)
    {
        var index = children.IndexOf(source);

        if (index >= 0)
        {
            children[index] = target;
            target.Parent = this;
            target.Range = source.Range;
        }
    }

    /// <summary>
    /// Removes a block from the children of the block.
    /// </summary>
    /// <param name="block">The block to remove.</param>
    public void Remove(Block block)
    {
        children.Remove(block);
    }

    /// <summary>
    /// Returns the next sibling of the block.
    /// </summary>
    /// <param name="block">The block to get the next sibling of.</param>
    /// <returns>The next sibling of the block.</returns>
    public Block? NextSibling(Block block)
    {
        var index = children.IndexOf(block);

        if (index >= 0 && index < children.Count - 1)
        {
            return children[index + 1];
        }

        return null;
    }

    /// <inheritdoc/>
    public override Block? LastChild => children.Count > 0 ? children[^1] : null;

    /// <inheritdoc/>
    public override Block? FirstChild => children.Count > 0 ? children[0] : null;
}
