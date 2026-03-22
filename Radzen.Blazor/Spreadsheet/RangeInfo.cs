using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Describes a sub-region of a range after splitting across frozen pane boundaries.
/// </summary>
public readonly struct RangeInfo
{
    /// <summary>The cell range for this region.</summary>
    public RangeRef Range { get; init; }
    /// <summary>Whether this region is in the frozen row area.</summary>
    public bool FrozenRow { get; init; }
    /// <summary>Whether this region is in the frozen column area.</summary>
    public bool FrozenColumn { get; init; }
    /// <summary>Whether this region is at the top edge of the original range.</summary>
    public bool Top { get; init; }
    /// <summary>Whether this region is at the left edge of the original range.</summary>
    public bool Left { get; init; }
    /// <summary>Whether this region is at the bottom edge of the original range.</summary>
    public bool Bottom { get; init; }
    /// <summary>Whether this region is at the right edge of the original range.</summary>
    public bool Right { get; init; }
}
