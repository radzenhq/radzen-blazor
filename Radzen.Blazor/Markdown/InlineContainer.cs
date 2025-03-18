using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Base class for inline elements that contain other inline elements.
/// </summary>
public abstract class InlineContainer : Inline
{
    private readonly List<Inline> children = [];

    /// <summary>
    /// Gets the children of the container.
    /// </summary>
    public IReadOnlyList<Inline> Children => children;

    /// <summary>
    /// Appends a child to the container.
    /// </summary>
    /// <param name="node">The child to add.</param>
    public void Add(Inline node)
    {
        children.Add(node);
    }
}
