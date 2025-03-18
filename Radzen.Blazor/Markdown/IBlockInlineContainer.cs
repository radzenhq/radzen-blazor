using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a block node that has inline children.
/// </summary>
public interface IBlockInlineContainer
{
    /// <summary>
    /// Gets the inline children of the block.
    /// </summary>
    IReadOnlyList<Inline> Children { get; }

    /// <summary>
    ///   Adds an inline child to the block.
    /// </summary>
    /// <param name="child"></param>
    void Add(Inline child);

    /// <summary>
    /// Gets string value of the block.
    /// </summary>
    string Value { get; }
}