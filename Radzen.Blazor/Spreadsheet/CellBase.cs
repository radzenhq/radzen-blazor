using System.Text;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Abstract base class for spreadsheet cells view components.
/// </summary>
public abstract class CellBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the cell rectangle in pixels.
    /// </summary>
    [Parameter]
    public PixelRectangle Rect { get; set; }

    /// <summary>
    /// Gets or sets the cell frozen state.
    /// </summary>
    [Parameter]
    public FrozenState FrozenState { get; set; }

    /// <summary>
    /// Returns the style of the cell.
    /// </summary>
    protected virtual string Style => GetStyle();

    /// <summary>
    /// Constructs the style string for the cell.
    /// </summary>
    protected virtual string GetStyle()
    {
        var sb = StringBuilderCache.Acquire();
        AppendStyle(sb);
        return StringBuilderCache.GetStringAndRelease(sb);
    }

    /// <summary>
    /// Appends the style properties to the StringBuilder.
    /// </summary>
    protected virtual void AppendStyle(StringBuilder sb)
    {
        Rect.AppendStyle(sb);
    }
}