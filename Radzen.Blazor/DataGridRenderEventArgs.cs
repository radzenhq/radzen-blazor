using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.Render" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridRenderEventArgs<T>
{
    /// <summary>
    /// Gets the instance of the RadzenDataGrid component which has rendered.
    /// </summary>
    public RadzenDataGrid<T> Grid { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this is the first time the RadzenDataGrid has rendered.
    /// </summary>
    /// <value><c>true</c> if this is the first time; otherwise, <c>false</c>.</value>
    public bool FirstRender { get; internal set; }
}

