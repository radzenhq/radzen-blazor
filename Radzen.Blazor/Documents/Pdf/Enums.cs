namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// Horizontal alignment of content.
/// </summary>
public enum HorizontalAlignment
{
    /// <summary>Align to the left edge.</summary>
    Left,
    /// <summary>Center horizontally.</summary>
    Center,
    /// <summary>Align to the right edge.</summary>
    Right,
    /// <summary>Stretch to fill the line.</summary>
    Justify,
    /// <summary>Align to the leading edge (respects flow direction).</summary>
    Start,
    /// <summary>Align to the trailing edge (respects flow direction).</summary>
    End,
}

/// <summary>
/// Vertical alignment of content.
/// </summary>
public enum VerticalAlignment
{
    /// <summary>Align to the top edge.</summary>
    Top,
    /// <summary>Center vertically.</summary>
    Middle,
    /// <summary>Align to the bottom edge.</summary>
    Bottom,
}

/// <summary>
/// Orientation of a page.
/// </summary>
public enum PageOrientation
{
    /// <summary>Portrait (height greater than width).</summary>
    Portrait,
    /// <summary>Landscape (width greater than height).</summary>
    Landscape,
}

/// <summary>
/// The line style of a border edge.
/// </summary>
public enum BorderStyle
{
    /// <summary>No border.</summary>
    None,
    /// <summary>A solid line.</summary>
    Solid,
    /// <summary>A dashed line.</summary>
    Dashed,
    /// <summary>A dotted line.</summary>
    Dotted,
}

/// <summary>
/// The base direction of inline content.
/// </summary>
public enum FlowDirection
{
    /// <summary>Left to right.</summary>
    LeftToRight,
    /// <summary>Right to left.</summary>
    RightToLeft,
}

/// <summary>
/// The writing mode used to lay out text.
/// </summary>
public enum WritingMode
{
    /// <summary>Horizontal lines flowing top to bottom.</summary>
    HorizontalTopToBottom,
    /// <summary>Vertical lines flowing right to left.</summary>
    VerticalRightToLeft,
    /// <summary>Vertical lines flowing left to right.</summary>
    VerticalLeftToRight,
}
