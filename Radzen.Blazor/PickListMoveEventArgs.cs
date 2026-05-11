using System.Collections.Generic;
using System.Linq;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenPickList{TItem}"/> Move event.
/// </summary>
public class PickListMoveEventArgs<TItem>
{
    /// <summary>
    /// Gets a value indicating the direction of the move.
    /// </summary>
    /// <value><c>true</c> when items were moved from the source collection to the target collection; <c>false</c> when they were moved from the target collection to the source collection.</value>
    public bool MoveDirectionToTarget { get; internal set; }

    /// <summary>
    /// Gets the items that were moved.
    /// </summary>
    public IEnumerable<TItem> Items { get; internal set; } = Enumerable.Empty<TItem>();
}
