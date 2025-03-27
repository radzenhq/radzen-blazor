using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a markdown node that can be visited by a <see cref="INodeVisitor"/>.
/// </summary>
public interface INode
{
    /// <summary>
    /// Accepts a <see cref="INodeVisitor"/>.
    /// </summary>
    /// <param name="visitor"></param>
    public void Accept(INodeVisitor visitor);
}
