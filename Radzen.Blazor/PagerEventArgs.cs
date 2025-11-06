namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenPager.PageChanged" /> event that is being raised.
/// </summary>
public class PagerEventArgs
{
    /// <summary>
    /// Gets how many items to skip.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets how many items to take.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets the current zero-based page index
    /// </summary>
    public int PageIndex { get; set; }
}

