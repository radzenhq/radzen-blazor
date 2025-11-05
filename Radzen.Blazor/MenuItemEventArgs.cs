using Microsoft.AspNetCore.Components.Web;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenMenu.Click" /> event that is being raised.
/// </summary>
public class MenuItemEventArgs : MouseEventArgs
{
    /// <summary>
    /// Gets text of the clicked item.
    /// </summary>
    public string Text { get; internal set; }

    /// <summary>
    /// Gets the value of the clicked item.
    /// </summary>
    public object Value { get; internal set; }

    /// <summary>
    /// Gets the path path of the clicked item.
    /// </summary>
    public string Path { get; internal set; }
}

