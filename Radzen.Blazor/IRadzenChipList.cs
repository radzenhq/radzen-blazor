using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Represents the common <see cref="RadzenChipList{TValue}" /> API used by
/// chip list items. Injected as a cascading property in <see cref="RadzenChipItem" />.
/// </summary>
public interface IRadzenChipList
{
    /// <summary>
    /// Adds the specified item to the chip list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    void AddItem(RadzenChipItem item);

    /// <summary>
    /// Removes the specified item from the chip list.
    /// </summary>
    /// <param name="item">The item.</param>
    void RemoveItem(RadzenChipItem item);

    /// <summary>
    /// Refreshes this instance.
    /// </summary>
    void Refresh();
}
