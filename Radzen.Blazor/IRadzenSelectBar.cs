using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Represents the common <see cref="RadzenSelectBar{TValue}" /> API used by
/// its items. Injected as a cascading property in <see cref="RadzenSelectBarItem" />.
/// </summary>
public interface IRadzenSelectBar
{
    /// <summary>
    /// Adds the specified item to the select bar.
    /// </summary>
    /// <param name="item">The item to add.</param>
    void AddItem(RadzenSelectBarItem item);

    /// <summary>
    /// Removes the specified item from the select bar.
    /// </summary>
    /// <param name="item">The item.</param>
    void RemoveItem(RadzenSelectBarItem item);

    /// <summary>
    /// Refreshes this instance.
    /// </summary>
    void Refresh();
}

