using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about RadzenDropZoneContainer CanDrop function and RadzenDropZone Drop event.
/// </summary>
public class RadzenDropZoneItemEventArgs<TItem>
{
    /// <summary>
    /// Gets the dragged item zone.
    /// </summary>
    public RadzenDropZone<TItem> FromZone { get; internal set; }

    /// <summary>
    /// Gets the drop zone.
    /// </summary>
    public RadzenDropZone<TItem> ToZone { get; internal set; }

    /// <summary>
    /// Gets the dragged item.
    /// </summary>
    public TItem Item { get; internal set; }

    /// <summary>
    /// Gets the dropped item.
    /// </summary>
    public TItem ToItem { get; internal set; }

    /// <summary>
    /// The data that underlies a drag-and-drop operation, known as the drag data store.
    /// See <see cref="DataTransfer"/>.
    /// </summary>
    public DataTransfer DataTransfer { get; set; } = default!;
}

