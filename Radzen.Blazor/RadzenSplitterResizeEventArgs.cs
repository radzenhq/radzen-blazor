namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenSplitter.Resize" /> event that is being raised.
/// </summary>
public class RadzenSplitterResizeEventArgs : RadzenSplitterEventArgs
{
    /// <summary>
    /// The new size of the pane
    /// </summary>
    public double NewSize { get; set; }
}

