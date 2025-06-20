namespace Radzen.Blazor.Spreadsheet;

internal readonly struct RangeInfo
{
    public RangeRef Range { get; init; }
    public bool FrozenRow { get; init; }
    public bool FrozenColumn { get; init; }
    public bool Top { get; init; }
    public bool Left { get; init; }
    public bool Bottom { get; init; }
    public bool Right { get; init; }
}