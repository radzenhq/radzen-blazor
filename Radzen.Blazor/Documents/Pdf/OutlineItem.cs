using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An entry in the document outline (bookmark) tree shown in the viewer's
/// navigation panel. Add root entries to <see cref="DocumentBuilder.Outline"/>.
/// </summary>
/// <param name="title">The title shown in the bookmark panel.</param>
/// <param name="target">The location the entry navigates to.</param>
public sealed class OutlineItem(string title, OutlineTarget target)
{
    /// <summary>Gets or sets the title shown in the bookmark panel.</summary>
    public string Title { get; set; } = title;

    /// <summary>Gets or sets the location the entry navigates to.</summary>
    public OutlineTarget Target { get; set; } = target;

    /// <summary>Gets the child entries nested under this one.</summary>
    public IList<OutlineItem> Children { get; } = [];
}

/// <summary>
/// The location an <see cref="OutlineItem"/> navigates to: a named anchor
/// (see <see cref="Run.Anchor"/>) or the top of a page by zero-based index.
/// </summary>
public sealed class OutlineTarget
{
    private OutlineTarget()
    {
    }

    /// <summary>Gets the anchor name this target resolves through, or <c>null</c> for a page target.</summary>
    public string? Anchor { get; private init; }

    /// <summary>Gets the zero-based page index, or <c>null</c> for an anchor target.</summary>
    public int? PageIndex { get; private init; }

    /// <summary>Creates a target that navigates to the named anchor.</summary>
    /// <param name="name">The anchor name (see <see cref="Run.Anchor"/>).</param>
    /// <returns>The anchor target.</returns>
    public static OutlineTarget ToAnchor(string name)
    {
        System.ArgumentException.ThrowIfNullOrEmpty(name);
        return new OutlineTarget { Anchor = name };
    }

    /// <summary>Creates a target that navigates to the top of the given page.</summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <returns>The page target.</returns>
    public static OutlineTarget ToPage(int pageIndex)
    {
        System.ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        return new OutlineTarget { PageIndex = pageIndex };
    }
}
