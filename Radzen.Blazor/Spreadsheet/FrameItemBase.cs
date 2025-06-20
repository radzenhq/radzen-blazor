using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a base class for frame items in a spreadsheet.
/// </summary>
public abstract partial class FrameItemBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the context for the virtual grid that contains the frame item.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the range reference that defines the area of the frame item in the spreadsheet.
    /// </summary>
    [Parameter]
    public RangeRef Range { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame item is frozen in the row direction.
    /// </summary>
    [Parameter]
    public bool FrozenRow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame item is frozen in the column direction.
    /// </summary>
    [Parameter]
    public bool FrozenColumn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame item is positioned at the top of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Top { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame item is positioned on the left side of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Left { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame item is positioned at the bottom of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Bottom { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame item is positioned on the right side of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Right { get; set; }

    /// <summary>
    /// Gets or sets the sheet that contains the frame item.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// Gets the CSS class for the frame item.
    /// </summary>
    protected abstract string Class { get; }


    /// <summary>
    /// Gets the style for the frame item, including its position and dimensions.
    /// </summary>
    protected string Style
    {
        get
        {
            var rect = Context.GetRectangle(Range.Start.Row, Range.Start.Column, Range.End.Row, Range.End.Column);
            return $"transform: translate({rect.Left.ToPx()}, {rect.Top.ToPx()}); width: {rect.Width.ToPx()}; height: {rect.Height.ToPx()}";
        }
    }
}