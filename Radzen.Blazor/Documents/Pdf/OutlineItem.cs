using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An entry in the document outline (bookmark) tree shown in the viewer's
/// navigation panel. Add or edit root entries through <see cref="Document.Outline"/>
/// or <see cref="DocumentBuilder.Outline"/>.
/// </summary>
/// <param name="title">The title shown in the bookmark panel.</param>
/// <param name="target">The location the entry navigates to, or <c>null</c> for a non-navigating entry.</param>
public sealed class OutlineItem(string title, OutlineTarget? target)
{
    private readonly TrackedList<OutlineItem> children = [];
    private bool touched;
    private string title = title;
    private OutlineTarget? target = target;
    private Color? color;
    private bool bold;
    private bool italic;
    private bool collapsed;

    /// <summary>Gets or sets the title shown in the bookmark panel.</summary>
    public string Title
    {
        get => title;
        set => Set(ref title, value);
    }

    /// <summary>Gets or sets the location the entry navigates to, or <c>null</c> when unavailable.</summary>
    // OutlineTarget is immutable, so assignment is the only way it can change.
    public OutlineTarget? Target
    {
        get => target;
        set => Set(ref target, value);
    }

    /// <summary>Gets the child entries nested under this one.</summary>
    public IList<OutlineItem> Children => children;

    /// <summary>
    /// Gets or sets the colour of the entry's title text (the <c>/C</c> entry). When
    /// <see langword="null"/> the viewer's default colour is used and no <c>/C</c> is written.
    /// </summary>
    public Color? Color
    {
        get => color;
        set => Set(ref color, value);
    }

    /// <summary>Gets or sets whether the entry's title is bold (the <c>/F</c> bit 2). Defaults to <see langword="false"/>.</summary>
    public bool Bold
    {
        get => bold;
        set => Set(ref bold, value);
    }

    /// <summary>Gets or sets whether the entry's title is italic (the <c>/F</c> bit 1). Defaults to <see langword="false"/>.</summary>
    public bool Italic
    {
        get => italic;
        set => Set(ref italic, value);
    }

    /// <summary>
    /// Gets or sets whether the entry is initially collapsed, hiding its descendants. A
    /// collapsed entry is written with a negative <c>/Count</c>. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Collapsed
    {
        get => collapsed;
        set => Set(ref collapsed, value);
    }

    /// <summary>
    /// Gets a value indicating whether this entry or any descendant has been modified since the
    /// document was loaded.
    /// </summary>
    public bool IsModified
    {
        get
        {
            if (touched || children.StructureChanged)
            {
                return true;
            }

            foreach (var child in children)
            {
                if (child.IsModified)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Set<T>(ref T field, T value)
    {
        field = value;
        touched = true;
    }

    internal void AcceptChanges()
    {
        touched = false;
        children.AcceptStructure();
        foreach (var child in children)
        {
            child.AcceptChanges();
        }
    }
}

// The page-destination fit the viewer applies when navigating to a page target.
internal enum OutlineFit
{
    // The library default: [page /XYZ 0 top 0], i.e. the top of the page at the
    // current zoom (kept so untouched outlines re-serialize byte-identically).
    PageTop,
    // [page /Fit]: fit the whole page in the window.
    Fit,
    // [page /FitH top]: fit the page width, positioning the given top coordinate.
    FitHorizontal,
    // [page /FitR left bottom right top]: fit the given rectangle in the window.
    Rectangle,
    // [page /XYZ left top zoom]: position the given point at the given zoom.
    Coordinates,
}

/// <summary>
/// The location an <see cref="OutlineItem"/> navigates to: a named anchor
/// (see <see cref="Run.Anchor"/>) or a page by zero-based index with a fit mode.
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

    internal OutlineFit Fit { get; private init; }

    internal double[] FitArguments { get; private init; } = [];

    /// <summary>Creates a target that navigates to the named anchor.</summary>
    /// <param name="name">The anchor name (see <see cref="Run.Anchor"/>).</param>
    /// <returns>The anchor target.</returns>
    public static OutlineTarget ToAnchor(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new OutlineTarget { Anchor = name };
    }

    /// <summary>Creates a target that navigates to the top of the given page.</summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <returns>The page target.</returns>
    public static OutlineTarget ToPage(int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        return new OutlineTarget { PageIndex = pageIndex };
    }

    /// <summary>
    /// Creates a target that fits the whole page in the window
    /// (destination <c>[page /Fit]</c>).
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <returns>The page target.</returns>
    public static OutlineTarget ToPageFit(int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        return new OutlineTarget { PageIndex = pageIndex, Fit = OutlineFit.Fit };
    }

    /// <summary>
    /// Creates a target that fits the page width, positioning <paramref name="top"/> at
    /// the top of the window (destination <c>[page /FitH top]</c>).
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="top">The y coordinate to place at the top of the window, in points.</param>
    /// <returns>The page target.</returns>
    public static OutlineTarget ToPageFitHorizontal(int pageIndex, double top)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        return new OutlineTarget { PageIndex = pageIndex, Fit = OutlineFit.FitHorizontal, FitArguments = [top] };
    }

    /// <summary>
    /// Creates a target that fits the given rectangle in the window
    /// (destination <c>[page /FitR left bottom right top]</c>).
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="left">The left edge of the rectangle, in points.</param>
    /// <param name="bottom">The bottom edge of the rectangle, in points.</param>
    /// <param name="right">The right edge of the rectangle, in points.</param>
    /// <param name="top">The top edge of the rectangle, in points.</param>
    /// <returns>The page target.</returns>
    public static OutlineTarget ToPageRectangle(int pageIndex, double left, double bottom, double right, double top)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        return new OutlineTarget { PageIndex = pageIndex, Fit = OutlineFit.Rectangle, FitArguments = [left, bottom, right, top] };
    }

    /// <summary>
    /// Creates a target that positions the given point at the given zoom
    /// (destination <c>[page /XYZ left top zoom]</c>).
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="left">The x coordinate to place at the left of the window, in points.</param>
    /// <param name="top">The y coordinate to place at the top of the window, in points.</param>
    /// <param name="zoom">The zoom factor; 0 keeps the current zoom.</param>
    /// <returns>The page target.</returns>
    public static OutlineTarget ToPageXYZ(int pageIndex, double left, double top, double zoom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        return new OutlineTarget { PageIndex = pageIndex, Fit = OutlineFit.Coordinates, FitArguments = [left, top, zoom] };
    }
}
